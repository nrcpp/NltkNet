import argparse
import hashlib
import io
import json
import tarfile
import xml.etree.ElementTree as ET
import zipfile
from pathlib import Path


def verify(package: Path, require_publishable: bool = False) -> None:
    with zipfile.ZipFile(package) as archive:
        inventory = json.loads(archive.read("licenses/inventory.json"))
        rid = inventory["rid"]
        nuspec = next(name for name in archive.namelist() if name.endswith(".nuspec"))
        root = ET.fromstring(archive.read(nuspec))
        license_node = root.find("{*}metadata/{*}license")
        if license_node is None or license_node.attrib.get("type") != "file":
            raise ValueError(f"{package}: missing file-based NuGet license metadata")
        if license_node.text != "licenses/LICENSE.txt":
            raise ValueError(f"{package}: incorrect NuGet license document")
        combined = archive.read("licenses/LICENSE.txt").decode("utf-8")
        runtime_path = f"runtimes/{rid}/native/python.tar.gz"
        payload = archive.read(runtime_path)
        checksum = archive.read(runtime_path + ".sha256").decode("ascii").strip()
        if hashlib.sha256(payload).hexdigest() != checksum:
            raise ValueError(f"{package}: runtime archive checksum mismatch")
        with tarfile.open(fileobj=io.BytesIO(payload), mode="r:gz") as runtime:
            for exclusion in inventory.get("runtime_modifications", {}).get("excluded_extensions", []):
                if "python/" + exclusion["path"] in runtime.getnames():
                    raise ValueError(f"{package}: excluded backend is still shipped")
                if any(component["name"] == exclusion["extension"] for component in inventory["cpython_components"]):
                    raise ValueError(f"{package}: excluded backend is still in the active inventory")
            if rid == "win-x64":
                acceptance = root.find("{*}metadata/{*}requireLicenseAcceptance")
                if acceptance is None or acceptance.text != "true":
                    raise ValueError(f"{package}: Windows package must request license acceptance")
                microsoft = json.loads(archive.read("licenses/microsoft-redistribution.json"))
                for dll in microsoft["files"]:
                    member = runtime.extractfile("python/" + dll["path"])
                    if member is None or hashlib.sha256(member.read()).hexdigest() != dll["sha256"]:
                        raise ValueError(f"{package}: Microsoft DLL inventory mismatch")
            embedded_inventory = runtime.extractfile("python/licenses/inventory.json")
            if embedded_inventory is None or embedded_inventory.read() != archive.read("licenses/inventory.json"):
                raise ValueError(f"{package}: embedded inventory differs from NuGet inventory")
            for name in archive.namelist():
                if name.startswith("licenses/") and not name.endswith("/"):
                    embedded = runtime.extractfile("python/" + name)
                    if embedded is None or embedded.read() != archive.read(name):
                        raise ValueError(f"{package}: embedded license differs: {name}")
            documents = inventory["documents"]
            if not documents:
                raise ValueError(f"{package}: empty license inventory")
            for name, checksum in documents.items():
                original = archive.read("licenses/" + name)
                if hashlib.sha256(original).hexdigest() != checksum:
                    raise ValueError(f"{package}: license hash mismatch: {name}")
                if original.decode("utf-8") not in combined:
                    raise ValueError(f"{package}: consolidated license omits: {name}")
            for native in inventory["native_files"]:
                member = runtime.extractfile("python/" + native["path"])
                if member is None or hashlib.sha256(member.read()).hexdigest() != native["sha256"]:
                    raise ValueError(f"{package}: native inventory mismatch: {native['path']}")
                if rid == "linux-x64" and any(
                    forbidden in native["path"].lower()
                    for forbidden in ("_dbm.", "libquadmath", "libgfortran", "libscipy_openblas", "libopenblas", "libdb-", "libdb.")
                ):
                    raise ValueError(f"{package}: excluded Linux native component is present")
            for component in inventory["cpython_components"]:
                for document in component["documents"]:
                    if document not in documents:
                        raise ValueError(f"{package}: uncollected CPython license: {document}")
            for distribution in inventory["python_packages"]:
                for document in distribution["documents"]:
                    if document not in documents:
                        raise ValueError(f"{package}: uncollected Python package license: {document}")
        blockers = inventory["publication_blockers"]
        print(f"PASS: {rid}: {len(documents)} license documents, matching embedded copies and native hashes")
        for blocker in blockers:
            print(f"PUBLICATION BLOCKER: {blocker}")
        if require_publishable and (blockers or not inventory["legal_clearance"]):
            raise ValueError(f"{package}: publication is not cleared; see licenses/PUBLISHING-REVIEW.txt")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("packages", nargs="+", type=Path)
    parser.add_argument("--require-publishable", action="store_true")
    args = parser.parse_args()
    for package in args.packages:
        verify(package, args.require_publishable)
