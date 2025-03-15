﻿using IronPython.Hosting;
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
            //var taggedWords = Nltk.PosTag(words.AsNet);

            //BuiltIn.Print(taggedWords.AsPython);
            //BuiltIn.Print(taggedWords.AsPython);
            //Console.WriteLine("Length = " + BuiltIn.Len(taggedWords.AsPython));

            //var myText = new Nltk.Text(words.AsPython);
            //var r = myText.Similar("Denis");
            //BuiltIn.Print(r.AsPython);

            var tuple = Nltk.Tag.Str2Tuple("fly/NN").AsNet;
            Console.WriteLine(tuple.Item1 + " " + tuple.Item2);
        }

        static void TestNameEntityRecognizer()
        {
            string text = "WASHINGTON -- In the wake of a string of abuses by New York police officers in the 1990s, Loretta E. Lynch, "+
                         "the top federal prosecutor in Brooklyn, spoke forcefully about the pain of a broken trust that African-Americans "+
                         "felt and said the responsibility for repairing generations of miscommunication and mistrust fell to law enforcement.";

            var tokens = Nltk.Tokenize.WordTokenize(text);
            var posTaggedWords = Nltk.PosTag(tokens.AsNet);

            // NOTE: This operation requires NumPy library for IronPython
            var neChunks = Nltk.NeChunk(posTaggedWords);        

            BuiltIns.Print($"NER output for text: '{text}'");
            BuiltIns.Print(neChunks);
        }

        static void Main(string[] args)
        {
            var engine = Python.CreateEngine();
            var scope = engine.CreateScope();
            string pythonCode = @"

print('hello from ironPython')
";
            try
            {
                engine.Execute(pythonCode, scope);  
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            Nltk.Init(new List<string>
            {
                @"C:\Program Files\IronPython 3.4\Lib",
                @"C:\Program Files\IronPython 3.4\Lib\site-packages",                
            });


            //TestCorpus.TestBrown();

            //TestPosTagger();            
            //TestBuiltIn.OverallTest();

            //TestNltkResultClass();            
            //TestTokenize();
            //TestProbability.OverallTest();

            //TestClassify.TestNames();
            TestNameEntityRecognizer();
            //TestStem();

            //Workarounds.TestPurePython();
        }
    }
}
