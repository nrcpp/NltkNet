using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NltkNet
{
    public static partial class Nltk
    {
        //nltk.tag
        public static class Tag
        {
            static Tag()
            {				
                Py.ImportModule("nltk.tag");
                Py.ExecuteScript($"from nltk.tag import str2tuple");
			}

            public static NltkResultTupleStrStr Str2Tuple(string str)
            {
                var str2tuple = Py.GetVariable("str2tuple");
				
                var result = str2tuple(str);
                return new NltkResultTupleStrStr()
                {
                    AsPython = result,
                    AsNet = ValueTuple.Create((string)result[0], (string)result[1])
                };
            }
        }
    }
}
