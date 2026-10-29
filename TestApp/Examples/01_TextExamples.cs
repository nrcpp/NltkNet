using NltkNet;
using System;
using System.Linq;

namespace TestApp.Examples
{
    internal static class TextExamples
    {
        internal static void Run(bool withData)
        {
            Console.WriteLine("\n=== 1. Text analysis ===");
            const string text = "Barack Obama visited Washington. Dogs are running in the park!";
            using var punctuation = Nltk.Tokenize.WordpunctTokenize(text);
            Console.WriteLine("Model-free tokens: " + string.Join(" | ", punctuation.AsNet));

            using var spans = Nltk.Tokenize.Util.RegexpSpanTokenize(text, @"\s+");
            Console.WriteLine("Token spans: " + string.Join(", ",
                spans.AsNet.Select(span => $"[{span.Item1},{span.Item2}): {text[span.Item1..span.Item2]}")));

            var stemmer = new Nltk.Stem.PorterStemmer();
            using (PythonValue ownedStemmer = stemmer.PyObject)
                foreach (string word in new[] { "running", "studies", "programming" })
                    Console.WriteLine($"Stem: {word} -> {stemmer.Stem(word)}");

            using var tag = Nltk.Tag.Str2Tuple("Washington/NNP");
            Console.WriteLine($"Tag conversion: {tag.AsNet.Item1} / {tag.AsNet.Item2}");

            if (!withData)
            {
                Console.WriteLine("Model-backed examples disabled by --without-data.");
                return;
            }

            using var sentences = Nltk.Tokenize.SentTokenize(text);
            Console.WriteLine("Sentences: " + string.Join(" | ", sentences.AsNet));
            using var tokens = Nltk.Tokenize.WordTokenize(text);
            using var tagged = Nltk.PosTag(tokens.AsNet);
            Console.WriteLine("POS: " + string.Join(" ", tagged.AsNet.Select(pair => $"{pair.Item1}/{pair.Item2}")));
            using PythonValue entities = Nltk.NeChunk(tagged);
            Console.WriteLine("Named entities:\n" + entities);

            var lemmatizer = new Nltk.Stem.WordNetLemmatizer();
            using (PythonValue ownedLemmatizer = lemmatizer.PyObject)
                Console.WriteLine("Lemma: dogs -> " + lemmatizer.Lemmatize("dogs"));

            ShowCorpus("words");
        }

        internal static void ShowCorpus(string name)
        {
            Nltk.Corpus.BaseCorpus corpus = name switch
            {
                "words" => new Nltk.Corpus.Words(),
                "brown" => new Nltk.Corpus.Brown(),
                "gutenberg" => new Nltk.Corpus.Gutenberg(),
                "stopwords" => new Nltk.Corpus.StopWords(),
                "inaugural" => new Nltk.Corpus.Inaugural(),
                "webtext" => new Nltk.Corpus.WebText(),
                "nps_chat" => new Nltk.Corpus.NpsChat(),
                "names" => new Nltk.Corpus.Names(),
                "timit" => new Nltk.Corpus.Timit(),
                _ => throw new ArgumentException("Unknown corpus: " + name + ". Use --help.")
            };
            using PythonValue ownedCorpus = corpus.CorpusObj;
            using var fileIds = corpus.FileIds();
            string file = name == "stopwords" ? "english" : fileIds.AsNet.First();
            if (name == "timit")
            {
                // TIMIT takes utterance IDs, not annotation filenames.
                string utterance = fileIds.AsNet.First(id => id.EndsWith(".wrd", StringComparison.Ordinal))[..^4];
                using var utteranceWords = corpus.Words(utterance);
                using var utteranceSentences = corpus.Sents(utterance);
                Console.WriteLine($"Corpus timit: {fileIds.AsNet.Count} files; utterance: {utterance}");
                Console.WriteLine("Words: " + string.Join(" ", utteranceWords.AsNet));
                Console.WriteLine("Sentences: " + utteranceSentences.AsNet.Count);
                return;
            }
            Console.WriteLine($"Corpus {name}: {fileIds.AsNet.Count} files; sample file: {file}");
            using var words = corpus is Nltk.Corpus.StopWords stopWords ? stopWords.EnglishWords() : corpus.Words(file);
            Console.WriteLine("Words: " + string.Join(", ", words.AsNet.Take(12)));
            string raw = corpus.Raw(file);
            Console.WriteLine("Raw: " + raw[..Math.Min(raw.Length, 100)].Replace('\n', ' '));

            if (name is "brown" or "gutenberg" or "inaugural" or "webtext")
            {
                using var sentences = corpus.Sents(file);
                using var paragraphs = corpus.Paras(file);
                Console.WriteLine($"Sentences: {sentences.AsNet.Count}; paragraphs: {paragraphs.AsNet.Count}");
            }
            if (name is "brown" or "nps_chat")
            {
                using var tagged = corpus.TaggedWords(file);
                Console.WriteLine("Tagged: " + string.Join(" ", tagged.AsNet.Take(8).Select(pair => $"{pair.Item1}/{pair.Item2}")));
            }
            if (corpus is Nltk.Corpus.Brown brown)
            {
                using var taggedSentences = brown.TaggedSents(new() { file });
                Console.WriteLine("Tagged sentences: " + taggedSentences.AsNet.Count);
            }
        }
    }
}
