using NltkNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TestApp
{
    class Program
    {
        static string text = "This IronPython script works fine when I run it by itself.";

        static void TestTokenize()
        {            
            List<Tuple<int, int>> tuples = Nltk.Tokenize.Util.RegexpSpanTokenize(text, "\\s");
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

            Console.WriteLine("Stem: " +stem);

            var lemmatizer = new Nltk.Stem.WordNetLemmatizer();
            Console.WriteLine("Lemmatize: " + lemmatizer.Lemmatize("best"));
        }


        private static void TestCorpus()
        {
            // NOTE: brown corpus have to be installed. By default to %appdata%\nltk_data\corpora\brown
            // See https://github.com/nrcpp/NltkNet/blob/master/NltkNet/Nltk/Nltk.Corpus.cs for more corpora
            var corpus = new Nltk.Corpus.Brown();       
            var fileids = corpus.FileIds();
            var words = corpus.Words(fileids.First());
            var sentences = corpus.Sents(fileids.First());
            var paragraphs = corpus.Paras(fileids.First());
            string text = corpus.Raw(fileids.First());
            var taggedWords = corpus.TaggedWords(fileids.First());

            var stopWordsCorpus = new Nltk.Corpus.StopWords();
            var stopWords = stopWordsCorpus.Words(null);

            // Process given 
            Console.WriteLine("Stopwords: \r\n" + string.Join(", ", stopWords));
            Console.WriteLine("Words from Brown corpus: \r\n" + string.Join(", ", words));
        }

        static void Main(string[] args)
        {
            Nltk.Init(new List<string>
            {
                @"C:\IronPython27\Lib",
                @"C:\IronPython27\Lib\site-packages",
            });

            TestTokenize();
            TestProbability();
            TestStem();
            TestCorpus();
        }
    }
}
