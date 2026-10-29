namespace NltkNet
{
    public static partial class Nltk
    {
        public static class Stem
        {          
            public class PorterStemmer : NltkClass<PorterStemmer>
            {                                
                public string Stem(string word) => PyObject.stem(word);
            }

            public class WordNetLemmatizer : NltkClass<WordNetLemmatizer>
            {
                public string Lemmatize(string word) => PyObject.lemmatize(word);
            }
        }
    }
}
