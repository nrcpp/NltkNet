#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
case "$(uname -s)/$(uname -m)" in
    Darwin/arm64) host=osx-arm64 ;;
    Linux/x86_64) host=linux-x64 ;;
    MINGW*/x86_64|MSYS*/x86_64) host=win-x64 ;;
    *) echo "Unsupported build host. Use macOS ARM64, Linux x64 or Windows x64 (Git Bash)." >&2; exit 1 ;;
esac
rid="${1:-$host}"

distribution() {
    case "$1" in
        osx-arm64)
            triple=aarch64-apple-darwin
            variant=pgo+lto
            full_checksum=016758f4881e5f71d63eddfbcdd53f25c5a9152435a863e4dc0770758eae32c3
            checksum=353d5f1b4fd2715e7053f12f068435d5bbad16b91e057006b5d01e0925709a7e
            platform=macosx_14_0_arm64 ;;
        win-x64)
            triple=x86_64-pc-windows-msvc
            variant=pgo
            full_checksum=37b4eb2818fee605fefa75ed156c4f2e1aaeaebaf662f9b30177c842ee16c48f
            checksum=1f2ab53880fc67ea323398c6388e05af28addc99a8e389209f08a51fdff4b149
            platform=win_amd64 ;;
        linux-x64)
            triple=x86_64-unknown-linux-gnu
            variant=pgo+lto
            full_checksum=789924432efcaaf05fec4bd3e4d8798733326c2ac0540eda33e84913c37a231e
            checksum=ce4f6d0a6cfdb3bed6e44c2f9affd85943c76a42e25ad3a259002f31d6fb8a20
            platform=manylinux_2_28_x86_64 ;;
        *) echo "Unsupported runtime: $1" >&2; exit 1 ;;
    esac
}

verify() {
    local actual
    if command -v sha256sum >/dev/null 2>&1; then
        actual="$(sha256sum "$2" | awk '{print $1}')"
    else
        actual="$(shasum -a 256 "$2" | awk '{print $1}')"
    fi
    if [[ "$actual" != "$1" ]]; then
        echo "SHA-256 mismatch: $2" >&2
        exit 1
    fi
    echo "$2: SHA-256 OK"
}

download() {
    distribution "$1"
    local directory="$root/.artifacts/runtime-$1"
    mkdir -p "$directory"
    downloaded="$directory/cpython.tar.gz"
    if [[ ! -f "$downloaded" ]]; then
        curl --fail --location --retry 3 \
            "https://github.com/astral-sh/python-build-standalone/releases/download/20261026/cpython-3.12.16%2B20261026-$triple-install_only_stripped.tar.gz" \
            --output "$downloaded"
    fi
    verify "$checksum" "$downloaded"
}

# Cross-building installs target wheels using the host's bundled Python, never system Python.
download "$host"
tools="$(mktemp -d "$root/.artifacts/runtime-$host/tools.XXXXXX")"
stage=""
upstream=""
cleanup() {
    local status=$?
    rm -rf "$tools"
    if [[ -n "$stage" ]]; then rm -rf "$stage"; fi
    if [[ -n "$upstream" ]]; then rm -rf "$upstream"; fi
    return "$status"
}
trap cleanup EXIT
tar -xzf "$downloaded" -C "$tools"
if [[ "$host" == win-x64 ]]; then
    python="$tools/python/python.exe"
else
    python="$tools/python/bin/python3"
fi

download "$rid"
work="$root/.artifacts/runtime-$rid"
output="$root/NltkNet.Runtime.$rid/runtimes/$rid/native"
mkdir -p "$output"
stage="$(mktemp -d "$work/stage.XXXXXX")"
tar -xzf "$downloaded" -C "$stage"
if [[ "$rid" == win-x64 ]]; then
    site="$stage/python/Lib/site-packages"
else
    site="$stage/python/lib/python3.12/site-packages"
fi
if [[ "$rid" == linux-x64 ]]; then
    numpy_build="$root/.artifacts/numpy-linux"
    if [[ ! -f "$numpy_build/provenance.json" || ! -f "$numpy_build/requirements.lock" ]]; then
        echo "Build Linux NumPy first: bash eng/build-linux-numpy.sh (requires Docker)." >&2
        exit 1
    fi
