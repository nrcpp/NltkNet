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
All preview packages use version `2.0.0-preview.1` and are available on nuget.org.

```sh
dotnet add package NltkNet --version 2.0.0-preview.1
# Choose the runtime matching your deployment platform:
dotnet add package NltkNet.Runtime.osx-arm64 --version 2.0.0-preview.1
```

```csharp
using System;
using System.Linq;
using NltkNet;

Nltk.Init();

// No models or corpora required.
using var tokens = Nltk.Tokenize.WordpunctTokenize(
    "Developers build tools. Developers share tools!");
var stemmer = new Nltk.Stem.PorterStemmer();
using PythonValue ownedStemmer = stemmer.PyObject;
var normalized = tokens.AsNet
    .Where(word => word.All(char.IsLetter))
    .Select(word => stemmer.Stem(word.ToLowerInvariant()))
    .ToList();

var frequencies = new Nltk.Probability.FreqDist(normalized);
using PythonValue ownedFrequencies = frequencies.PyObject;
using var common = frequencies.MostCommon(3);
foreach (var (word, count) in common.AsNet)
    Console.WriteLine($"{word}: {count}");
// develop: 2
// tool: 2
// build: 1
```

For model-backed entity recognition, after provisioning the datasets:

```csharp
using var tokens = Nltk.Tokenize.WordTokenize("Barack Obama visited Washington.");
using var tagged = Nltk.PosTag(tokens.AsNet);
using PythonValue entities = Nltk.NeChunk(tagged);
Console.WriteLine(entities);
```

This requires `punkt_tab`, `averaged_perceptron_tagger_eng`,
`maxent_ne_chunker_tab` and `words`. The repository's
[numbered examples](https://github.com/nrcpp/NltkNet/tree/master/TestApp/Examples)
also demonstrate corpora, classification, probabilities and Python interop.

The runtime contains pinned CPython 3.12.16, NLTK 3.10.4 and NumPy 2.5.4.
Initialization discovers the runtime automatically, checks its archive hash,
and extracts it to a versioned per-user cache. It never downloads dependencies.

Models/corpora are not included. Provision the datasets needed by your
application under `<application directory>/nltknet/data/`. Missing models
raise NLTK errors. Review dataset licenses before redistributing them.

Runtime packages include original component terms and a consolidated license
under `licenses/`, also embedded in the runtime archive. See
`licenses/PUBLISHING-REVIEW.txt` for redistribution obligations; publication
does not replace downstream distributors' own license review.
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
