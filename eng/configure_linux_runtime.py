import hashlib
import json
import shutil
import sys
from pathlib import Path


def configure(runtime: Path, numpy_build: Path) -> None:
    provenance = json.loads((numpy_build / "provenance.json").read_bytes())
    wheel = numpy_build / "wheels" / provenance["wheel"]
    if hashlib.sha256(wheel.read_bytes()).hexdigest() != provenance["wheel_sha256"]:
        raise ValueError("Linux NumPy wheel hash differs from its build provenance")
    if provenance["meson_options"]["blas"] != "none" or provenance["meson_options"]["lapack"] != "none":
        raise ValueError("Linux NumPy must be built without external BLAS/LAPACK")
    expected_lock = f"numpy==2.5.4 --hash=sha256:{provenance['wheel_sha256']}\n"
    if (numpy_build / "requirements.lock").read_text(encoding="ascii") != expected_lock:
        raise ValueError("Linux NumPy lock differs from its build provenance")
    report_file = runtime / "wheel-install-report.json"
    report = json.loads(report_file.read_bytes())
    numpy_report = json.loads((runtime / "numpy-wheel-install-report.json").read_bytes())
    if len(numpy_report["install"]) != 1 or numpy_report["install"][0]["metadata"]["name"] != "numpy":
        raise ValueError("Expected exactly one locally built NumPy installation")
    report["install"].extend(numpy_report["install"])
    report_file.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    extensions = list((runtime / "lib/python3.12/lib-dynload").glob("_dbm.*.so"))
    if len(extensions) != 1:
        raise ValueError(f"Expected exactly one Berkeley DB backend, found {len(extensions)}")
    extension = extensions[0]
    removed = {
        "extension": "_dbm",
        "path": extension.relative_to(runtime).as_posix(),
        "sha256": hashlib.sha256(extension.read_bytes()).hexdigest(),
        "reason": "Exclude statically linked Berkeley DB/Sleepycat; dbm.dumb remains available.",
    }
    extension.unlink()
    (runtime / "runtime-modifications.json").write_text(
        json.dumps({"excluded_extensions": [removed]}, indent=2) + "\n", encoding="utf-8"
    )
    for name in ("provenance.json", "requirements.lock"):
        shutil.copyfile(numpy_build / name, runtime / ("numpy-build-" + name))


if __name__ == "__main__":
    configure(Path(sys.argv[1]), Path(sys.argv[2]))
