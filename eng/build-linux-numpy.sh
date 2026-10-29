#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
image="quay.io/pypa/manylinux_2_28_x86_64@sha256:e0adf18521ad35192df733a6c62d35df7801957d24e0470238aacecfab2188a3"
work="$root/.artifacts/numpy-linux"
mkdir -p "$work"
source_url="https://files.pythonhosted.org/packages/95/b0/c7453d0b6e2073c3264468b106ee1563750cecc910965e67357e3698c83e/numpy-2.5.4.tar.gz"
if [[ ! -f "$work/numpy-2.5.4.tar.gz" ]]; then
    curl --fail --location --retry 3 "$source_url" -o "$work/numpy-2.5.4.tar.gz"
fi

docker run --rm --platform linux/amd64 \
    --user "$(id -u):$(id -g)" -e HOME=/tmp \
    -v "$root/eng:/eng:ro" -v "$work:/work" "$image" bash -c '
set -euo pipefail
echo "9a94cf751c9ad8ebaa835bcd3d40dacf8534ad086b88c38029b65123c7999d2a  /work/numpy-2.5.4.tar.gz" | sha256sum -c -
stage="$(mktemp -d /work/build.XXXXXX)"
trap '\''rm -rf "$stage"'\'' EXIT
/opt/python/cp312-cp312/bin/python -m venv "$stage/venv"
python="$stage/venv/bin/python"
"$python" -I -m pip --isolated install --index-url https://pypi.org/simple \
    --require-hashes --only-binary=:all: --no-deps -r /eng/requirements-numpy-build.lock
tar -xzf /work/numpy-2.5.4.tar.gz -C "$stage"
export PATH="$stage/venv/bin:$PATH"
export SOURCE_DATE_EPOCH=1792972800
"$python" -I -m pip --isolated wheel --no-build-isolation --no-deps --no-index \
    -Csetup-args=-Dblas=none -Csetup-args=-Dlapack=none \
    -Csetup-args=-Dallow-noblas=true -Csetup-args=-Dcpu-baseline=min \
    -Csetup-args=-Dcpu-dispatch=none -Ccompile-args=-j4 \
    --wheel-dir "$stage/wheels" "$stage/numpy-2.5.4"
auditwheel repair --plat manylinux_2_28_x86_64 -w "$stage/repaired" "$stage"/wheels/*.whl
"$python" -I -m pip --isolated install --no-deps --no-index "$stage"/repaired/*.whl
cd /
"$python" -I /eng/check_linux_runtime.py --numpy-only
auditwheel show "$stage"/repaired/*.whl
mkdir -p /work/wheels
cp "$stage"/repaired/*.whl /work/wheels/
"$python" -I /eng/linux_numpy_provenance.py /work
'
echo "Linux NumPy wheel and build provenance: $work"
