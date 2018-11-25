using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NltkNet
{
    public static partial class Nltk
    {
        public static PythonWrapper py;

        public static void Init(List<string> libsPaths)
        {
            py = new PythonWrapper();
            py.AddLibPaths(libsPaths);
            py.ImportModule("nltk");
            py.SetDefaultModule("nltk");            
        }

        public static dynamic Call(string funcName, params dynamic[] arguments) => py.CallModuleFunction(funcName, arguments);
        public static T Call<T>(string funcName, params dynamic[] arguments) => py.CallModuleFunction<T>(funcName, arguments);
        public static List<T> CallGetList<T>(string funcName, params dynamic[] arguments) => 
            py.CallModuleFunction<IList<object>>(funcName, arguments).Cast<T>().ToList();
    }
}
