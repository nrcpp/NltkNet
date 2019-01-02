using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NltkNet
{
    public static class StandardLibrary
    {
        static StandardLibrary()
        {
            // built-in functions: print, len
            Nltk.Py.ExecuteScript("def __print__(obj):\r\n\tprint obj");
            Nltk.Py.ExecuteScript("def __len__(obj):\r\n\treturn len(obj)");
        }

        public static void Print(dynamic pyObj) => Nltk.Py.CallFunction("__print__", pyObj);
        public static long Len(dynamic pyObj) => Nltk.Py.CallFunction("__len__", pyObj);
    }
}
