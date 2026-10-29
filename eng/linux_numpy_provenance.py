import hashlib
import json
import sys
from pathlib import Path


work = Path(sys.argv[1])
wheels = list((work / "wheels").glob("numpy-2.5.4-*.whl"))
if len(wheels) != 1:
    raise ValueError(f"Expected exactly one Linux NumPy wheel, found {len(wheels)}")
wheel = wheels[0]
provenance = {
    "source_url": "https://files.pythonhosted.org/packages/95/b0/c7453d0b6e2073c3264468b106ee1563750cecc910965e67357e3698c83e/numpy-2.5.4.tar.gz",
    "source_sha256": "9a94cf751c9ad8ebaa835bcd3d40dacf8534ad086b88c38029b65123c7999d2a",
    "builder_image": "quay.io/pypa/manylinux_2_28_x86_64@sha256:e0adf18521ad35192df733a6c62d35df7801957d24e0470238aacecfab2188a3",
    "build_requirements_sha256": hashlib.sha256(Path("/eng/requirements-numpy-build.lock").read_bytes()).hexdigest(),
    "meson_options": {
        "blas": "none", "lapack": "none", "allow-noblas": True,
        "cpu-baseline": "min", "cpu-dispatch": "none",
    },
    "wheel": wheel.name,
    "wheel_sha256": hashlib.sha256(wheel.read_bytes()).hexdigest(),
}
(work / "provenance.json").write_text(json.dumps(provenance, indent=2) + "\n", encoding="utf-8")
(work / "requirements.lock").write_text(
    f"numpy==2.5.4 --hash=sha256:{provenance['wheel_sha256']}\n", encoding="ascii"
)
