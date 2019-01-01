using NltkNet;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;

namespace TestApp
{
    class Workarounds
    {
        public static void TestPurePython()
        {
            // Initialization required
            Nltk.Init(new List<string>
            {
                @"C:\IronPython27\Lib",
                @"C:\IronPython27\Lib\site-packages",
            });


            // Imports NLTK corpus module
            Nltk.Py.ImportModule("nltk.corpus");

            // Import 'names' object to access corpus content
            Nltk.Py.ExecuteScript("from nltk.corpus import names");

            // Get object by name
            dynamic namesObj = Nltk.Py.GetObject("names");

            // Call object's method 'names.words()'
            dynamic namesList = Nltk.Py.CallMethod(namesObj, "words");
            
            foreach (var name in namesList)
            {
                Console.Write(name + ", ");
            }
        }
    }
}
