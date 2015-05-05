using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS.Safe.Encapsulation
{
    public class ListValue<T> : Structure, IList<T>, IIndexable, IDumper
    {
        private readonly IList<T> internalList;
        private const int INDENT_SPACES = 2;

        public ListValue()
            : this(new List<T>())
        {
        }

        public ListValue(IEnumerable<T> listValue)
        {
            internalList = listValue.ToList();
            ListInitializeSuffixes();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return internalList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            internalList.Add(item);
        }

        public void Clear()
        {
            internalList.Clear();
        }

        public bool Contains(T item)
        {
            return internalList.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            internalList.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return internalList.Remove(item);
        }

        public int Count
        {
            get { return internalList.Count; }
        }

        public bool IsReadOnly
        {
            get { return internalList.IsReadOnly; }
        }

        public int IndexOf(T item)
        {
            return internalList.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            internalList.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            internalList.RemoveAt(index);
        }

        public T this[int index]
        {
            get { return internalList[index]; }
            set { internalList[index] = value; }
        }

        private void ListInitializeSuffixes()
        {
            AddSuffix("ADD",      new OneArgsSuffix<T>                  (toAdd => internalList.Add(toAdd), Resources.ListAddDescription));
            AddSuffix("INSERT",   new TwoArgsSuffix<int, T>             ((index, toAdd) => internalList.Insert(index, toAdd)));
            AddSuffix("REMOVE",   new OneArgsSuffix<int>                (toRemove => internalList.RemoveAt(toRemove)));
            AddSuffix("CLEAR",    new NoArgsSuffix                      (() => internalList.Clear()));
            AddSuffix("LENGTH",   new NoArgsSuffix<int>                 (() => internalList.Count));
            AddSuffix("ITERATOR", new NoArgsSuffix<Enumerator>          (() => new Enumerator(internalList.GetEnumerator())));
            AddSuffix("COPY",     new NoArgsSuffix<ListValue<T>>        (() => new ListValue<T>(this)));
            AddSuffix("CONTAINS", new OneArgsSuffix<bool, T>            (item => internalList.Contains(item)));
            AddSuffix("SUBLIST",  new TwoArgsSuffix<ListValue, int, int>(SubListMethod));
            AddSuffix("EMPTY",    new NoArgsSuffix<bool>                (() => !internalList.Any()));
            AddSuffix("DUMP",     new NoArgsSuffix<string>              (() => Dump(99, 0).ToString()));
        }

        // This test case was added to ensure there was an example method with more than 1 argument.
        private ListValue SubListMethod(int start, int runLength)
        {
            var subList = new ListValue();
            for (int i = start; i < internalList.Count && i < start + runLength; ++i)
            {
                subList.Add(internalList[i]);
            }
            return subList;
        }

        public override bool SetSuffix(string suffixName, object value)
        {
            //These were deprecated in v0.15. Text here it to assist in upgrading scripts
            switch (suffixName)
            {
                case "ADD":
                    throw new Exception("Old syntax \n" +
                                           "   SET _somelist_:ADD TO _value_\n" +
                                           "is no longer supported. Try replacing it with: \n" +
                                           "   _somelist_:ADD(_value_).\n");
                case "CONTAINS":
                    throw new Exception("Old syntax \n" +
                                           "   SET _somelist_:CONTAINS TO _value_\n" +
                                           "is no longer supported. Try replacing it with: \n" +
                                           "   SET _somelist_:CONTAINS(_value_) TO _value_\n");
                case "REMOVE":
                    throw new Exception("Old syntax \n" +
                                           "   SET _somelist_:REMOVE TO _number_\n" +
                                           "is no longer supported. Try replacing it with: \n" +
                                           "   _somelist_:REMOVE(_number_).\n");
                default:
                    return false;
            }
        }

        public static ListValue<T> CreateList<TU>(IEnumerable<TU> list)
        {
            return new ListValue<T>(list.Cast<T>());
        }

        public object GetIndex(int index)
        {
            return internalList[index];
        }

        public void SetIndex(int index, object value)
        {
            internalList[index] = (T)value;
        }

        public StringBuilder Dump(int limit, int depth = 0)
        {
            var toReturn = new StringBuilder();

            var listString = string.Format("LIST of {0} items", Count);
            listString = string.Empty.PadLeft(depth * INDENT_SPACES) + listString; 
            toReturn.AppendLine(listString);

            if (limit <= 0) return toReturn;

            for (int index = 0; index < internalList.Count; index++)
            {
                var item = internalList[index];


                var dumper = item as IDumper;
                if (dumper != null)
                {
                    toReturn.Append(dumper.Dump(--limit, ++depth));
                }
                else
                {
                    var itemString = string.Format("[[{0}] {1}]", index, item);
                    itemString = string.Empty.PadLeft(depth * INDENT_SPACES) + itemString; 
                    toReturn.AppendLine(itemString);
                }
            }
            return toReturn;
        }

        public override string ToString()
        {
            return Dump(1).ToString();
        }
    }

    public class ListValue : ListValue<object>
    {
        public ListValue()
        {
            InitializeSuffixes();
        }

        public ListValue(IEnumerable<object> toCopy)
            : base(toCopy)
        {
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("COPY", new NoArgsSuffix<ListValue>(() => new ListValue(this)));
        }

        public new static ListValue CreateList<T>(IEnumerable<T> toCopy)
        {
            return new ListValue(toCopy.Cast<object>());
        }
    }
}