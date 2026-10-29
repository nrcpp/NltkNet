using System;
using System.Collections.Generic;
using System.IO;

namespace NltkNet
{
    public static partial class Nltk
    {
        private static readonly object InitLock = new object();
        private static PythonWrapper python;

        public static PythonWrapper Py => python ?? throw new InvalidOperationException("Call Nltk.Init() before using NltkNet.");

        public static void Init()
        {
            lock (InitLock)
            {
                if (python != null)
                    return;
                var candidate = new PythonWrapper();
                try
                {
                    candidate.ImportModule("nltk");
                    candidate.SetDefaultModule("nltk");
                    candidate.SetVariable("_nltknet_data", Path.Combine(AppContext.BaseDirectory, "nltknet", "data"));
                    candidate.ExecuteScript("import nltk\nnltk.data.path[:] = [_nltknet_data]");
                    python = candidate;
                }
                catch
                {
                    candidate.Dispose();
                    throw;
                }
            }
        }

        [Obsolete("Python is bundled. Use Init() without paths. Use Py.AddLibPaths only for additional custom Python modules.")]
        public static void Init(List<string> libsPaths)
        {
            if (libsPaths != null && libsPaths.Count != 0)
                throw new NotSupportedException("External IronPython installations are no longer supported. Use Nltk.Init() and the matching runtime NuGet package.");
            Init();
        }

        public static dynamic Call(string funcName, params dynamic[] arguments) => Py.CallModuleFunction(funcName, arguments);
        public static T Call<T>(string funcName, params dynamic[] arguments) => Py.CallModuleFunction<T>(funcName, arguments);        

        public static dynamic CreateNltkObject(string className, params dynamic[] arguments) => Py.CallModuleFunction(className, arguments);

        #region NLTK namespace methods

        public static NltkResultListTupleStringString PosTag(List<string> words)
        {
            var taggedWords = Py.CallModuleFunction("nltk", "pos_tag", words);

            return new NltkResultListTupleStringString()
            {
                AsPython = taggedWords
            };
        }

        public static dynamic NeChunk(dynamic posTaggedText)
        {
            object input = posTaggedText is NltkResultListTupleStringString result ? result.AsPython : (object)posTaggedText;
            var namedEntities = Py.CallModuleFunction("nltk", "ne_chunk", input);

            return namedEntities;
        }


        public static class NaiveBayesClassifier
        {
            public static dynamic Train(dynamic featureSet) => Classify.NaiveBayes.NaiveBayesClassifier.Train(featureSet);
        }


        public class Text : NltkClass<Text>
        {
            public Text(object pyObject) : base(pyObject)
            {
            }

            public NltkResult<string> Similar(string word)
            {
                var result = Py.CallMethod(PyObject, "similar", word);

                return new NltkResult<string>()
                {
                    AsPython = result ?? new System.Dynamic.ExpandoObject(),
                    AsNet = (string)result
                };
            }
        }

        #endregion        
    }
}
