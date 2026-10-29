using NltkNet;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TestApp.Examples
{
    internal static class LearningExamples
    {
        private static Dictionary<string, string> Features(string word) => new()
        {
            ["last_letter"] = word[^1..].ToLowerInvariant()
        };

        internal static void Run()
        {
            Console.WriteLine("\n=== 2. Statistics and classification (no datasets) ===");
            using var tokens = Nltk.Tokenize.WordpunctTokenize("Developers build tools. Developers share tools!");
            var stemmer = new Nltk.Stem.PorterStemmer();
            using PythonValue ownedStemmer = stemmer.PyObject;
            var normalized = tokens.AsNet
                .Where(word => word.All(char.IsLetter))
                .Select(word => stemmer.Stem(word.ToLowerInvariant()))
                .ToList();
            var frequencies = new Nltk.Probability.FreqDist(normalized);
            using (PythonValue ownedFrequencies = frequencies.PyObject)
            using (var common = frequencies.MostCommon(3))
                Console.WriteLine("Frequencies: " + string.Join(", ", common.AsNet.Select(pair => $"{pair.Key}={pair.Value}")));

            var conditional = new Nltk.Probability.ConditionalFreqDist();
            using (PythonValue ownedConditional = conditional.PyObject)
            {
                conditional.AddCondition("positive", "great");
                conditional.AddCondition("positive", "great");
                conditional.AddCondition("negative", "bad");
                using PythonValue positive = conditional["positive"];
                dynamic counts = positive;
                Console.WriteLine("Conditional count: positive/great = " + counts["great"]);
                using PythonValue common = counts.most_common();
                using PythonValue hapaxes = counts.hapaxes();
                Console.WriteLine("Positive words: " + common + "; occurring once: " + hapaxes);
            }

            var training = new List<(Dictionary<string, string>, string)>
            {
                (Features("sunny"), "ends-in-y"),
                (Features("happy"), "ends-in-y"),
                (Features("rain"), "other"),
                (Features("cloud"), "other")
            };
            using PythonValue classifier = Nltk.NaiveBayesClassifier.Train(training);
            dynamic model = classifier;
            Console.WriteLine("Prediction for 'windy': " + model.classify(Features("windy")));
            using PythonValue probability = model.prob_classify(Features("windy"));
            dynamic scores = probability;
            Console.WriteLine($"P(ends-in-y | windy): {scores.prob("ends-in-y"):F3}");
            double accuracy = Nltk.Classify.Accuracy(classifier, training);
            Console.WriteLine($"Training accuracy: {accuracy:P0} (illustration only, not a held-out evaluation)");
        }
    }
}
