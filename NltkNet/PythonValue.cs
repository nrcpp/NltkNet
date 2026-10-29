using Python.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Runtime.CompilerServices;

namespace NltkNet
{
    /// <summary>A Python object whose dynamic operations acquire the CPython GIL.</summary>
    public sealed class PythonValue : DynamicObject, IEnumerable<object>, IDisposable
    {
        private readonly PyObject value;
        private bool disposed;

        internal PythonValue(PyObject value) => this.value = value;

        internal PyObject Borrow()
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            return value;
        }

        internal static PyObject ToPython(object item)
        {
            if (item is PythonValue wrapped)
                return wrapped.Borrow().NewReference();
            if (item is PyObject python)
                return python.NewReference();
            if (item is IDictionary dictionary)
            {
                var result = new PyDict();
                try
                {
                    foreach (DictionaryEntry pair in dictionary)
                    {
                        using var key = ToPython(pair.Key);
                        using var entry = ToPython(pair.Value);
                        result.SetItem(key, entry);
                    }
                    return result;
                }
                catch
                {
                    result.Dispose();
                    throw;
                }
            }
            if (item is ITuple tuple)
            {
                var values = new PyObject[tuple.Length];
                try
                {
                    for (int i = 0; i < values.Length; i++)
                        values[i] = ToPython(tuple[i]);
                    return new PyTuple(values);
                }
                finally
                {
                    foreach (var element in values)
                        element?.Dispose();
                }
            }
            if (item is IEnumerable sequence && item is not string)
            {
                var result = new PyList();
                try
                {
                    foreach (var element in sequence)
                    {
                        using var entry = ToPython(element);
                        result.Append(entry);
                    }
                    return result;
                }
                catch
                {
                    result.Dispose();
                    throw;
                }
            }
            return item.ToPython();
        }

        // Takes ownership of the supplied reference.
        internal static object FromPython(PyObject result)
        {
            if (result.IsNone())
            {
                result.Dispose();
                return null;
            }
            using var type = result.GetPythonType();
            object converted;
            switch (type.Name)
            {
                case "str": converted = result.As<string>(); break;
                case "bool": converted = result.As<bool>(); break;
                case "int":
                    long number = result.As<long>();
                    converted = number >= int.MinValue && number <= int.MaxValue ? (object)(int)number : number;
                    break;
                case "float": converted = result.As<double>(); break;
                default: return new PythonValue(result);
            }
            result.Dispose();
            return converted;
        }

        internal static object Invoke(PyObject callable, object[] arguments)
        {
            var converted = new PyObject[arguments.Length];
            try
            {
                for (int i = 0; i < arguments.Length; i++)
                    converted[i] = ToPython(arguments[i]);
                return FromPython(callable.Invoke(converted));
            }
            finally
            {
                foreach (var argument in converted)
                    argument?.Dispose();
            }
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            using (Py.GIL())
                result = FromPython(Borrow().GetAttr(binder.Name));
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            using (Py.GIL())
            using (var converted = ToPython(value))
                Borrow().SetAttr(binder.Name, converted);
            return true;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            if (binder.CallInfo.ArgumentNames.Count != 0)
                throw new NotSupportedException("Named dynamic arguments are not supported. Use PythonWrapper.Evaluate for Python keyword calls.");
            using (Py.GIL())
            using (var callable = Borrow().GetAttr(binder.Name))
                result = Invoke(callable, args);
            return true;
        }

        public override bool TryInvoke(InvokeBinder binder, object[] args, out object result)
        {
            if (binder.CallInfo.ArgumentNames.Count != 0)
                throw new NotSupportedException("Named dynamic arguments are not supported.");
            using (Py.GIL())
                result = Invoke(Borrow(), args);
            return true;
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            if (indexes.Length != 1)
                throw new NotSupportedException("Use a tuple for multidimensional Python indexing.");
            using (Py.GIL())
            using (var index = ToPython(indexes[0]))
                result = FromPython(Borrow().GetItem(index));
            return true;
        }

        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            if (indexes.Length != 1)
                throw new NotSupportedException("Use a tuple for multidimensional Python indexing.");
            using (Py.GIL())
            using (var index = ToPython(indexes[0]))
            using (var entry = ToPython(value))
                Borrow().SetItem(index, entry);
            return true;
        }

        public override string ToString()
        {
            using (Py.GIL())
                return Borrow().ToString();
        }

        public IEnumerator<object> GetEnumerator()
        {
            PyIter iterator;
            using (Py.GIL())
                iterator = Borrow().GetIterator();
            try
            {
                while (true)
                {
                    object item;
                    using (Py.GIL())
                    {
                        if (!iterator.MoveNext())
                            yield break;
                        item = FromPython(iterator.Current);
                    }
                    yield return item;
                }
            }
            finally
            {
                using (Py.GIL())
                    iterator.Dispose();
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Dispose()
        {
            using (Py.GIL())
            {
                if (!disposed)
                {
                    value.Dispose();
                    disposed = true;
                }
            }
        }
    }
}
