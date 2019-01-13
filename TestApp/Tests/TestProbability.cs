using NltkNet;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;

namespace TestApp
{
    static class TestProbability
    {
        static string text = "cow cat mouse cat tiger";

        public static void OverallTest()
        {
            // See https://www.nltk.org/_modules/nltk/probability.html for more details

            //TestFreqDist();
            TestCondFreqDist();
        }

        private static void TestFreqDist()
        {
            var words = Nltk.Tokenize.WordTokenize(text);
            var fdist = new Nltk.Probability.FreqDist(words.AsPython);

            var result = fdist.MostCommon().AsNet;

            Console.WriteLine("Most common words: ");
            foreach (var item in result)
                Console.WriteLine(item.Key + ": " + item.Value);
        }

        static void TestCondFreqDist()
        {
            var words = Nltk.Tokenize.WordTokenize(text);
            var cfdist = new Nltk.Probability.ConditionalFreqDist();

            foreach (string word in words.AsNet)
            {
                var condition = word.Length;
                cfdist[condition][word] += 1;                
            }

            foreach (var condition in cfdist.PyObject)
                foreach (var word in cfdist[condition])
                    BuiltIns.Print("Cond. frequency of " + word + " " + cfdist[condition].freq(word) + " [condition is word length =" + condition + "]");
        }
    }
}
