using NltkNet;
using System;
using System.Collections.Generic;
using System.IO;

namespace TestApp
{
    class Program
    {
        static string text = File.ReadAllText("files/test.txt");

        static void TestTokenize()
        {
            List<Tuple<int, int>> tuples = Nltk.Tokenize.Util.RegexpSpanTokenize(text, "\\s");
            
            return;

            var list = Nltk.Tokenize.SentTokenize(text);
            if (list != null)
                foreach (var item in list)
                    Console.Write(item + "\r\n");
        }

        static void Main(string[] args)
        {
            Nltk.Init(new List<string>
            {
                @"C:\IronPython27\Lib",
                @"C:\IronPython27\Lib\site-packages",
            });

            TestTokenize();
        }
    }
}
