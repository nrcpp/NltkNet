#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
case "$(uname -s)/$(uname -m)" in
    Darwin/arm64) rid=osx-arm64 ;;
    Linux/x86_64) rid=linux-x64 ;;
    MINGW*/x86_64|MSYS*/x86_64) rid=win-x64 ;;
    *) echo "Unsupported verification host." >&2; exit 1 ;;
esac
mkdir -p "$root/.artifacts"
feed="$root/.artifacts/feed"
packages="$(mktemp -d "$root/.artifacts/consumer-packages.XXXXXX")"
publish="$(mktemp -d "$root/.artifacts/NuGet проверка.XXXXXX")"
cache_home="$(mktemp -d "$root/.artifacts/cache-home.XXXXXX")"
trap 'rm -rf "$publish" "$packages" "$cache_home"' EXIT
cache="$cache_home/NltkNet/runtimes"
# A Windows process needs a native absolute path, not Git Bash's /d/... spelling.
if [[ "$rid" == win-x64 ]]; then cache="$(cygpath -m "$cache")"; fi

dotnet pack "$root/NltkNet/NltkNet.csproj" -c Release -o "$feed"
dotnet pack "$root/NltkNet.Runtime.$rid/NltkNet.Runtime.$rid.csproj" -c Release -o "$feed"
dotnet restore "$root/PackageSmokeTest/PackageSmokeTest.csproj" \
    --configfile "$root/PackageSmokeTest/NuGet.Config" \
    --packages "$packages" --force --no-cache
dotnet publish "$root/PackageSmokeTest/PackageSmokeTest.csproj" \
    -c Release --no-restore -p:RestorePackagesPath="$packages" -o "$publish"

dotnet_path="$(command -v dotnet)"
cd /
env -u PYTHONHOME -u PYTHONPATH -u VIRTUAL_ENV \
    PYTHONHOME=/does-not-exist PYTHONPATH=/does-not-exist \
    "$dotnet_path" "$publish/PackageSmokeTest.dll" --parallel-init

run_consumer() {
    if command -v sandbox-exec >/dev/null 2>&1; then
        sandbox-exec -p '(version 1)(allow default)(deny network*)' \
            "$dotnet_path" "$publish/PackageSmokeTest.dll" "$@"
    else
        "$dotnet_path" "$publish/PackageSmokeTest.dll" "$@"
    fi
}
if ! command -v sandbox-exec >/dev/null 2>&1; then
    echo "NOTE: OS-level network denial is not available here; this run does not certify offline behavior."
fi
run_consumer
NLTKNET_CACHE_DIRECTORY="$cache" run_consumer --parallel-init --check-isolated-cache &
first=$!
NLTKNET_CACHE_DIRECTORY="$cache" run_consumer --parallel-init --check-isolated-cache &
second=$!
status=0
wait "$first" || status=$?
wait "$second" || status=$?
if [[ "$status" != 0 ]]; then
    exit "$status"
fi
if [[ ! -d "$root/.artifacts/nltk-data" ]]; then
    echo "Prepare test datasets first: bash eng/prepare-data.sh" >&2
    exit 1
fi

mkdir -p "$publish/nltknet/data"
cp -R "$root/.artifacts/nltk-data/." "$publish/nltknet/data/"
chmod -R go-w "$publish/nltknet/data"
run_consumer --with-data

runtime="$publish/nltknet/runtime/$rid/python.tar.gz"
cp "$runtime.sha256" "$runtime.sha256.original"
printf '%064d\n' 0 > "$runtime.sha256"
"$dotnet_path" "$publish/PackageSmokeTest.dll" --expect-integrity-error
mv "$runtime.sha256.original" "$runtime.sha256"
mv "$runtime" "$runtime.hidden"
"$dotnet_path" "$publish/PackageSmokeTest.dll" --expect-missing-runtime
echo "PASS: $rid NuGet packages, publish, Unicode paths, unrelated cwd, concurrent extraction, models and diagnostics."
