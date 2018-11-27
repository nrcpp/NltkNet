using NltkNet;
using System;
using System.Collections.Generic;
using System.IO;

namespace TestApp
{
    class Program
    {
        static string text = File.ReadAllText("files/test.txt");

        static void TestTokenize()
        {
            //List<Tuple<int, int>> tuples = Nltk.Tokenize.Util.RegexpSpanTokenize(text, "\\s");
            var list = Nltk.Tokenize.SentTokenize(text);
            if (list != null)
                foreach (var item in list)
                    Console.Write(item + "\r\n");
        }

        static void TestProbability()
        {
            var words = Nltk.Tokenize.WordTokenize(text);
            var fd = Nltk.Probability.FreqDist.Create(words);

            var result = fd.MostCommon(null);
            foreach (var item in result)
                Console.WriteLine(item.Key + ": " + item.Value);
        }

        static void TestStem()
        {
            var stemmer = new Nltk.Stem.PorterStemmer();
            var words = new List<string>() { "program", "programs", "programmer", "programming", "programmers" };
            var stem = stemmer.Stem("girls");

            Console.WriteLine(stem);
        }

        static void Main(string[] args)
        {
            Nltk.Init(new List<string>
            {
                @"C:\IronPython27\Lib",
                @"C:\IronPython27\Lib\site-packages",
            });

            //TestTokenize();
            //TestProbability();
            TestStem();

            return;
            var py = new PythonWrapper();
            py.LoadCode("def toStr(s):\r\n\treturn s.lower();", null);
            var word = py.CallFunction("toStr", "ABCD");

            var words = Nltk.Call("word_tokenize", "this is a text");
            foreach (var w in words)
                Console.WriteLine(w + " " + (w).GetType().FullName);
        }
    }
}
