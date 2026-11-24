# Copilot instructions for NltkNet

NltkNet: C# wrappers for NLTK on .NET 10 (Python.NET + a bundled portable CPython). Projects: `NltkNet` (library),
`NltkNet.Runtime.osx-arm64` / `.win-x64` / `.linux-x64` (runtime packages), `TestApp` (examples and self-test),
`PackageSmokeTest` (consumer of the real NuGet packages), `eng/` (preparation, verification and release scripts).
Details: `README.md`.

## Branches and CI/CD

- `dev` is the default branch. Commit and merge all changes into `dev`.
- `master` is the release branch. Never push or merge into `master` unless the owner asks for a release.
- Only a push to `master` runs native CI (`runtime-packages.yml`: macOS, Windows, Linux). Pushes to `dev` and pull
  requests start no workflow runs. macOS minutes count 10x and Windows 2x against the Actions quota: don't trigger
  runs without need.
- Publishing to nuget.org is manual only (`publish.yml`, run by the owner) and accepts a successful CI run from
  `master`. Published versions can't be overwritten. Never publish, tag or create releases yourself.
- CI artifacts are kept 7 days (`retention-days`); never add `upload-artifact` without `retention-days`.

## Build and check

```sh
bash eng/prepare-runtime.sh
bash eng/prepare-data.sh
dotnet run --project TestApp -- --self-test
bash eng/verify-packages.sh
```

## Rules

- Keep runtime versions (CPython, NLTK, NumPy, Python.NET) pinned; don't change them without the owner.
- Don't change runtime license files or `PUBLISHING-REVIEW.txt` conclusions; redistribution is the owner's decision.
- Never commit keys or NuGet credentials (publishing uses Trusted Publishing, no API key).
- Report honestly: "build only" when compiled but not run; "not verified" when not checked.
