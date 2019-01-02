using NltkNet;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;

namespace TestApp
{
    static class TestCorpus
    {
        public static void OverallTest()
        {
            // NOTE: brown corpus have to be installed. By default to %appdata%\nltk_data\corpora\brown
            // See https://github.com/nrcpp/NltkNet/blob/master/NltkNet/Nltk/Nltk.Corpus.cs for more corpora
            var corpus = new Nltk.Corpus.Brown();

            var fileidsResult = corpus.FileIds();
            List<string> fileidsNet = fileidsResult.AsNet;
            dynamic fileids = fileidsResult;

            Console.WriteLine(fileids[0]);

            var words = corpus.Words(fileidsNet.First());
            var sentences = corpus.Sents(fileidsNet.First());
            var paragraphs = corpus.Paras(fileidsNet.First());
            string text = corpus.Raw(fileidsNet.First());
            var taggedWords = corpus.TaggedWords(fileidsNet.First());

            var stopWordsCorpus = new Nltk.Corpus.StopWords();
            var stopWords = stopWordsCorpus.Words(null);

            // Process given 
            Console.WriteLine("Stopwords: \r\n" + string.Join(", ", stopWords.AsNet));
            Console.WriteLine("Words from Brown corpus: \r\n" + string.Join(", ", words.AsNet));
        }


        public static void TestBrown()
        {
            var corpus = new Nltk.Corpus.Brown();
            var sents = corpus.TaggedSents(null, "adventure").AsNet;
            
            foreach (var s in sents)
                foreach (var item in s)
                {
                    Console.WriteLine($"({item.Item1}, {item.Item2})");
                }                
        }
    }
}
