using NltkNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestApp
{
    internal static class RuntimeTests
    {
        private static void Check(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException("Runtime test failed: " + message);
        }

        internal static void Run(bool withData = true)
        {
            var original = Nltk.Py;
            Parallel.For(0, 8, _ => Nltk.Init());
            Check(ReferenceEquals(original, Nltk.Py), "Init must be idempotent");
            Check(Nltk.Py.Evaluate("__import__('nltk').__version__") == "3.10.4", "NLTK version");
            Check(Nltk.Py.Evaluate("__import__('sys').version_info[:2] == (3, 12)"), "CPython version");
            string expectedPlatform = OperatingSystem.IsWindows() ? "win32" : OperatingSystem.IsLinux() ? "linux" : "darwin";
            Check(Nltk.Py.Evaluate("__import__('sys').platform") == expectedPlatform, "actual OS platform");
            Check(Nltk.Py.Evaluate("__import__('sys').getfilesystemencoding()") == "utf-8", "Unicode filesystem paths in a C locale");
            string prefix = Nltk.Py.Evaluate("__import__('sys').prefix");
            Check(prefix.Replace('\\', '/').Contains("NltkNet/runtimes/"), "packaged interpreter, not system Python: " + prefix);
            Check(Nltk.Py.Evaluate("'/does-not-exist' not in __import__('sys').path"), "external Python environment is ignored");
            using (var scope = new PythonWrapper())
            {
                scope.ExecuteScript("import numpy as np\n");
                Check(scope.Evaluate("bool(np.allclose(np.dot([[1., 2.], [3., 4.]], [[2.], [1.]]), [[4.], [10.]]))"), "NumPy matrix multiplication");
                Check(scope.Evaluate("bool(np.allclose(np.linalg.solve([[3., 1.], [1., 2.]], [9., 8.]), [2., 3.]))"), "NumPy linear solver");
                Check(scope.Evaluate("bool(np.allclose(np.linalg.svd([[3., 0.], [0., 2.]], compute_uv=False), [3., 2.]))"), "NumPy SVD");
                if (OperatingSystem.IsLinux())
                {
                    Check(scope.Evaluate("__import__('importlib.util', fromlist=['find_spec']).find_spec('_dbm') is None"), "Berkeley DB backend is excluded on Linux");
                    scope.ExecuteScript("import tempfile, shelve, os\nwith tempfile.TemporaryDirectory() as directory:\n    path = os.path.join(directory, 'shelf')\n    with shelve.open(path) as shelf:\n        shelf['nltk'] = ['works', 42]\n    with shelve.open(path) as shelf:\n        assert shelf['nltk'] == ['works', 42]\n");
                }
            }

            using (var result = Nltk.Tokenize.WordpunctTokenize("Hello, world!"))
            {
                Check(result.AsNet.SequenceEqual(new[] { "Hello", ",", "world", "!" }), "token list conversion");
                dynamic forwarded = result;
                Check(forwarded[0] == "Hello" && forwarded.__len__() == 4, "dynamic result forwarding");
            }
            Check(new Nltk.Stem.PorterStemmer().Stem("programming") == "program", "stemming");
            using (var spans = Nltk.Tokenize.Util.RegexpSpanTokenize("one two", "\\s"))
                Check(spans.AsNet.SequenceEqual(new[] { Tuple.Create(0, 3), Tuple.Create(4, 7) }), "generator conversion");
            Check(Nltk.Tag.Str2Tuple("fly/NN").AsNet == ("fly", "NN"), "tuple conversion");
            Check(BuiltIns.Len(new[] { 1, 2, 3 }) == 3, "managed arrays");
            Check(BuiltIns.Str(BuiltIns.List(BuiltIns.Range(1, 5, 2))) == "[1, 3]", "Python 3 range");
            Check(BuiltIns.Str(BuiltIns.List(BuiltIns.Zip(new[] { 1, 2 }, new[] { "a", "b" }))) == "[(1, 'a'), (2, 'b')]", "zip");
            Check(BuiltIns.ImportNames("nltk.tokenize").__name__ == "nltk.tokenize", "dotted module import");
            using (var scope = new PythonWrapper())
            {
                scope.ExecuteScript("def double(value):\n    return value * 2\nclass Sample:\n    def __init__(self):\n        self.name = 'before'\n");
                Check(scope.CallFunction("double", 3) == 6, "scope functions");
                dynamic instance = scope.CreateObject("Sample");
                instance.name = "after";
                Check(instance.name == "after", "Python attributes");
                scope.SetVariable("items", new Dictionary<string, object> { ["nested"] = new[] { 1, 2 } });
                Check(scope.Evaluate("items['nested'][1]") == 2, "nested managed containers");
                Check(scope.Evaluate("None") == null && scope.Evaluate("True") == true, "scalar conversions");
                Check(scope.Evaluate("3.5") == 3.5 && scope.Evaluate("2147483648") == 2147483648L, "numeric conversions");
            }

            using (PythonValue dictionary = BuiltIns.Dict())
            {
                dynamic value = dictionary;
                value["name"] = "NLTK";
                Check(value["name"] == "NLTK", "dictionary access");
            }
            var distribution = new Nltk.Probability.FreqDist(new[] { "a", "b", "a" });
            Check(distribution.MostCommon().AsNet["a"] == 2, "frequency distribution");
            var conditional = new Nltk.Probability.ConditionalFreqDist();
            conditional.AddCondition("category", "word");
            Check(conditional["category"]["word"] == 1, "conditional frequency distribution");

            var training = new List<(Dictionary<string, string>, string)>
            {
                (new Dictionary<string, string> { ["letter"] = "a" }, "vowel"),
                (new Dictionary<string, string> { ["letter"] = "e" }, "vowel"),
                (new Dictionary<string, string> { ["letter"] = "b" }, "consonant")
            };
            dynamic classifier = Nltk.NaiveBayesClassifier.Train(training);
            Check(classifier.classify(new Dictionary<string, string> { ["letter"] = "a" }) == "vowel", "classifier with managed tuples and dictionaries");
            Check(Nltk.Classify.Accuracy(classifier, training) > 0.9, "classifier accuracy");

            Parallel.For(0, 32, i =>
            {
                using var result = Nltk.Tokenize.WordpunctTokenize("thread " + i);
                Check(result.AsNet.Count == 2, "parallel calls and GIL");
            });

            bool missingModuleReported = false;
            try { Nltk.Py.ImportModule("_nltknet_missing_module_"); }
            catch (Python.Runtime.PythonException ex) { missingModuleReported = ex.Message.Contains("_nltknet_missing_module_"); }
            Check(missingModuleReported, "Python exceptions must propagate");

            if (withData)
            {
                Check(Nltk.Tokenize.SentTokenize("Hello world. This is NLTK.").AsNet.Count == 2, "sentence tokenization");
                Check(new Nltk.Stem.WordNetLemmatizer().Lemmatize("dogs") == "dog", "lemmatization");
                var tokens = Nltk.Tokenize.WordTokenize("Barack Obama visited Washington.").AsNet;
                var tagged = Nltk.PosTag(tokens);
                Check(tagged.AsNet.Count == tokens.Count && tagged.AsNet.All(pair => pair.Item2.Length > 0), "POS tagging");
                dynamic tree = Nltk.NeChunk(tagged.AsPython);
                Check(tree.label() == "S", "NER tree");
                Check(BuiltIns.Str(tree).Contains("PERSON") && BuiltIns.Str(tree).Contains("GPE"), "NER labels");
                var corpus = new Nltk.Corpus.Words();
                Check(corpus.FileIds().AsNet.Count > 0 && corpus.Words().AsNet.Contains("hello"), "corpus conversions");
            }
            else
            {
                bool missingModelReported = false;
                try { Nltk.Tokenize.SentTokenize("No model available."); }
                catch (Python.Runtime.PythonException ex) { missingModelReported = ex.Message.Contains("punkt_tab"); }
                Check(missingModelReported, "missing models must fail explicitly without downloading");
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Check(Nltk.Tokenize.WordpunctTokenize("still alive").AsNet.Count == 2, "object lifetime after GC");
            Console.WriteLine("PASS: CPython, NLTK, conversions, classification, GIL, errors" + (withData ? ", models, POS and NER." : " (without data)."));
        }
    }
}
