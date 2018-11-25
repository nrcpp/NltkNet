using IronPython.Runtime;
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

        public static List<T> List<T>(dynamic generatorObj, Func<object, T> converter)
        {
            var result = new List<T>();
            foreach (T item in (PythonGenerator)generatorObj)
                result.Add(converter(item));
            return result;
        }

        private static T Item<T>(this PythonTuple tuple, int index) => (T)tuple[index];

        public static List<Tuple<int, int>> ListTuple2(dynamic generatorObj)
        {
            List<Tuple<int, int>> result = new List<Tuple<int, int>>();
            foreach (PythonTuple item in (PythonGenerator)generatorObj)
            {
                result.Add(new Tuple<int, int>(
                    item.Item<int>(0), item.Item<int>(1)));
            }

            return result;
        }

    }
}
