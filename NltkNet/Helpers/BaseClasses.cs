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


        public class NltkResult<NetType, PythonType> : DynamicObject
        {
            public NetType AsNet { get; set; }
            public PythonType AsPython { get; set; }
            public dynamic AsDynamic => AsPython;


            public NltkResult()
            {
                AsNet = default(NetType);
                AsPython = default(PythonType);
            }

            public NltkResult(NetType netValue, PythonType pythonValue)
            {
                AsNet = netValue;
                AsPython = pythonValue;
            }

            public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
            {
                MethodInfo method = AsPython.GetType().GetMethod(binder.Name);
                result = method.Invoke(AsPython, args);
                return true;
            }


            // TODO: access multi-dimesional 
            // https://stackoverflow.com/questions/2783502/c-sharp-using-the-dynamic-keyword-to-access-properties-via-strings-without-refle
            public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
            {
                result = AsDynamic[indexes[0]];
                return true;
            }

            public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
            {
                AsDynamic[indexes[0]] = value;
                return true;
            }

            public override bool TryGetMember(GetMemberBinder binder, out object result)
            {
                result = AsPython.GetType().GetProperty(binder.Name).GetValue(AsPython);
                return true;
            }

            public override bool TrySetMember(SetMemberBinder binder, object value)
            {
                AsPython.GetType().GetProperty(binder.Name).SetValue(AsPython, value);
                return true;
            }
        }

        public class NltkResult<NetType> : NltkResult<NetType, dynamic> { }

        public class NltkResultListString : NltkResult<List<string>, IronPython.Runtime.List> { }
        public class NltkResultListStringDynamic : NltkResult<List<string>, dynamic> { }
        public class NltkResultListListString : NltkResult<List<List<string>>, dynamic> { }
        public class NltkResultListListListString : NltkResult<List<List<List<string>>>, dynamic> { }
        public class NltkResultListTupleStringString : NltkResult<List<Tuple<string, string>>, dynamic> {}
        public class NltkResultDictionaryStringInt : NltkResult<Dictionary<string, int>, dynamic> { }
        public class NltkResultListTupleIntInt : NltkResult<List<Tuple<int, int>>, dynamic> {}
    

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
