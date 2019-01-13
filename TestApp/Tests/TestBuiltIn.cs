using NltkNet;

namespace TestApp
{
    using System;
    using System.Collections.Generic;

    // Using NltkNet.BuiltIn static class to access Print, Str, Len, List, Sorted, Import and other IronPython built-in functions.
    // See https://ironpython-test.readthedocs.io/en/latest/library/functions.html for details
    using static NltkNet.BuiltIns;

    static class TestBuiltIn
    {        
        public static void OverallTest()
        {
            Nltk.Init(new List<string>()
            {
                @"C:\IronPython27\Lib",
                @"C:\IronPython27\Lib\site-packages",
            });

            TestImport();
            TestStandard();
        }


        // Using **__import__** built-in to import *nltk.corpus.brown* and *nltk.corpus.wordnet*
        public static void TestImport()
        {            
            dynamic corpuses = ImportNames("nltk.corpus", "brown", "wordnet");
            dynamic brown = corpuses.brown;
            dynamic wordnet = corpuses.wordnet;

            Print(brown.words(brown.fileids()[0]));
        }

        // Test standard python 2.7 functions (camel-case): Len,Str,List,Sorted,Range,Zip etc.
        public static void TestStandard()
        {
            var lst1 = new List<int> { 5, 4, 3, 2, 1, 5, 4, 3, 2, 1 };
            var lst2 = new List<int> { 10, 20, 30, 40, 50 };
            var lst3 = new List<string>() { "A", "B", "C", "D" };

            Print("Len: " + Len(lst1));
            Print("Sorted: " + Str(Sorted(lst1)));
            var tuple = (1, 2, "str");

            Print("Tuple2List: " + tuple);
            Print(List(tuple));
            
            Print("Set: " + Str(Set(lst1)));
            var range = Range(0, 30, 3);
            Print("Range: " + Str(List(range)));

            Print("Zip: " + Str(Zip(lst1, lst2)));

            //Print(Globals());
            Console.ReadLine();            
        }
    }
}
