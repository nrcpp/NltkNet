using IronPython.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NltkNet
{
    public static partial class Nltk
    {
        public static class Corpus
        {
            static Corpus()
            {
                py.ImportModule("nltk.corpus");
            }

            /// <summary>
            /// class nltk.corpus.util.LazyCorpusLoader(name, reader_cls, *args, **kwargs)
            /// </summary>
            public class LazyCorpusLoader : NltkClass<LazyCorpusLoader>
            {
                public LazyCorpusLoader(string name, object reader_cls, params object[] args) : base(name, reader_cls, args)
                {
                }

                // TODO:
            }

            public class BaseCorpus
            {
                public string CorpusName { get; }
                public dynamic CorpusObj { get; }

                protected BaseCorpus(string name)
                {
                    CorpusName = name;

                    py.LoadCode($"from nltk.corpus import {name}", null);
                    CorpusObj = py.GetVariable(name);
                }

                public List<string> FileIds()
                {
                    var words = py.CallMethod(CorpusObj, "fileids");

                    var result = new List<string>();
                    foreach (var fi in words)
                        result.Add((string)fi);

                    return result;
                }

                public List<string> Words(string fileid)
                {
                    var words = py.CallMethod(CorpusObj, "words", fileid);

                    var result = new List<string>();
                    foreach (var w in words)
                        result.Add((string)w);

                    return result;
                }


                // list of(list of str)
                public List<List<string>> Sents(string fileid)
                {
                    var sents = py.CallMethod(CorpusObj, "sents", fileid);

                    var result = new List<List<string>>();
                    foreach (var s in sents)
                    {
                        var words = new List<string>();
                        foreach (var w in s)
                            words.Add((string)w);

                        result.Add(words);
                    }

                    return result;
                }

                // paras() : list of(list of (list of str))
                public List<List<List<string>>> Paras(string fileid)
                {
                    var paras = py.CallMethod(CorpusObj, "paras", fileid);

                    var result = new List<List<List<string>>>();
                    foreach (var p in paras)
                    {
                        var sents = new List<List<string>>();
                        foreach (var s in p)
                        {
                            var words = new List<string>();
                            foreach (var w in s)
                                words.Add((string)w);

                            sents.Add(words);
                        }

                        result.Add(sents);
                    }

                    return result;
                }

                public string Raw(string fileid) => py.CallMethod(CorpusObj, "raw", fileid);

                public virtual List<Tuple<string, string>> TaggedWords(string fileid)
                {
                    List<Tuple<string, string>> result = new List<Tuple<string, string>>();

                    var taggedWords = py.CallMethod(CorpusObj, "tagged_words", fileid);
                    foreach (var tw in taggedWords)
                    {
                        var wordTag = new Tuple<string, string>(tw[0], tw[1]);
                        result.Add(wordTag);
                    }

                    return result;
                }
            }

            public class Brown : BaseCorpus
            {
                public Brown() : base("brown") { }                
            }

            public class Gutenberg : BaseCorpus
            {
                public Gutenberg() : base("gutenberg") { }                
            }

            public class WordNet : BaseCorpus
            {
                public WordNet() : base("wordnet") { }

                public new List<string> Words(string lang = "eng")
                {
                    var words = (IEnumerator<dynamic>)py.CallMethod(CorpusObj, "words", lang);

                    var result = new List<string>();

                    while (words.MoveNext())
                    {
                        result.Add((string)words.Current);
                    }

                    return result;
                }
            }
        }
    }
}
