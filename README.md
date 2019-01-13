# NltkNet   
[![Build Status](https://travis-ci.com/nrcpp/NltkNet.svg?branch=master)](https://travis-ci.com/nrcpp/NltkNet)
 ![NltkNet Logo](https://i.ibb.co/phKzWqj/small-icon.png)

### C# wrapper for NLTK library ([http://nltk.org](http://nltk.org))

- [Nuget package](https://www.nuget.org/packages/NltkNet/)

## **Frameworks supported**

- .NET 4.5 or later

## **Dependencies**

- [IronPython](https://www.nuget.org/packages/IronPython) - 2.7.9 or later (Includes [DynamicLanguageRuntime](https://www.nuget.org/packages/DynamicLanguageRuntime/))
- [System.ValueTuple](https://www.nuget.org/packages/System.ValueTuple) - 4.3.0 

## **Pre-Requirements**

Before start using NltkNet wrapper it is required to download and install latest IronPython binaries from [official site](http://ironpython.net/). You will need IronPython standard libraries for NLTK, as well as installing NLTK library for IronPython. Also IronPython interpreter will be helpful to test python scripts interactively from Visual Studio or command line.

It is expectable that most developers already have experience with NLTK library using Python and looking for a way to use in C#. Guides in this section are mostly for a developers who just started learning NLP using NLTK and haven&#39;t much experience with Python.

### **Installing IronPython**

- [Download](http://ironpython.net/download/) IronPython MSI package (you could also download ZIP archive)
- Run installer. By default IronPython binaries will be installed to _C:\Program Files \IronPython_ folder. _Tip:_ For your convenience IronPython could be installed to **C:\IronPython27** folder. Further, in examples below we&#39;ll use that path for initialization of path to IronPython and NLTK libs.
- Add IronPython installation path to PATH environment variable

### **Add IronPython environment to Visual Studio**

- In order to have ability to run IronPython from _within Visual Studio_ you may add IronPython environment to Python Environments there.
- Use this [documentation](https://docs.microsoft.com/en-us/visualstudio/python/managing-python-environments-in-visual-studio) for details

### **Install NLTK library for IronPython**

There are different ways to install nltk library. If you have experience with using Python and installing packages then everything is clear here.

- _From Visual Studio._ Make sure you&#39;re added IronPython to Python Environments of Visual Studio on the previous step.
  - Then go to View-&gt;Other Windows-&gt;Python Environments to open existing Python Environments.
  - Choose IronPython environment and then choose _Packages (PyPi)_ from combobox below
  - Type there nltk and choose &#39;pip install nltk&#39;
  - After installing, make sure you have installed nltk folder at &lt;IronPython Path&gt;\Libs\site-packages\nltk
- _From command line using pip._ Run &#39;pip install nltk&#39; from command line. Path to _Pip.exe_ have to be added to PATH environment variable. If you have several Python environments in system then, make sure you&#39;re installing _nltk_ library to IronPython path.
- _From binaries._ Download binaries from [https://pypi.org/project/nltk/#files](https://pypi.org/project/nltk/#files). And run installer or unpack archive to &lt;IronPythonPath&gt;\Libs\site-packages\nltk. See [https://www.nltk.org/install.html](https://www.nltk.org/install.html) for more details.

### **Install NLTK corpuses**

_Corpus_(plural _corpora_) or _text corpus_ is a large and structured set of texts (nowadays usually electronically stored and processed). In corpus linguistics, they are used to do statistical analysis and hypothesis testing, checking occurrences or validating linguistic rules within a specific language territory.

NLTK library contains lots of ready-to-use corpuses which usually stores as a set of text files. It will be useful to load certain corpus on studying NLP using NLTK library, instead of creating it from scratch.

If you&#39;re using NLTK library for learning NLP, download NLTK _book_ related corpuses and linguistic data.

Use such script either from *Visual Studio Python Interactive Window* or *Iron Python command line* to do so:

```python
import nltk 
import nltk.corpus
nltk.download('book')
```

## Getting Started

When whole third-party stuff is in-place then we are ready to test NltkNet. Install NltkNet nuget package using your usual way. For example from Package Manager Console by pasting:

```
Install-Package NltkNet
```

**Use this code to initialize paths to IronPython standard and third-party libraries:**
```C#
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NltkNet;

namespace TestApp
{
    class Program
    {        
        static void Main(string[] args)
        {
            Nltk.Init(new List<string>
            {
                @"C:\IronPython27\Lib",                 // Path to IronPython standard libraries
                @"C:\IronPython27\Lib\site-packages",   // Path to IronPython third-party libraries
            });           
        }
    }
}
```

**More practical samples**
```C#
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using NltkNet;

namespace TestApp
{
    class Program
    {
        static string text = "This IronPython script works fine when I run it by itself.";

        private static void TestNltkResultClass()
        {
            var corpus = new Nltk.Corpus.Inaugural();

            // example of using NltkResult class
            var fileidsResult = corpus.FileIds();

            // Get .NET List<string>
            List<string> fileidsNet = fileidsResult.AsNet;                          
            
            // Get IronPython.Runtime.List
            IronPython.Runtime.List fileidsPython = fileidsResult.AsPython;         
            
            // Cast to Dynamic to access object fields in Python-like style
            dynamic fileids = fileidsResult;                                        

            // using DynamicObject
            Console.WriteLine(fileids[0]);
            Console.WriteLine(fileids.__len__());

            // access sentences (list of list of strings)
            var sentencesResult = corpus.Sents();
            dynamic sentences = sentencesResult;

           // Manipulating with Python object: first word in first sentense
            Console.WriteLine(sentences[0][0]);        
            List<List<string>> netSentences = sentencesResult.AsNet;

            Console.WriteLine(netSentences[0][0]);              // the same with .NET object
            Console.WriteLine(netSentences.First().First());     // using LINQ
        }

        static void TestTokenize()
        {            
            var tuples = Nltk.Tokenize.Util.RegexpSpanTokenize(text, "\\s");

            var list = Nltk.Tokenize.SentTokenize(text).AsNet;            
            foreach (var item in list)
                Console.Write(item + ", ");
        }

        static void TestProbability()
        {
            var words = Nltk.Tokenize.WordTokenize(text);
            var fd = new Nltk.Probability.FreqDist(words.AsPython);

            var result = fd.MostCommon(null).AsNet;
            foreach (var item in result)
                Console.WriteLine(item.Key + ": " + item.Value);
        }

        static void TestStem()
        {
            var stemmer = new Nltk.Stem.PorterStemmer();
            var words = new List<string>() { "program", "programs", "programmer", "programming", "programmers" };
            var stem = stemmer.Stem("girls");

            Console.WriteLine("Stem: " +stem);

            var lemmatizer = new Nltk.Stem.WordNetLemmatizer();
            Console.WriteLine("Lemmatize: " + lemmatizer.Lemmatize("best"));
        }


        private static void TestCorpus()
        {
            // NOTE: brown corpus have to be installed. By default to %appdata%\nltk_data\corpora\brown
            // See https://github.com/nrcpp/NltkNet/blob/master/NltkNet/Nltk/Nltk.Corpus.cs for more corpora
            var corpus = new Nltk.Corpus.Brown();
            
            var fileidsResult = corpus.FileIds();
            List<string> fileidsNet = fileidsResult.AsNet;
            dynamic fileids = fileidsResult;

            Console.WriteLine(fileids[0]);

            var words = corpus.Words(fileidsNet.First());
            var sentences = corpus.Sents(fileidsNet.First());
            var paragraphs = corpus.Paras(fileidsNet.First());
            string text = corpus.Raw(fileidsNet.First());
            var taggedWords = corpus.TaggedWords(fileidsNet.First());

            var stopWordsCorpus = new Nltk.Corpus.StopWords();
            var stopWords = stopWordsCorpus.Words(null);

            // Process given 
            Console.WriteLine("Stopwords: \r\n" + string.Join(", ", stopWords));
            Console.WriteLine("Words from Brown corpus: \r\n" + string.Join(", ", words));
        }

        
        static void Main(string[] args)
        {
            Nltk.Init(new List<string>
            {
                @"C:\IronPython27\Lib",
                @"C:\IronPython27\Lib\site-packages",
            });

            TestNltkResultClass();
            TestCorpus();
            TestTokenize();
            TestProbability();
            TestStem();
        }
    }
}

```

## What if there is no required NLTK feature in wrapper?

**NltkNet** wrapper may be considered as a starting point for learning NLP using C# and Visual Studio. Current version of NltkNet does not cover lots of features of original NLTK library written in Python. You may use workarounds to execute Python code that didn't wrapped yet.
First is direct access to **Nltk.Py** property which provides you ability to execute any IronPython script, including wrappers arround method calls and creating objects. Consider the below example that illustrates possibility of using `unwrapped` features of NLTK:
```C#
using NltkNet;
using System;
using System.Collections.Generic;
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
                Console.Write(name + ", ");
        }
    }
}
```



### Other examples that uses IronPython built-in functions:

```C#
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


        // Using '__import__' built-in to import 'nltk.corpus.brown' and 'nltk.corpus.wordnet'
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

            Print(Globals());
            Console.ReadLine();            
        }
    }
}
```



