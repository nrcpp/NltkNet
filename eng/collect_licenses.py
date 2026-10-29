import argparse
import email.policy
import hashlib
import json
import re
import shutil
from email.parser import BytesParser
from pathlib import Path


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def is_notice(path: Path) -> bool:
    return path.name.upper().startswith(
        ("LICENSE", "LICENCE", "COPYING", "NOTICE", "AUTHORS", "COPYRIGHT")
    )


def collect(runtime: Path, upstream: Path, repository: Path, rid: str) -> Path:
    destination = runtime / "licenses"
    if destination.exists():
        raise ValueError(f"License destination already exists: {destination}")
    destination.mkdir()
    metadata = json.loads((upstream / "PYTHON.json").read_text(encoding="utf-8"))
    expected_triples = {
        "osx-arm64": "aarch64-apple-darwin",
        "linux-x64": "x86_64-unknown-linux-gnu",
        "win-x64": "x86_64-pc-windows-msvc",
    }
    if metadata["target_triple"] != expected_triples[rid]:
        raise ValueError("Full distribution metadata does not match the target RID")

    documents: dict[str, str] = {}

    def include(source: Path, relative: Path) -> None:
        if not source.is_file() or source.stat().st_size == 0:
            raise ValueError(f"Missing or empty license document: {source}")
        target = destination / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(source, target)
        documents[relative.as_posix()] = digest(target)

    include(repository / "LICENSE", Path("NltkNet-MIT.txt"))
    include(repository / "eng/THIRD-PARTY-NOTICES.txt", Path("THIRD-PARTY-NOTICES.txt"))
    include(runtime / "PYTHON-BUILD-STANDALONE-LICENSE", Path("build-tooling/MPL-2.0.txt"))
    if rid == "win-x64":
        include(runtime / "MICROSOFT-VS2022-COMMUNITY-LICENSE.txt", Path("microsoft/VS2022-COMMUNITY-LICENSE.txt"))
        include(repository / "eng/WINDOWS-REDISTRIBUTION.txt", Path("microsoft/REDISTRIBUTION.txt"))
        shutil.copyfile(runtime / "microsoft-redistribution.json", destination / "microsoft-redistribution.json")
    shutil.copyfile(upstream / "PYTHON.json", destination / "CPython-PYTHON.json")
    shutil.copyfile(upstream / "provenance.json", destination / "CPython-provenance.json")
    modifications: dict[str, object] = {"excluded_extensions": []}
    modifications_file = runtime / "runtime-modifications.json"
    if modifications_file.is_file():
        modifications = json.loads(modifications_file.read_bytes())
        shutil.copyfile(modifications_file, destination / modifications_file.name)
    exclusions = modifications["excluded_extensions"]
    for exclusion in exclusions:
        if (runtime / exclusion["path"]).exists():
            raise ValueError(f"Excluded extension is still present: {exclusion['path']}")
        if exclusion["extension"] not in metadata["build_info"]["extensions"]:
            raise ValueError(f"Excluded extension not found in upstream metadata: {exclusion['extension']}")
    for name in ("numpy-build-provenance.json", "numpy-build-requirements.lock"):
        if (runtime / name).is_file():
            shutil.copyfile(runtime / name, destination / name)
    for source in sorted((upstream / "licenses").glob("*")):
        if source.is_file():
            include(source, Path("cpython") / source.name)

    components: list[dict[str, object]] = []
    python_license = metadata["license_path"]
    if not (upstream / python_license).is_file():
        raise ValueError(f"Missing CPython license: {python_license}")
    components.append({
        "name": "CPython",
        "licenses": metadata["licenses"],
        "documents": ["cpython/" + Path(python_license).name],
    })
    for name, variants in sorted(metadata["build_info"]["extensions"].items()):
        if any(exclusion["extension"] == name for exclusion in exclusions):
            continue
        for variant in variants:
            paths = variant.get("license_paths", [])
            for path in paths:
                if not (upstream / path).is_file():
                    raise ValueError(f"Missing native dependency license: {path}")
            if paths or variant.get("licenses"):
                components.append({
                    "name": name,
                    "licenses": variant.get("licenses", []),
                    "public_domain": variant.get("license_public_domain"),
                    "documents": ["cpython/" + Path(path).name for path in paths],
                    "links": variant.get("links", []),
                })

    site = runtime / ("Lib/site-packages" if rid == "win-x64" else "lib/python3.12/site-packages")
    distributions = sorted(site.glob("*.dist-info"))
    if not distributions:
        raise ValueError(f"No Python distribution metadata found: {site}")
    packages: list[dict[str, object]] = []
    for distribution in distributions:
        message = BytesParser(policy=email.policy.default).parsebytes(
            (distribution / "METADATA").read_bytes()
        )
        name, version = str(message["Name"]), str(message["Version"])
        if not re.fullmatch(r"[A-Za-z0-9_.-]+", name):
            raise ValueError(f"Invalid distribution name: {name}")
        package_documents: set[Path] = set()
        license_directory = distribution / "licenses"
        if license_directory.is_dir():
            package_documents.update(p for p in license_directory.rglob("*") if p.is_file())
        package_documents.update(p for p in distribution.iterdir() if p.is_file() and is_notice(p))
        for declared in message.get_all("License-File", []):
            candidates = (license_directory / str(declared), distribution / str(declared))
            found = next((p for p in candidates if p.is_file()), None)
            if found is None:
                raise ValueError(f"{name}: declared License-File is missing: {declared}")
            package_documents.add(found)
        if not any(is_notice(p) and not p.name.upper().startswith("AUTHORS") for p in package_documents):
            raise ValueError(f"{name}: no license text found")
        for source in sorted(package_documents):
            include(source, Path("python-packages") / source.relative_to(site))
        packages.append({
            "name": name,
            "version": version,
            "declared_license": str(message.get("License-Expression") or message.get("License") or ""),
            "license_classifiers": [
                str(c) for c in message.get_all("Classifier", []) if str(c).startswith("License ::")
            ],
            "source": f"https://pypi.org/project/{name}/{version}/#files",
            "documents": [
                "python-packages/" + p.relative_to(site).as_posix()
                for p in sorted(package_documents)
            ],
        })

    # Preserve notices outside dist-info too, notably pip's vendored dependencies.
    for source in sorted(site.rglob("*")):
        if source.is_file() and is_notice(source):
            relative = Path("python-packages") / source.relative_to(site)
            if relative.as_posix() not in documents:
                include(source, relative)
    vendor_versions = site / "pip/_vendor/vendor.txt"
    if vendor_versions.is_file():
        include(vendor_versions, Path("python-packages/pip/_vendor/vendor.txt"))

    native_files = [
        {"path": p.relative_to(runtime).as_posix(), "sha256": digest(p)}
        for p in sorted(runtime.rglob("*"))
        if p.is_file() and destination not in p.parents
        and (p.suffix.lower() in (".dll", ".dylib", ".pyd", ".exe") or ".so" in p.suffixes)
    ]
    blockers: list[str] = []
    if any("Sleepycat" in component.get("licenses", []) for component in components):
        blockers.append(
            "Berkeley DB (Sleepycat) is linked into _dbm. Resolve source-availability "
            "obligations for DB and accompanying software, or remove/replace this "
            "backend and verify compatibility. A generic source homepage is not sufficient."
        )
    if any("libquadmath" in str(item["path"]) for item in native_files):
        blockers.append(
            "NumPy bundles libquadmath (LGPL-2.1-or-later). Provide the exact corresponding "
            "source including applicable patches/build information, and verify LGPL "
            "replacement/relinking and recipient rights. NumPy's license text is included; "
            "it does not itself fulfill source-distribution obligations."
        )
    if any(re.search(r"(vcruntime|msvcp)\d", str(item["path"]), re.IGNORECASE) for item in native_files):
        blockers.append(
            "Microsoft runtime DLLs are bundled by CPython/NumPy. Official VS2022 "
            "Community terms and redistribution guidance are included on Windows. "
            "The publisher must confirm applicable Visual Studio entitlement and "
            "section 4 downstream protective terms. Inclusion and NuGet license-acceptance "
            "metadata do not establish recipient agreement or publisher entitlement."
        )

    review = [
        f"NltkNet runtime license review: {rid}",
        "",
        "This inventory records shipped files and upstream declarations; it is not legal clearance.",
        "Unused license texts from the full CPython archive are retained for reference.",
        "Their presence alone does not mean the corresponding component is shipped.",
        "CPython-PYTHON.json records per-extension link/license declarations.",
        "runtime-modifications.json, when present, records intentionally excluded",
        "extensions; inventory.json describes only the retained extensions.",
        "NumPy's declarations may describe libraries absent from a particular wheel;",
        "inventory.json separately records the native files actually present.",
        "",
        "Source-form availability:",
        "- Unmodified tqdm Python source is included in the runtime's site-packages/tqdm.",
        "  Its MPL-2.0/MIT terms remain applicable independently of NltkNet's MIT license.",
        "- pip's vendored source and notices are included under site-packages/pip/_vendor.",
        "  This includes MPL-licensed certifi source/certificate data, where present.",
        "- GCC runtime notices and the GCC Runtime Library Exception are preserved in",
        "  NumPy's LICENSE.txt. Review the exception against the actual shipped binaries;",
        "  do not infer that the exception also covers libquadmath.",
        "- Exact target-wheel download URLs and hashes are in wheel-install-report.json.",
        "  Upstream CPython archive URL/hash are in CPython-provenance.json.",
        "",
        "Outstanding publication blockers:",
        *[f"- {blocker}" for blocker in blockers],
    ]
    if not blockers:
        review.append("- No platform-specific blocker detected by these checks.")
    review.extend([
        "",
        "Manual review is still required for source completeness, native build provenance,",
        "copyright/NOTICE obligations, and any terms not expressed in package metadata.",
        "Models/corpora are not included and require a separate redistribution review.",
        "",
    ])
    (destination / "PUBLISHING-REVIEW.txt").write_text("\n".join(review), encoding="utf-8")
    shutil.copyfile(runtime / "wheel-install-report.json", destination / "wheel-install-report.json")
    inventory = {
        "rid": rid,
        "cpython_components": components,
        "python_packages": packages,
        "native_files": native_files,
        "runtime_modifications": modifications,
        "documents": documents,
        "publication_blockers": blockers,
        "legal_clearance": False,
    }
    (destination / "inventory.json").write_text(
        json.dumps(inventory, indent=2, sort_keys=True) + "\n", encoding="utf-8"
    )
    combined = [
        "NltkNet runtime - applicable license texts and notices\n",
        "NltkNet integration/packaging is MIT licensed; bundled components retain their",
        "own terms. This document does not relicense third-party software.",
        "See inventory.json and PUBLISHING-REVIEW.txt for scope and unresolved obligations.\n",
    ]
    for relative in sorted(documents):
        combined.extend([
            f"\n{'=' * 72}\n{relative}\n{'=' * 72}\n",
            (destination / relative).read_bytes().decode("utf-8"),
        ])
    (destination / "LICENSE.txt").write_bytes(("\n".join(combined) + "\n").encode("utf-8"))
    print(f"{rid}: collected {len(documents)} documents, {len(packages)} Python packages")
    for blocker in blockers:
        print(f"PUBLICATION BLOCKER: {blocker}")
    return destination


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("rid", choices=("osx-arm64", "linux-x64", "win-x64"))
    parser.add_argument("runtime", type=Path)
    parser.add_argument("upstream", type=Path)
    parser.add_argument("repository", type=Path)
    args = parser.parse_args()
    collect(args.runtime, args.upstream, args.repository, args.rid)
