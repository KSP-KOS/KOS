using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace kOS.Safe.Encapsulation
{
    public class ListValue : Structure, IIndexable
    {
        private readonly IList<object> list;

        public ListValue()
        {
            list = new List<object>();
        }

        private ListValue(ListValue toCopy)
        {
            list = new List<object>(toCopy.list);
        }

        public int Count
        {
            get { return list.Count; }
        }

        public override bool SetSuffix(string suffixName, object value)
        {
            switch (suffixName)
            {
                case "ADD":
                    list.Add(value);
                    return true;
                case "CONTAINS":
                    return list.Contains(value);
                case "REMOVE":
                    var index = int.Parse(value.ToString());
                    list.RemoveAt(index);
                    return true;
                default:
                    return false;
            }
        }
        
        public void AddMethod(object item)
        {
            list.Add(item);
        }        
        public void RemoveMethod(int index)
        {
            list.RemoveAt(index);
        }
        public bool ContainsMethod(object item)
        {
            return list.Contains(item);
        }
        public int LengthMethod()
        {
            return list.Count;
        }
        // This test case was added to ensure there was an example method with more than 1 argument.
        public ListValue SubListMethod(int start, int runLength)
        {
             ListValue subList = new ListValue();
             for( int i = start ; i < list.Count && i < start + runLength ; ++i )
             {
                 subList.Add( list[i] );
             }
             return subList;
        }
        
        // It seems to be impossible in C# to just generically make a delegate out of
        // a method - it won't let you return it in a generic object like so:
        //     return funcname_without_parentheses;
        // or so:
        //     return (Delegate)funcname_without_parentheses;
        // or so:
        //     return new Delegate(funcname_without_parentheses);
        // It refuses to let you store a delegate as an object unless it's been given a fully qualified
        // type name for the delegate.  I don't understand why it can't read everything it needs to know
        // to understand the delegate's prototype from the function's prototype.  Oh well, anyway that's the
        // reason for this extra level of verbosity here:
        public delegate void DelegateOfAddMethod(object item);
        public delegate void DelegateOfRemoveMethod(int index);
        public delegate bool DelegateOfContainsMethod(object item);
        public delegate int DelegateOfLengthMethod();
        public delegate ListValue DelegateOfSubListMethod(int start, int runLength);
                                                   

        public override object GetSuffix(string suffixName)
        {
            switch (suffixName)
            {
                case "CLEAR":
                    list.Clear();
                    return true;
                case "LENGTH":
                    return list.Count;
                case "ITERATOR":
                    return new Enumerator(list.GetEnumerator());
                case "COPY":
                    return new ListValue(this);
                    
                // These are placeholders to test with until erendrake's new suffix system is in place.
                // The assumption is that any suffix which is a method should return an object of
                // type delegate from its GetSuffix call to represent that it is a method.
                // REMEMBER that even though a method can alter the object, it's still returned
                // via GetSuffix and not SetSuffix because you are returning the delegate function
                // as the object and letting the kOS computer call the delegate once it pulls the
                // arguments off the stack for it.
                case "METHADD":
                    return (DelegateOfAddMethod) AddMethod;
                case "METHREMOVE":
                    return (DelegateOfRemoveMethod) RemoveMethod;
                case "METHCONTAINS":
                    return (DelegateOfContainsMethod) ContainsMethod;
                // This is a test case to make sure it can deal with methods with zero arguments:
                case "METHLENGTH":
                    return (DelegateOfLengthMethod) LengthMethod;
                // This is a test case to make sure it can deal with more than 1 argument:
                case "METHSUBLIST":
                    return (DelegateOfSubListMethod) SubListMethod;

                default:
                    return string.Format("Suffix {0} Not Found", suffixName);
            }
        }

        public void Add(object toAdd)
        {
            list.Add(toAdd);
        }

        public override string ToString()
        {
            return string.Format("{0} LIST({1})", base.ToString(), list.Count);
        }

        public bool Empty()
        {
            return !list.Any();
        }

        #region IIndexable Members

        public object GetIndex(int index)
        {
            return list[index];
        }

        public void SetIndex(int index, object value)
        {
            list[index] = value;
        }

        #endregion
    }

    public class Enumerator : Structure
    {
        private readonly IEnumerator enumerator;
        private readonly object lockObject = new object();
        private int index = -1;
        private bool status;

        public Enumerator(IEnumerator enumerator)
        {
            this.enumerator = enumerator;
        }

        public override object GetSuffix(string suffixName)
        {
            lock (lockObject)
            {
                switch (suffixName)
                {
                    case "RESET":
                        index = -1;
                        status = false;
                        enumerator.Reset();
                        return true;
                    case "NEXT":
                        status = enumerator.MoveNext();
                        index++;
                        return status;
                    case "ATEND":
                        return !status;
                    case "INDEX":
                        return index;
                    case "VALUE":
                        return enumerator.Current;
                    case "ITERATOR":
                        return this;
                    default:
                        return string.Format("Suffix {0} Not Found", suffixName);
                }
            }
        }

        public override string ToString()
        {
            return string.Format("{0} Enumerator", base.ToString());
        }
    }
}
