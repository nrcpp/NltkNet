using NltkNet;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestApp.Examples
{
    internal static class PythonExamples
    {
        internal static void Run()
        {
            Console.WriteLine("\n=== 3. Python interop (no datasets) ===");
            using var scope = new PythonWrapper();
            scope.SetVariable("document", new Dictionary<string, object>
            {
                ["title"] = "NLTK from C#",
                ["scores"] = new[] { 2, 4, 6 }
            });
            scope.ExecuteScript("""
                def average(document):
                    return sum(document['scores']) / len(document['scores'])

                class Counter:
                    def __init__(self, value):
                        self.value = value
                    def increment(self, amount):
                        self.value += amount
                        return self.value
                """);
            using PythonValue document = scope.GetVariable("document");
            double average = scope.CallFunction("average", document);
            Console.WriteLine($"Managed dictionary/array -> Python function -> double: {average}");
            Console.WriteLine("Expression: " + scope.Evaluate("document['title'].upper()"));

            using PythonValue counter = scope.CreateObject("Counter", 10);
            dynamic instance = counter;
            instance.value = 20;
            Console.WriteLine("Python object attribute/method: " + instance.increment(5));

            using PythonValue dictionary = BuiltIns.Dict();
            dynamic values = dictionary;
            values["library"] = "NLTK";
            Console.WriteLine("Python dictionary index: " + values["library"]);
            using PythonValue range = BuiltIns.Range(1, 6, 2);
            using PythonValue list = BuiltIns.List(range);
            Console.WriteLine($"Built-ins: {BuiltIns.Str(list)}, length = {BuiltIns.Len(list)}");
            foreach (object value in list)
                Console.WriteLine("Iterated Python value: " + value);
            using PythonValue unique = BuiltIns.Set(new List<string> { "b", "a", "b" });
            using PythonValue sorted = BuiltIns.Sorted(unique);
            using PythonValue zipped = BuiltIns.Zip(new[] { "first", "second" }, new[] { 1, 2 });
            using PythonValue pairs = BuiltIns.List(zipped);
            Console.WriteLine("Set/sorted: " + sorted + "; zip: " + pairs);

            using PythonValue bigrams = Nltk.Call("bigrams", new List<string> { "C#", "and", "Python" });
            using PythonValue bigramList = BuiltIns.List(bigrams);
            Console.WriteLine("Generic NLTK call: " + bigramList);
            using PythonValue tokenizer = Nltk.CreateNltkObject("RegexpTokenizer", @"\w+");
            using PythonValue words = Nltk.Py.CallMethod(tokenizer, "tokenize", "No model, just words!");
            Console.WriteLine("Generic NLTK object: " + words);
            scope.ImportModule("math");
            Console.WriteLine("Module function: sqrt(81) = " + scope.CallModuleFunction("math", "sqrt", 81));

            var lengths = new int[4];
            Parallel.For(0, lengths.Length, index =>
            {
                using var tokens = Nltk.Tokenize.WordpunctTokenize("parallel call " + index);
                lengths[index] = tokens.AsNet.Count;
            });
            Console.WriteLine("Parallel calls (GIL acquired internally): " + string.Join(", ", lengths));
        }
    }
}
