using NltkNet;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;

namespace TestApp
{
    class Program
    {
        static string text = "Hello colleague! Thanks for using NltkNet library. Wish you no bugs there!";


        private static void TestNltkResultClass()
        {
            var corpus = new Nltk.Corpus.Inaugural();

            // example of using NltkResult class
            var fileidsResult = corpus.FileIds();

            List<string> fileidsNet = fileidsResult.AsNet;                          // Get .NET List<string>
            IronPython.Runtime.List fileidsPython = fileidsResult.AsPython;         // Get IronPython.Runtime.List
            dynamic fileids = fileidsResult;                                        // Cast to Dynamic to access object fields in Python-like style

            // using DynamicObject
            Console.WriteLine(fileids[0]);
            Console.WriteLine(fileids.__len__());

            // access sentences (list of list of strings)
            var sentencesResult = corpus.Sents();
            dynamic sentences = sentencesResult;

            Console.WriteLine(sentences[0][0]);        // Manipulating with Python object: first word in first sentense
            List<List<string>> netSentences = sentencesResult.AsNet;

            Console.WriteLine(netSentences[0][0]);              // the same with .NET object
            Console.WriteLine(netSentences.First().First());     // using LINQ
        }


        static void TestTokenize()
        {            
            var tuples = Nltk.Tokenize.Util.RegexpSpanTokenize(text, "\\s").AsNet;

            Console.WriteLine("Nltk.Tokenize.Util.RegexpSpanTokenize:");
            foreach (var item in tuples)
                Console.Write($"({item.Item1}, {item.Item2}), ");

            var list = Nltk.Tokenize.SentTokenize(text).AsNet;            
            foreach (var item in list)
                Console.Write(item + ", ");
        }

        static void TestProbability()
        {
            var words = Nltk.Tokenize.WordTokenize(text);
            var fd = Nltk.Probability.FreqDist.Create(words.AsPython);

            var result = fd.MostCommon(null).AsNet;
            foreach (var item in result)
                Console.WriteLine(item.Key + ": " + item.Value);
        }

        static void TestStem()
        {
            var stemmer = new Nltk.Stem.PorterStemmer();
            var words = new List<string>() { "program", "programs", "programmer", "programming", "programmers" };
            var stem = stemmer.Stem("girls");

            Console.WriteLine("Stem: " +stem);

            var lemmatizer = new Nltk.Stem.WordNetLemmatizer();
            Console.WriteLine("Lemmatize: " + lemmatizer.Lemmatize("best"));
        }



        static void TestPosTagger()
        {
            var words = Nltk.Tokenize.WordTokenize(text);
            var taggedWords = Nltk.PosTag(words.AsNet);

            //StandardLibrary.Print(taggedWords.AsPython);
            StandardLibrary.Print(taggedWords.AsPython);
            Console.WriteLine("Length = " + StandardLibrary.Len(taggedWords.AsPython));
        }


        static void Main(string[] args)
        {
            Nltk.Init(new List<string>
            {
                @"C:\IronPython27\Lib",
                @"C:\IronPython27\Lib\site-packages",
            });


            //TestCorpus.TestBrown();

            TestPosTagger();

            //TestNltkResultClass();            
            //TestTokenize();
            //TestProbability();
            //TestStem();

            //Workarounds.TestPurePython();
        }
    }
}
