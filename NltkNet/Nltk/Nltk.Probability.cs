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

                public static FreqDist Create(dynamic samples) => new FreqDist(samples);

                public NltkResultDictionaryStringInt MostCommon(int? number)
                {
                    var dict = PyObject.most_common(number);
                    var result = ToDictionary<string, int>(dict);

                    return new NltkResultDictionaryStringInt()
                    {
                        AsNet = result,
                        AsPython = dict
                    };
                }
            }
        }
    }
}
