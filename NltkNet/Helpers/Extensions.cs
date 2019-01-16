using IronPython.Hosting;
using IronPython.Runtime;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NltkNet
{
    public static partial class Nltk
    {
        public static IEnumerable<T> ToEnumerable<T>(this IEnumerator<T> enumerator)
        {
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }

        #region From Python To .NET types converters

        public static Dictionary<T1, T2> ToNetDictionary<T1, T2>(IronPython.Runtime.List list)
        {
            Dictionary<T1, T2> result = new Dictionary<T1, T2>();
            foreach (PythonTuple item in list)
            {
                result.Add((T1)item[0], (T2)item[1]);
            }

            return result;
        }

        public static List<T> ToNetList<T>(this IronPython.Runtime.List list)
            => list.Cast<T>().ToList(); 
        
        public static List<T> ToNetList<T>(this PythonGenerator generatorObj, Func<object, T> converter)
        {
            var result = new List<T>();
            foreach (T item in generatorObj)
                result.Add(converter(item));
            return result;
        }

        
        public static List<Tuple<T1, T2>> ToNetListTuple<T1, T2>(this PythonGenerator generatorObj)
        {
            List<Tuple<T1, T2>> result = new List<Tuple<T1, T2>>();
            foreach (PythonTuple item in generatorObj)
            {
                result.Add(new Tuple<T1, T2>((T1)item[0], (T2)item[1]));
            }

            return result;
        }

        public static List<Tuple<int, int>> ToNetListTupleIntInt(dynamic generatorObj) => ToNetListTuple<int, int>(generatorObj);

        #endregion


        #region from C# to Python extensions

        public static IronPython.Runtime.List ToIronPythonList<T>(this IEnumerable<T> list)
        {
            var result = new IronPython.Runtime.List();

            foreach (var item in list)
                result.Add(item);

            return result;
        }

        #endregion


        #region C#-only extensions

        private readonly static Random _random = new Random();

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = _random.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        #endregion
    }
}
