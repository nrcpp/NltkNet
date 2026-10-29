using System;
using System.Collections.Generic;
using System.Linq;

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

        public static Dictionary<T1, T2> ToNetDictionary<T1, T2>(PythonValue list)
        {
            Dictionary<T1, T2> result = new Dictionary<T1, T2>();
            foreach (dynamic item in list)
            {
                result.Add((T1)item[0], (T2)item[1]);
            }

            return result;
        }

        public static List<T> ToNetList<T>(this PythonValue list)
            => list.Cast<T>().ToList(); 
        
        public static List<T> ToNetList<T>(this PythonValue generatorObj, Func<object, T> converter)
        {
            var result = new List<T>();
            foreach (object item in generatorObj)
                result.Add(converter(item));
            return result;
        }

        
        public static List<Tuple<T1, T2>> ToNetListTuple<T1, T2>(this PythonValue generatorObj)
        {
            List<Tuple<T1, T2>> result = new List<Tuple<T1, T2>>();
            foreach (dynamic item in generatorObj)
            {
                result.Add(new Tuple<T1, T2>((T1)item[0], (T2)item[1]));
            }

            return result;
        }

        public static List<Tuple<int, int>> ToNetListTupleIntInt(dynamic generatorObj) => ToNetListTuple<int, int>(generatorObj);

        #endregion


        #region from C# to Python extensions

        public static PythonValue ToPythonList<T>(this IEnumerable<T> list)
        {
            return BuiltIns.List(list);
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
