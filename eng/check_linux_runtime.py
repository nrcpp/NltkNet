import argparse
import importlib.util
import json
import subprocess
import sys
import tempfile
from pathlib import Path

import numpy as np


parser = argparse.ArgumentParser()
parser.add_argument("--numpy-only", action="store_true")
args = parser.parse_args()
np.testing.assert_allclose(np.dot([[1., 2.], [3., 4.]], [[2.], [1.]]), [[4.], [10.]])
np.testing.assert_allclose(np.linalg.solve([[3., 1.], [1., 2.]], [9., 8.]), [2., 3.])
np.testing.assert_allclose(np.linalg.svd([[3., 0.], [0., 2.]], compute_uv=False), [3., 2.])
config = np.show_config(mode="dicts")
for dependency in ("blas", "lapack"):
    # NumPy's generated config removes false-valued fields.
    if config["Build Dependencies"][dependency].get("found", False):
        raise ValueError(f"External {dependency} unexpectedly enabled")
numpy_root = Path(np.__file__).parent
for path in numpy_root.rglob("*.so"):
    dynamic = subprocess.check_output(["readelf", "-d", str(path)], text=True)
    if any(name in dynamic.lower() for name in ("libquadmath", "libgfortran", "libopenblas", "libscipy_openblas")):
        raise ValueError(f"Forbidden NumPy dependency: {path}\n{dynamic}")

if not args.numpy_only:
    runtime = Path(sys.prefix)
    if importlib.util.find_spec("_dbm") is not None:
        raise ValueError("Berkeley DB _dbm backend is still importable")
    for path in runtime.rglob("*"):
        if path.is_file() and (".so" in path.name or path.parent.name == "bin"):
            if any(name in path.name.lower() for name in ("libquadmath", "libgfortran", "libdb-", "libdb.")):
                raise ValueError(f"Forbidden runtime library is present: {path}")
            if path.read_bytes()[:4] != b"\x7fELF":
                continue
            dynamic = subprocess.check_output(["readelf", "-d", str(path)], text=True)
            if any(name in dynamic.lower() for name in ("libquadmath", "libgfortran", "libdb-", "libdb.")):
                raise ValueError(f"Forbidden runtime dependency: {path}\n{dynamic}")
    import dbm
    import shelve
    with tempfile.TemporaryDirectory() as directory:
        with shelve.open(str(Path(directory) / "shelf")) as shelf:
            shelf["nltk"] = ["works", 42]
        with shelve.open(str(Path(directory) / "shelf")) as shelf:
            if shelf["nltk"] != ["works", 42]:
                raise ValueError("shelve round-trip failed without Berkeley DB")
    print("PASS: _dbm excluded; dbm/shelve fallback works; runtime ELF dependencies checked")
print("PASS: NumPy dot/solve/SVD and no external BLAS/Fortran dependencies")
print(json.dumps(config["Build Dependencies"], indent=2))
