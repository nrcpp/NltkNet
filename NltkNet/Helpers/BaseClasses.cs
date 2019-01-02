using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NltkNet
{
    public static partial class Nltk
    {
        /// <summary>
        /// Class that could be expanded with additional properties and methods
        /// </summary>
        public class NltkDynamicObject : DynamicObject
        {
            protected readonly Dictionary<string, object> _dictionary = new Dictionary<string, object>();

            public override bool TryGetMember(GetMemberBinder binder, out object result)
            {
                _dictionary.TryGetValue(binder.Name, out result);
                return true;
            }

            public override bool TrySetMember(SetMemberBinder binder, object value)
            {
                _dictionary[binder.Name] = value;
                return true;
            }
        }


        public class NltkClass<T>
        {
            public dynamic PyObject { get; protected set; }

            protected NltkClass(params object[] args)
            {
                PyObject = CreateNltkObject(typeof(T).Name, args);
            }
        }
    }
}
