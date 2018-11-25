using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NltkNet
{
    public static partial class Nltk
    {
        public static class Tokenize
        {
            public static List<string> WordTokenize(string text) => CallGetList<string>("word_tokenize", text);
            public static List<string> WordpunctTokenize(string text) => CallGetList<string>("wordpunct_tokenize", text);

            public static List<string> SentTokenize(string text) => CallGetList<string>("sent_tokenize", text);

            public static class Util
            {
                public static List<Tuple<int,int>> RegexpSpanTokenize(string text, string regexp)
                {
                    dynamic generator = Call("regexp_span_tokenize", text, regexp);
                    return ListTuple2(generator);
                }
            }
        }
    }
}
