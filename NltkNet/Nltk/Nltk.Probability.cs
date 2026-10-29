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

                public ConditionalFreqDist(string pythonExpresion) : base()
                {
                    if (pythonExpresion != null)
                    {
                        dynamic expr = Py.Evaluate(pythonExpresion);
                        PyObject = CreateNltkObject(nameof(ConditionalFreqDist), expr);
                    }
                    
                }

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
