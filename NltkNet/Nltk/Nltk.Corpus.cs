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
                Py.ImportModule("nltk.corpus");
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

                    Py.ExecuteScript($"from nltk.corpus import {name}");
                    CorpusObj = Py.GetVariable(name);
                }

                public NltkResultListString FileIds()
                {
                    var words = Py.CallMethod(CorpusObj, "fileids");

                    return new NltkResultListString()
                    {                        
                        AsPython = words,                     
                    };
                }

                public NltkResultListStringDynamic Words(string fileid = null)
                {
                    var words = Py.CallMethod(CorpusObj, "words", fileid);

                    return new NltkResultListStringDynamic()
                    {                        
                        AsPython = words,
                    };
                }
        

                // list of(list of str)
                public NltkResultListListString Sents(string fileid = null)
                {
                    var sents = Py.CallMethod(CorpusObj, "sents", fileid);
                   
                    return new NltkResultListListString()
                    {                        
                        AsPython = (dynamic)sents
                    };
                }


                // paras() : list of(list of (list of str))
                public NltkResultListListListString Paras(string fileid = null)
                {
                    var paras = Py.CallMethod(CorpusObj, "paras", fileid);
                    
                    return new NltkResultListListListString()
                    {
                        AsPython = paras,                        
                    };
                }

                public string Raw(string fileid) => Py.CallMethod(CorpusObj, "raw", fileid);

                public virtual NltkResultListTupleStringString TaggedWords(string fileid = null)
                {                    
                    var taggedWords = Py.CallMethod(CorpusObj, "tagged_words", fileid);
                    
                    return new NltkResultListTupleStringString()
                    {                    
                        AsPython = taggedWords,
                    };
                }
            }

            public class Brown : BaseCorpus
            {
                public Brown() : base("brown") { }


                public NltkResultListListTupleStringString TaggedSents(List<string> fileids = null, string categories = null, string tagset = null)
                {                    
                    var taggedSents = Py.CallMethod(CorpusObj, "tagged_sents", fileids, categories);
                    
                    return new NltkResultListListTupleStringString()
                    {                 
                        AsPython = taggedSents,
                    };
                }
            }

            public class Gutenberg : BaseCorpus
            {
                public Gutenberg() : base("gutenberg") { }                
            }

            public class StopWords : BaseCorpus
            {
                public StopWords() : base("stopwords") { }

                public NltkResultListStringDynamic EnglishWords() => Words("english");
            }

            public class Inaugural : BaseCorpus
            {
                public Inaugural() : base("inaugural") { }
            }

            public class WebText : BaseCorpus
            {
                public WebText() : base("webtext") { }
            }

            public class NpsChat : BaseCorpus
            {
                public NpsChat() : base("nps_chat") { }
            }

            public class Words : BaseCorpus
            {
                public Words() : base("words") { }
            }

            public class Timit : BaseCorpus
            {
                public Timit() : base("timit") { }
            }

            public class Names : BaseCorpus
            {
                public Names() : base("names") { }
            }
        }
    }
}
