import argparse
import hashlib
import json
import shutil
import xml.etree.ElementTree as ET
import zipfile
from pathlib import Path

from verify_licenses import verify


def select(source: Path, destination: Path, version: str, commit: str) -> None:
    if destination.exists():
        raise ValueError(f"Release destination must not exist: {destination}")
    expected = {
        "packages-osx-arm64": {"NltkNet", "NltkNet.Runtime.osx-arm64"},
        "packages-win-x64": {"NltkNet", "NltkNet.Runtime.win-x64"},
        "packages-linux-x64": {"NltkNet", "NltkNet.Runtime.linux-x64"},
    }
    selected: dict[str, Path] = {}
    for folder, package_ids in expected.items():
        paths = sorted((source / folder).glob("*.nupkg"))
        actual: set[str] = set()
        for path in paths:
            with zipfile.ZipFile(path) as archive:
                manifests = [name for name in archive.namelist() if name.endswith(".nuspec")]
                if len(manifests) != 1:
                    raise ValueError(f"{path}: expected exactly one NuGet manifest")
                metadata = ET.fromstring(archive.read(manifests[0])).find("{*}metadata")
                if metadata is None:
                    raise ValueError(f"{path}: missing package metadata")
                identity = metadata.findtext("{*}id")
                if identity not in package_ids or identity in actual:
                    raise ValueError(f"{path}: unexpected or duplicate package ID: {identity}")
                if metadata.findtext("{*}version") != version:
                    raise ValueError(f"{path}: unexpected version")
                repository = metadata.find("{*}repository")
                if repository is None or repository.attrib.get("commit") != commit:
                    raise ValueError(f"{path}: package source commit differs from successful CI")
                actual.add(identity)
                if identity != "NltkNet":
                    verify(path)
                    selected[identity] = path
                elif folder == "packages-osx-arm64":
                    selected[identity] = path
        if actual != package_ids:
            raise ValueError(f"{folder}: expected {package_ids}, found {actual}")
    destination.mkdir(parents=True)
    hashes = {}
    for identity, path in sorted(selected.items()):
        filename = f"{identity}.{version}.nupkg"
        shutil.copyfile(path, destination / filename)
        hashes[filename] = hashlib.sha256(path.read_bytes()).hexdigest()
    (destination / "SHA256.json").write_text(json.dumps(hashes, indent=2) + "\n", encoding="utf-8")
    print(f"Selected {len(selected)} tested packages from commit {commit}; no rebuild performed.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("source", type=Path)
    parser.add_argument("destination", type=Path)
    parser.add_argument("version")
    parser.add_argument("commit")
    args = parser.parse_args()
    select(args.source, args.destination, args.version, args.commit)
