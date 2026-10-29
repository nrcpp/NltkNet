# NltkNet 2.0 preview

.NET 10 wrappers for modern NLTK using Python.NET, without system Python or
IronPython installations.

Reference both `NltkNet` and the matching platform runtime package:
`NltkNet.Runtime.osx-arm64` (macOS 14+), `NltkNet.Runtime.win-x64` (Windows x64),
or `NltkNet.Runtime.linux-x64` (Linux x64 with glibc 2.28+).
Alpine/musl and other architectures are not included.
The Linux runtime excludes the Berkeley DB `_dbm` backend and uses a custom
NumPy build without external BLAS/Fortran libraries. Other dbm/shelve backends
remain available; matrix-heavy operations may be slower.
All preview packages use version `2.0.0-preview.1` and are built locally;
they are not published to nuget.org.

```csharp
using NltkNet;

Nltk.Init();
using var tokens = Nltk.Tokenize.WordpunctTokenize("Hello, world!");
Console.WriteLine(string.Join(" | ", tokens.AsNet));
Console.WriteLine(new Nltk.Stem.PorterStemmer().Stem("programming"));
```

The runtime contains pinned CPython 3.12.16, NLTK 3.10.4 and NumPy 2.5.4.
Initialization discovers the runtime automatically, checks its archive hash,
and extracts it to a versioned per-user cache. It never downloads dependencies.

Models/corpora are not included. Provision the datasets needed by your
application under `<application directory>/nltknet/data/`. Missing models
raise NLTK errors. Review dataset licenses before redistributing them.

Runtime packages include original component terms and a consolidated license
under `licenses/`, also embedded in the runtime archive. See
`licenses/PUBLISHING-REVIEW.txt` for unresolved redistribution obligations;
these local previews are not yet cleared for public release.
Windows includes Microsoft runtime DLLs under their own terms, not MIT.
Distributors must comply with those terms; see the included Windows review.

This is a breaking migration from IronPython: use `Nltk.Init()` without paths;
`AsPython` now contains a GIL-aware `PythonValue` instead of IronPython types.
Dispose owned results when finished. Dynamic operations acquire the GIL.
The CPython interpreter is process-global and remains alive for the process
lifetime. Do not dispose the shared `Nltk.Py` scope.

See the [repository documentation](https://github.com/nrcpp/NltkNet#readme)
for preparation scripts, NuGet packaging/verification, supported features,
dataset deployment, and migration limitations.
