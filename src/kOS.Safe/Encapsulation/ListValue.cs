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
                    // TODO:
                    // @erendrake:  When changing this to your new Suffix system, it might be a good
                    // idea to keep the old SetSuffix term "ADD", but change what it does to make it result in
                    // an exception like the example below.  That way when people use old scripts
                    // that use it, they'll get a nice explanation of exactly how to upgrade the script.
                    // This could save us some headaches when this is released.
                    // You can stil use the word "ADD" as the name of the new method because as a method,
                    // it will be a GetSUffix instead of a SetSuffix.  Only throw this message when it's used
                    // as a SetSuffix:
                    throw new System.Exception("Old syntax \n" +
                                               "   SET _somelist_:ADD TO _value_\n" +
                                               "is no longer supported. Try replacing it with: \n" +
                                               "   _somelist_:ADD(_value_).\n");
                    return true; // compiler warning, unreachable because of the throw statement, but keep it in case I edit it later.
                case "CONTAINS":
                    // TODO: Uhm... @erendrake, take a careful look at this.  What is this even doing here?
                    // It sounds like it should return a boolean but... but... This is a SetSuffix not a 
                    // GetSuffix so I don't quite get how the resulting boolean is meant to be read by the
                    // script.  Maybe when you method-ize ListValue.cs, this would make sense as a GetSuffix method
                    // instead: SET doesItExist TO MYLIST:CONTAINS(VALUE).
                    return list.Contains(value);
                case "REMOVE":
                    // TODO:
                    // @erendrake:  When changing this to your new Suffix system, it might be a good
                    // idea to keep the old SetSuffix term "REMOVE", but yadda yadda yadda (same comment
                    // as I made above for "ADD".)
                    throw new System.Exception("Old syntax \n" +
                                               "   SET _somelist_:REMOVE TO _number_\n" +
                                               "is no longer supported. Try replacing it with: \n" +
                                               "   _somelist_:REMOVE(_number_).\n");
                    return true; // compiler warning, unreachable because of the throw statement, but keep it in case I edit it later.
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
                case "ADD":
                    return (DelegateOfAddMethod) AddMethod;
                case "REMOVE":
                    return (DelegateOfRemoveMethod) RemoveMethod;
                case "CONTAINS":
                    return (DelegateOfContainsMethod) ContainsMethod;
                // This is a test case that duplicates the information provided by :LENGTH, but does it as a function,
                // because I needed a test case in which there was a method with zero arguments:
                case "GETLENGTH":
                    return (DelegateOfLengthMethod) LengthMethod;
                // This is a test case to make sure it can deal with more than 1 argument:
                case "SUBLIST":
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
