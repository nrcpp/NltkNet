using IronPython.Runtime;
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
        public class NltkResult<NetType, PythonType> : DynamicObject                   
        {
            private NetType _asNet;

            public NetType AsNet
            {
                get
                {
                    // Lazy pattern
                    if (_asNet == null && ToNetConverter != null)
                        _asNet = ToNetConverter(AsPython);

                    return _asNet;
                }

                set => _asNet = value;
            }

            public PythonType AsPython { get; set; }
            public dynamic AsDynamic => AsPython;

            virtual public Func<PythonType, NetType> ToNetConverter { get; set; }            

            public NltkResult()
            {                
            }

            public NltkResult(NetType netValue, PythonType pythonValue)
            {
                AsNet = netValue;
                AsPython = pythonValue;
            }

            public NltkResult(Func<PythonType, NetType> converter, PythonType pythonValue)
            {
                ToNetConverter = (Func<PythonType, NetType>)converter;
                AsPython = pythonValue;
            }

            #region DynamicObject overrides

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

            #endregion
        }

        public class NltkResult<NetType> : NltkResult<NetType, dynamic> { }

        public class NltkResult : NltkResult<dynamic, dynamic> { }

        public class NltkResultTupleStrStr : NltkResult<(string, string), dynamic>
        {
            public override Func<dynamic, (string, string)> ToNetConverter => tuple => ValueTuple.Create((string)tuple[0], (string)tuple[1]);
        }

        public class NltkResultListString : NltkResult<List<string>, IronPython.Runtime.List>
        {
            public override Func<List, List<string>> ToNetConverter => list => list.Cast<string>().ToList();
        }

        public class NltkResultListStringDynamic : NltkResult<List<string>, dynamic>
        {
            public override Func<dynamic, List<string>> ToNetConverter => list =>
            {
                List<string> result = new List<string>();
                foreach (var item in list)
                    result.Add((string)item);
                return result;
            };
        }


        public class NltkResultListListString : NltkResult<List<List<string>>, dynamic>
        {
            public override Func<dynamic, List<List<string>>> ToNetConverter
                => listOflistOfStr => Convert(listOflistOfStr);

            static List<List<string>> Convert(dynamic listOflistOfStr)
            {
                var result = new List<List<string>>();
                foreach (var s in listOflistOfStr)
                {
                    var words = new List<string>();
                    foreach (var w in s)
                        words.Add((string)w);

                    result.Add(words);
                }

                return result;
            }
        }


        public class NltkResultListListListString : NltkResult<List<List<List<string>>>, dynamic>
        {
            public override Func<dynamic, List<List<List<string>>>> ToNetConverter => listOflistOflistOfStr => Convert(listOflistOflistOfStr);

            static List<List<List<string>>> Convert(dynamic listOflistOflistOfStr)
            {
                var result = new List<List<List<string>>>();
                foreach (var p in listOflistOflistOfStr)
                {
                    var sents = new List<List<string>>();
                    foreach (var s in p)
                    {
                        var words = new List<string>();
                        foreach (var w in s)
                            words.Add((string)w);

                        sents.Add(words);
                    }

                    result.Add(sents);
                }

                return result;
            }
        }


        public class NltkResultListTupleStringString : NltkResult<List<Tuple<string, string>>, dynamic>
        {
            public override Func<dynamic, List<Tuple<string, string>>> ToNetConverter =>
                    tupleStrStr => Convert(tupleStrStr);

            static List<Tuple<string, string>> Convert(dynamic tupleStrStr)
            {
                List<Tuple<string, string>> result = new List<Tuple<string, string>>();
                
                foreach (var tw in tupleStrStr)
                {
                    var wordTag = new Tuple<string, string>(tw[0], tw[1]);
                    result.Add(wordTag);
                }

                return result;
            }
        }

        // list of list of tuple str str
        public class NltkResultListListTupleStringString : NltkResult<List<List<(string, string)>>, dynamic>
        {
            public override Func<dynamic, List<List<(string, string)>>> ToNetConverter =>
                    tupleStrStr => Convert(tupleStrStr);

            static List<List<(string, string)>> Convert(dynamic listOfListOftupleStrStr)
            {
                var result = new List<List<(string, string)>>();

                foreach (var listOfList in listOfListOftupleStrStr)
                {
                    var taggedSent = new List<(string, string)>();
                    foreach (var sent in listOfList)
                    {
                        taggedSent.Add((sent[0], sent[1]));
                    }

                    result.Add(taggedSent);
                }


                return result;
            }
        }


        public class NltkResultDictionaryStringInt : NltkResult<Dictionary<string, int>, dynamic>
        {
            public override Func<dynamic, Dictionary<string, int>> ToNetConverter =>
                listOftupleStrInt => Nltk.ToNetDictionary<string, int>(listOftupleStrInt);
        }

        public class NltkResultListTupleIntInt : NltkResult<List<Tuple<int, int>>, dynamic>
        {
            public override Func<dynamic, List<Tuple<int, int>>> ToNetConverter =>
                listOftupleIntInt => Nltk.ToNetListTuple<int, int>(listOftupleIntInt); 
        }
    }
}
