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
        }

        public static dynamic Call(string funcName, params dynamic[] arguments) => Py.CallModuleFunction(funcName, arguments);
        public static T Call<T>(string funcName, params dynamic[] arguments) => Py.CallModuleFunction<T>(funcName, arguments);        

        public static dynamic CreateNltkObject(string className, params dynamic[] arguments) => Py.CallModuleFunction(className, arguments);
    }
}
