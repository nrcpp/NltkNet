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

            public static class Brown
            {
                static dynamic _brownObj;
                static Brown()
                {
                    py.LoadCode("from nltk.corpus import brown", null);
                    _brownObj = py.GetVariable("brown");
                }

                public static List<string> FileIds()
                {
                    var words = py.CallMethod(_brownObj, "fileids");

                    var result = new List<string>();
                    foreach( var fi in words)
                        result.Add((string)fi);

                    return result;
                }

                public static List<string> Words(string fileid)
                {                    
                    var words = py.CallMethod(_brownObj, "words", fileid);

                    var result = new List<string>();
                    foreach (var w in words)
                        result.Add((string)w);

                    return result;
                }


                // list of(list of str)
                public static List<List<string>> Sents(string fileid)
                {
                    var sents = py.CallMethod(_brownObj, "sents", fileid);

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
                public static List<List<List<string>>> Paras(string fileid)
                {
                    var paras = py.CallMethod(_brownObj, "paras", fileid);

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

                public static string Raw(string fileid) => py.CallMethod(_brownObj, "raw", fileid);
            }
        }
    }
}
