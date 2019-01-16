using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NltkNet
{
    public partial class Nltk
    {
        public static class Classify
        {
            public readonly static dynamic Module;

            static Classify()
            {
                Module = BuiltIns.ImportNames("nltk.classify");
            }

            public static class NaiveBayes
            {
                public readonly static dynamic Module;

                static NaiveBayes()
                {
                    Module = BuiltIns.ImportNames("nltk.classify.naivebayes");
                }

                public static class NaiveBayesClassifier
                {
                    public readonly static dynamic Class = BuiltIns.ImportNames("nltk.classify.naivebayes", "NaiveBayesClassifier").NaiveBayesClassifier;

                    public static dynamic Train(dynamic featureSet) => Class.train(featureSet);
                }
            }
        }        
    }
}
