using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NltkNet
{
    public class PythonWrapper
    {
        private ScriptEngine _engine;
        private ScriptScope _scope;
        private ScriptSource _source;
        private CompiledCode _compiled;
        private object _pythonClass;
        private dynamic _defaultModule;

        public PythonWrapper()
        {
            _engine = Python.CreateEngine();
            _scope = _engine.CreateScope();
        }

        public void LoadCodeFromFile(string pyfile, string className = null)
        {
            string code = File.ReadAllText(pyfile);

            LoadCode(code, className);
        }

        public void LoadCode(string code, string className)
        {
            _source = _engine.CreateScriptSourceFromString(code, Microsoft.Scripting.SourceCodeKind.Statements);
            _compiled = _source.Compile();

            _compiled.Execute(_scope);

            if (className != null)
                _pythonClass = _engine.Operations.Invoke(_scope.GetVariable(className));
        }


        public void ImportModule(string moduleName) =>
            _scope.ImportModule(moduleName);

        public void AddLibPaths(List<string> libsPaths)
        {
            if (libsPaths == null || libsPaths.Count == 0) return;

            ICollection<string> searchPaths = _engine.GetSearchPaths();

            foreach (var path in libsPaths)
            {
                searchPaths.Add(path);
            }

            _engine.SetSearchPaths(searchPaths);
        }


        public void SetVariable(string variable, dynamic value)
        {
            _scope.SetVariable(variable, value);
        }

        public void SetDefaultClass(string className)
        {
            _pythonClass = _engine.Operations.Invoke(_scope.GetVariable(className));
        }

        public void SetDefaultModule(string moduleName)
        {
            _defaultModule = _scope.GetVariable(moduleName);
        }

        public dynamic GetVariable(string variable)
        {
            return _scope.GetVariable(variable);
        }

        public T GetVariable<T>(string variable)
        {
            return _scope.GetVariable<T>(variable);
        }

        public T GetFunction<T>(string funcName)
        {
            return _scope.GetVariable<T>(funcName);
        }

        public void CallMethod(string method, params dynamic[] arguments)
        {
            _engine.Operations.InvokeMember(_pythonClass, method, arguments);
        }

        public dynamic CallFunction(string funcName, params dynamic[] arguments)
        {
            var func = GetVariable(funcName);
            return _engine.Operations.Invoke(func, arguments);
        }

        public dynamic CallModuleFunction(string moduleName, string funcName, params dynamic[] arguments)
        {
            var module = GetVariable(moduleName);
            return _engine.Operations.InvokeMember(module, funcName, arguments);
        }

        public dynamic CallModuleFunction(string funcName, params dynamic[] arguments)
        {
            return _engine.Operations.InvokeMember(_defaultModule, funcName, arguments);
        }

        public T CallModuleFunction<T>(string funcName, params dynamic[] arguments) =>
            (T)CallModuleFunction(funcName, arguments);
    }
}
