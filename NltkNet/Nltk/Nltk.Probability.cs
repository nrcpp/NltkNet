using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NltkNet
{
    public static partial class Nltk
    {
        public static class Probability
        {
            public class FreqDist : NltkClass<FreqDist>
            {                
                public FreqDist(object samples) : base(samples)
                {                    
                }
                
                public NltkResultDictionaryStringInt MostCommon(int? number = null)
                {
                    var dict = PyObject.most_common(number);
                    
                    return new NltkResultDictionaryStringInt()
                    {                        
                        AsPython = dict
                    };
                }
            }


            public class CondFreqDist : NltkClass<FreqDist>
            {
                public CondFreqDist(object condition = null) : base(condition)
                {
                }

                int N() => PyObject.N();

                public dynamic this[object conditionValue]
                {
                    get => PyObject[conditionValue];
                    set => PyObject[conditionValue] = value;
                }

                public NltkResultDictionaryStringInt MostCommon(int? number = null)
                {
                    var dict = PyObject.most_common(number);

                    return new NltkResultDictionaryStringInt()
                    {
                        AsPython = dict
                    };
                }
            }
        }
    }
}
