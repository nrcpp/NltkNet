using System;

namespace NltkNet
{
    public static class BuiltIns
    {
        static BuiltIns()
        {
            Nltk.Py.ExecuteScript("def __print__(obj):\r\n\tprint(obj, flush=True)\r\n" +
                                  "def __len__(obj):\r\n\treturn len(obj)\r\n" +
                                  "def __str__(obj):\r\n\treturn str(obj)\r\n" +
                                  "def __set__(obj):\r\n\treturn set(obj)\r\n" +
                                  "def __list__(obj):\r\n\treturn list(obj)\r\n" +
                                  "def __dict__(*obj):\r\n\treturn dict(*obj)\r\n" +
                                  "def __sorted__(obj):\r\n\treturn sorted(obj)\r\n" +
                                  "def __range__(p1, p2, p3):\r\n\treturn range(p1, p2, p3)\r\n" +
                                  "def __zip__(*obj):\r\n\treturn zip(*obj)\r\n" +
                                  "def __importfunc__(name, globals, locals, fromlist, level):\r\n\treturn __import__(name, globals, locals, fromlist, level)\r\n" +
                                  "def __globals__():\r\n\treturn globals()\r\n" +
                                  "def __locals__():\r\n\treturn locals()\r\n"
                                  );
        }

        public static void Print(dynamic pyObj) => Nltk.Py.CallFunction("__print__", pyObj);
        public static long Len(dynamic pyObj) => Convert.ToInt64((object)Nltk.Py.CallFunction("__len__", pyObj));
        public static string Str(dynamic pyObj) => Nltk.Py.CallFunction("__str__", pyObj ?? "");
        public static dynamic Set(dynamic pyObj) => Nltk.Py.CallFunction("__set__", pyObj);
        public static dynamic List(dynamic pyObj) => Nltk.Py.CallFunction("__list__", pyObj);
        public static dynamic Dict(dynamic pyObj) => Nltk.Py.CallFunction("__dict__", pyObj);
        public static dynamic Dict() => Nltk.Py.CallFunction("__dict__");
        public static dynamic Sorted(dynamic pyObj) => Nltk.Py.CallFunction("__sorted__", pyObj);
        public static dynamic Range(int start, int stop, int step = 1) => Nltk.Py.CallFunction("__range__", start, stop, step);
        public static dynamic Zip(params dynamic[] objects) =>  Nltk.Py.CallFunction("__zip__", objects);
        public static dynamic Import(string name, dynamic globals, dynamic locals, dynamic fromlist, int level) =>                
            Nltk.Py.CallFunction("__importfunc__", name, globals, locals, fromlist, level);

        public static dynamic ImportModule(string name) => ImportNames(name);
        public static dynamic ImportNames(string moduleName, params string[] fromList) => Import(moduleName, Globals(), Locals(), fromList.Length == 0 ? new[] { "*" } : fromList, 0);

        public static dynamic Globals() => Nltk.Py.CallFunction("__globals__");
        public static dynamic Locals() => Nltk.Py.CallFunction("__locals__");
    }
}
