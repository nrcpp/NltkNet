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

        public static dynamic CreateNltkObject(string className, params dynamic[] arguments) => py.CallModuleFunction(className, arguments);


        public static List<T> List<T>(dynamic generatorObj, Func<object, T> converter)
        {
            var result = new List<T>();
            foreach (T item in (PythonGenerator)generatorObj)
                result.Add(converter(item));
            return result;
        }

        private static T Item<T>(this PythonTuple tuple, int index) => (T)tuple[index];

        public static List<Tuple<T1, T2>> ListTuple2<T1, T2>(dynamic generatorObj)
        {
            List<Tuple<T1, T2>> result = new List<Tuple<T1, T2>>();
            foreach (PythonTuple item in (PythonGenerator)generatorObj)
            {
                result.Add(new Tuple<T1, T2>(
                    item.Item<T1>(0), item.Item<T2>(1)));
            }

            return result;
        }

        public static Dictionary<T1, T2> ListDictionary<T1, T2>(dynamic list)
        {
            Dictionary<T1, T2> result = new Dictionary<T1, T2>();
            foreach (PythonTuple item in (IronPython.Runtime.List)list)
            {
                result.Add(item.Item<T1>(0), item.Item<T2>(1));
            }

            return result;
        }

        public static List<Tuple<int, int>> ListTupleIntInt(dynamic generatorObj) => ListTuple2<int, int>(generatorObj);


        public class NltkClass<T> 
        {
            public dynamic PyObject { get; protected set; }

            protected NltkClass(params object[] args)
            {
                PyObject = CreateNltkObject(typeof(T).Name, args);
            }
        }
    }
}
