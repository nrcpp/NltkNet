using NltkNet;
using System;
using System.Linq;
using TestApp.Examples;

namespace TestApp
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            if (args.Contains("--help"))
            {
                Console.WriteLine("TestApp [--example=1|2|3] [--without-data] [--corpus=NAME] [--self-test]");
                Console.WriteLine("1: text, models and corpora; 2: statistics and classification; 3: Python interop.");
                Console.WriteLine("Extra corpora: brown, gutenberg, stopwords, inaugural, webtext, nps_chat, names, timit, words.");
                Console.WriteLine("Provision extra corpora in nltknet/data before using --corpus; no downloads occur at runtime.");
                return;
            }
            foreach (string argument in args)
                if (argument != "--self-test" && argument != "--without-data" &&
                    argument != "--example=1" && argument != "--example=2" && argument != "--example=3" &&
                    !argument.StartsWith("--corpus=", StringComparison.Ordinal))
                    throw new ArgumentException("Unknown argument: " + argument + ". Use --help.");
            string[] selected = args.Where(arg => arg.StartsWith("--example=", StringComparison.Ordinal)).ToArray();
            if (selected.Length > 1)
                throw new ArgumentException("Select only one example.");
            string[] corpora = args.Where(arg => arg.StartsWith("--corpus=", StringComparison.Ordinal)).ToArray();
            if (args.Contains("--self-test") && (selected.Length > 0 || args.Contains("--without-data")))
                throw new ArgumentException("--self-test runs the model-backed regression suite; do not combine it with example options.");
            if (corpora.Length > 0 && (args.Contains("--without-data") ||
                args.Contains("--self-test") || selected.Any(arg => arg != "--example=1")))
                throw new ArgumentException("--corpus requires example 1 with datasets enabled.");

            Nltk.Init();
            if (args.Contains("--self-test"))
            {
                RuntimeTests.Run();
                return;
            }

            if (selected.Length == 0 || selected[0] == "--example=1")
                TextExamples.Run(withData: !args.Contains("--without-data"));
            foreach (string corpus in corpora)
                TextExamples.ShowCorpus(corpus.Substring("--corpus=".Length));
            if (selected.Length == 0 || selected[0] == "--example=2")
                LearningExamples.Run();
            if (selected.Length == 0 || selected[0] == "--example=3")
                PythonExamples.Run();
        }
    }
}