fi
PYTHONNOUSERSITE=1 "$python" -I -m pip --isolated install \
    --index-url https://pypi.org/simple \
    --disable-pip-version-check --no-compile --no-deps --only-binary=:all: --require-hashes \
    --target "$site" --upgrade --platform "$platform" --python-version 3.12 \
    --implementation cp --abi cp312 -r "$root/eng/requirements-$rid.lock" \
    --report "$stage/python/wheel-install-report.json"
if [[ "$rid" == linux-x64 ]]; then
    "$python" -I -m pip --isolated install --no-index --find-links "$numpy_build/wheels" \
        --disable-pip-version-check --no-compile --no-deps --only-binary=:all: --require-hashes \
        --target "$site" --upgrade --platform "$platform" --python-version 3.12 \
        --implementation cp --abi cp312 -r "$numpy_build/requirements.lock" \
        --report "$stage/python/numpy-wheel-install-report.json"
fi
cp "$root/eng/requirements-$rid.lock" "$stage/python/requirements.lock"
cp "$root/eng/requirements-common.lock" "$stage/python/requirements-common.lock"
if [[ "$rid" == linux-x64 ]]; then
    "$python" -I "$root/eng/configure_linux_runtime.py" "$stage/python" "$numpy_build"
fi
if [[ "$rid" == win-x64 ]]; then
    terms="$work/Visual-Studio-2022-Community-License-EN.docx"
    if [[ ! -f "$terms" ]]; then
        curl --fail --location --retry 3 \
            https://visualstudio.microsoft.com/wp-content/uploads/2021/11/Visual-Studio-2022-Community-License-EN.docx \
            --output "$terms"
    fi
    verify 41a207b10c8ab91d0d2f10a854715f73dca54509581692d2fe179aa3ffcb8540 "$terms"
    "$python" -I "$root/eng/collect_windows_terms.py" "$stage/python" "$terms"
fi
curl --fail --location --retry 3 \
    "https://raw.githubusercontent.com/astral-sh/python-build-standalone/20261026/LICENSE" \
    --output "$stage/python/PYTHON-BUILD-STANDALONE-LICENSE"
full="$work/cpython-full.tar.zst"
full_url="https://github.com/astral-sh/python-build-standalone/releases/download/20261026/cpython-3.12.16%2B20261026-$triple-${variant/+/%2B}-full.tar.zst"
if [[ ! -f "$full" ]]; then
    curl --fail --location --retry 3 "$full_url" --output "$full"
fi
verify "$full_checksum" "$full"
upstream="$(mktemp -d "$work/licenses.XXXXXX")"
# Windows' system bsdtar supports zstd without an external zstd executable.
if [[ "$host" == win-x64 ]]; then
    "$(cygpath -u "$SYSTEMROOT")/System32/tar.exe" -xf "$full" -C "$upstream" python/PYTHON.json python/licenses
else
    tar -xf "$full" -C "$upstream" python/PYTHON.json python/licenses
fi
printf '{"url":"%s","sha256":"%s"}\n' "$full_url" "$full_checksum" > "$upstream/python/provenance.json"
"$python" -I "$root/eng/collect_licenses.py" "$rid" "$stage/python" "$upstream/python" "$root"
rm -rf "$upstream"
if [[ "$host" == "$rid" ]]; then
    if [[ "$rid" == win-x64 ]]; then
        target_python="$stage/python/python.exe"
    else
        target_python="$stage/python/bin/python3"
    fi
    "$target_python" -I -m pip check
    "$target_python" -I -c 'import nltk, numpy, regex; print("Prepared NLTK", nltk.__version__, "NumPy", numpy.__version__)'
    if [[ "$rid" == linux-x64 ]]; then
        "$target_python" -I "$root/eng/check_linux_runtime.py"
    fi
else
    echo "Cross-built $rid; import/execution must be verified on the target OS."
fi
COPYFILE_DISABLE=1 tar -czf "$output/python.tar.gz" -C "$stage" python
licenses="$root/NltkNet.Runtime.$rid/runtimes/licenses"
if [[ -d "$licenses" ]]; then rm -rf "$licenses"; fi
mkdir -p "$licenses"
cp -R "$stage/python/licenses/." "$licenses/"
if command -v sha256sum >/dev/null 2>&1; then
    sha256sum "$output/python.tar.gz" | awk '{print $1}' > "$output/python.tar.gz.sha256"
else
    shasum -a 256 "$output/python.tar.gz" | awk '{print $1}' > "$output/python.tar.gz.sha256"
fi
echo "Runtime prepared in $output"
