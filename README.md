# NltkNet

C# wrapper for the NLTK library ([https://nltk.org](https://nltk.org))

- [Nuget package](https://www.nuget.org/packages/NltkNet/)

## **Frameworks supported**

- .NET 4.5 or later

## **Dependencies**

- [IronPython](https://www.nuget.org/packages/IronPython) - 2.7.9 or later (Includes [DynamicLanguageRuntime](https://www.nuget.org/packages/DynamicLanguageRuntime/))

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
using NltkNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TestApp
{
    class Program
    {
        static string text = "This IronPython script works fine when I run it by itself.";

        static void TestTokenize()
        {            
            List<Tuple<int, int>> tuples = Nltk.Tokenize.Util.RegexpSpanTokenize(text, "\\s");
            var list = Nltk.Tokenize.SentTokenize(text);
            if (list != null)
                foreach (var item in list)
                    Console.Write(item + "\r\n");
        }

        static void TestProbability()
        {
            var words = Nltk.Tokenize.WordTokenize(text);
            var fd = Nltk.Probability.FreqDist.Create(words);

            var result = fd.MostCommon(null);
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
            var fileids = corpus.FileIds();
            var words = corpus.Words(fileids.First());
            var sentences = corpus.Sents(fileids.First());
            var paragraphs = corpus.Paras(fileids.First());
            string text = corpus.Raw(fileids.First());
            var taggedWords = corpus.TaggedWords(fileids.First());

            var stopWordsCorpus = new Nltk.Corpus.StopWords();
            var stopWords = stopWordsCorpus.Words(null);

            // Process data returned from NLTK library...
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

            TestTokenize();
            TestProbability();
            TestStem();
            TestCorpus();
        }
    }
}

```

## What's next?

**NltkNet** wrapper may be considered as a starting point for learning NLP using C# and Visual Studio. Current version of NltkNet does not cover lots of features of original NLTK library written in Python. You may use workarounds to execute Python code that didn't wrapped yet.
Take a look at methods `NltkNet.Call` and `NltkNet.CreateNltkObject`.
Also you could execute any of Python code using `PythonWrapper.LoadFromCode` method.

Documentation will be extended with more examples of using NLTK library features that didn't wrapped yet.

