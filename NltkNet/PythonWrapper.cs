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
        private dynamic _pythonClass;
        private dynamic _defaultModule;

        public PythonWrapper()
        {
            _engine = Python.CreateEngine();
            _scope = _engine.CreateScope();
        }

        public void LoadCodeFromFile(string pyfile, string className = null)
        {
            string code = File.ReadAllText(pyfile);

            dynamic result = ExecuteScript(code);
            if (className != null)
                _pythonClass = _engine.Operations.Invoke(_scope.GetVariable(className));
        }


        public dynamic ExecuteScript(string code)
        {
            _source = _engine.CreateScriptSourceFromString(code, Microsoft.Scripting.SourceCodeKind.Statements);
            _compiled = _source.Compile();

            dynamic result = _compiled.Execute(_scope);

            return result;
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

        public dynamic CreateObject(string className, params dynamic[] parameters)
        {
            return _engine.Operations.Invoke(_scope.GetVariable(className), parameters);
        }

        public void SetDefaultClass(string className, params dynamic[] arguments)
        {
            _pythonClass = CreateObject(className, arguments);
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

        // aliases
        public T GetFunction<T>(string funcName) => _scope.GetVariable<T>(funcName);
        public dynamic GetObject(string objectName) => _scope.GetVariable(objectName);
        public dynamic GetModule(string moduleName) => _scope.GetVariable(moduleName);


        public dynamic CallMethod(dynamic pyObject, string method, params dynamic[] parameters)
        {
            return _engine.Operations.InvokeMember(pyObject, method, parameters);
        }

        public dynamic CallMethod(string method, params dynamic[] parameters)
        {
            return _engine.Operations.InvokeMember(_pythonClass, method, parameters);
        }

        public dynamic CallFunction(string funcName, params dynamic[] parameters)
        {
            var func = GetVariable(funcName);
            return _engine.Operations.Invoke(func, parameters);
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
