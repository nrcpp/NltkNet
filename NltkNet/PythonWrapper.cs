using Python.Runtime;
using System;
using System.Collections.Generic;
using System.IO;

namespace NltkNet
{
    public sealed class PythonWrapper : IDisposable
    {
        private readonly PyModule scope;
        private PythonValue pythonClass;
        private string defaultModule;
        private bool disposed;

        public PythonWrapper()
        {
            PortableRuntime.Initialize();
            using (Py.GIL())
                scope = Py.CreateScope();
        }

        public void LoadCodeFromFile(string pyfile, string className = null)
        {
            ExecuteScript(File.ReadAllText(pyfile));
            if (className != null)
                pythonClass = CreateObject(className);
        }

        public dynamic ExecuteScript(string code)
        {
            using (Py.GIL())
                Scope.Exec(code);
            return null;
        }

        public dynamic Evaluate(string expression)
        {
            using (Py.GIL())
                return PythonValue.FromPython(Scope.Eval(expression));
        }

        private PyModule Scope
        {
            get
            {
                ObjectDisposedException.ThrowIf(disposed, this);
                return scope;
            }
        }

        public void ImportModule(string moduleName)
        {
            using (Py.GIL())
            using (var module = Py.Import(moduleName))
                Scope.Set(moduleName, module);
        }

        public void AddLibPaths(List<string> libsPaths)
        {
            ArgumentNullException.ThrowIfNull(libsPaths);
            using (Py.GIL())
            using (var sys = Py.Import("sys"))
            using (var path = sys.GetAttr("path"))
            {
                foreach (string directory in libsPaths)
                {
                    if (!Directory.Exists(directory))
                        throw new DirectoryNotFoundException($"Python search directory does not exist: {directory}");
                    using var entry = new PyString(Path.GetFullPath(directory));
                    using var ignored = path.InvokeMethod("append", new PyObject[] { entry });
                }
            }
        }

        public void SetVariable(string variable, dynamic value)
        {
            using (Py.GIL())
            using (var converted = PythonValue.ToPython((object)value))
                Scope.Set(variable, converted);
        }

        public dynamic CreateObject(string className, params dynamic[] parameters) => CallFunction(className, parameters);
        public void SetDefaultClass(string className, params dynamic[] arguments) => pythonClass = CreateObject(className, arguments);
        public void SetDefaultModule(string moduleName) => defaultModule = moduleName;

        public dynamic GetVariable(string variable)
        {
            using (Py.GIL())
                return PythonValue.FromPython(Scope.Get(variable));
        }

        public T GetVariable<T>(string variable) => (T)GetVariable(variable);
        public T GetFunction<T>(string funcName) => GetVariable<T>(funcName);
        public dynamic GetObject(string name) => GetVariable(name);
        public dynamic GetModule(string name) => GetVariable(name);

        public dynamic CallMethod(dynamic pyObject, string method, params dynamic[] parameters)
        {
            using (Py.GIL())
            using (var target = PythonValue.ToPython((object)pyObject))
            using (var callable = target.GetAttr(method))
                return PythonValue.Invoke(callable, parameters);
        }

        public dynamic CallMethod(string method, params dynamic[] parameters) => CallMethod(pythonClass, method, parameters);

        public dynamic CallFunction(string funcName, params dynamic[] arguments)
        {
            using (Py.GIL())
            using (var callable = Scope.Get(funcName))
                return PythonValue.Invoke(callable, arguments);
        }

        public dynamic CallModuleFunction(string moduleName, string funcName, params dynamic[] arguments)
        {
            using (Py.GIL())
            using (var module = Py.Import(moduleName))
            using (var callable = module.GetAttr(funcName))
                return PythonValue.Invoke(callable, arguments);
        }

        public dynamic CallModuleFunction(string funcName, params dynamic[] arguments)
        {
            if (defaultModule == null)
                throw new InvalidOperationException("No default Python module has been configured.");
            return CallModuleFunction(defaultModule, funcName, arguments);
        }

        public T CallModuleFunction<T>(string funcName, params dynamic[] arguments) => (T)CallModuleFunction(funcName, arguments);

        public void Dispose()
        {
            using (Py.GIL())
            {
                if (!disposed)
                {
                    pythonClass?.Dispose();
                    scope.Dispose();
                    disposed = true;
                }
            }
        }
    }
}
