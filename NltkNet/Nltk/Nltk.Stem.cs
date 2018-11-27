using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NltkNet
{
    public static partial class Nltk
    {
        public static class Stem
        {
            public static IronPython.Runtime.List ToPython<T>(IEnumerable<T> list)
            {
                var result = new IronPython.Runtime.List();
                
                foreach (var item in list)
                    result.Add(item);
                
                return result;
            }

            public class PorterStemmer : NltkClass<PorterStemmer>
            {                                
                public string Stem(string word) => PyObject.stem(word);
            }
        }
    }
}
