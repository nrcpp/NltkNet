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


            public class ConditionalFreqDist : NltkClass<ConditionalFreqDist>
            {
                public ConditionalFreqDist() : base()
                {
                }

                public ConditionalFreqDist(string pythonExpresion) 
                {
                    if (pythonExpresion != null)
                    {
                        dynamic expr = Py.ExecuteScript(pythonExpresion);
                        PyObject = CreateNltkObject(nameof(ConditionalFreqDist), expr);
                    }
                    
                }

                int N() => PyObject.N();

                public dynamic Hapaxes() => PyObject.hapaxes();

                public void AddCondition(object conditionKey, string word)
                {
                    var pyCondition = PyObject[conditionKey];

                    pyCondition[word] += 1;                    
                }

                public dynamic this[object conditionKey]
                {
                    get => PyObject[conditionKey];
                }

                //{
                //    get
                //    {
                //        var condition = PyObject[word];
                //        if (condition == null || condition == 0)
                //            condition = PyObject[word] = new Dictionary<object, object>();

                //        return condition;
                //    }

                //    set
                //    {
                //        AddCondition((string)word, value);
                //    }
                //}

                public NltkResult MostCommon(int? number = null)
                {
                    var dict = PyObject.most_common(number);

                    return new NltkResult()
                    {
                        AsPython = dict
                    };
                }
            }
        }
    }
}
