using IronPython.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NltkNet
{
    public static partial class Nltk
    {
        public static PythonWrapper Py { get; private set; }

        public static void Init(List<string> libsPaths)
        {
            Py = new PythonWrapper();
            Py.AddLibPaths(libsPaths);
            Py.ImportModule("nltk");
            Py.SetDefaultModule("nltk");
            
            // we need to set os platform to 'win', instead of 'cli' to make it work such things as 'nltk.pos_tag(words)'
            Py.ImportModule("sys");
            Py.ExecuteScript("sys.platform = 'win32'");            
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
