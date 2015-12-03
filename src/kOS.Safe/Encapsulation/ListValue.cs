using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace kOS.Safe.Encapsulation
{
    public class ListValue<T> : EnumerableValue<T, IList<T>>, IIndexable
    {
        private const int INDENT_SPACES = 2;

        public ListValue()
            : this(new List<T>())
        {
        }

        public ListValue(IEnumerable<T> listValue) : base("LIST", new List<T>(listValue))
        {
            ListInitializeSuffixes();
        }

        public void Add(T item)
        {
            collection.Add(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            collection.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return collection.Remove(item);
        }

        public void Clear()
        {
            collection.Clear();
        }

        public override int Count
        {
            get { return collection.Count; }
        }

        public void RemoveAt(int index)
        {
            collection.RemoveAt(index);
        }

        public T this[int index]
        {
            get { return collection[index]; }
            set { collection[index] = value; }
        }

        private void ListInitializeSuffixes()
        {
            AddSuffix("COPY",     new NoArgsSuffix<ListValue<T>>        (() => new ListValue<T>(this)));
            AddSuffix("LENGTH",   new NoArgsSuffix<int>                 (() => collection.Count));
            AddSuffix("CLEAR",    new NoArgsSuffix                      (() => collection.Clear()));
            AddSuffix("ADD",      new OneArgsSuffix<T>                  (toAdd => collection.Add(toAdd), Resources.ListAddDescription));
            AddSuffix("INSERT",   new TwoArgsSuffix<int, T>             ((index, toAdd) => collection.Insert(index, toAdd)));
            AddSuffix("REMOVE",   new OneArgsSuffix<int>                (toRemove => collection.RemoveAt(toRemove)));
            AddSuffix("SUBLIST",  new TwoArgsSuffix<ListValue, int, int>(SubListMethod));
       }

        // This test case was added to ensure there was an example method with more than 1 argument.
        private ListValue SubListMethod(int start, int runLength)
        {
            var subList = new ListValue();
            for (int i = start; i < collection.Count && i < start + runLength; ++i)
            {
                subList.Add(collection[i]);
            }
            return subList;
        }

        public static ListValue<T> CreateList<TU>(IEnumerable<TU> list)
        {
            return new ListValue<T>(list.Cast<T>());
        }

        public object GetIndex(object index)
        {
            // TODO: remove double and float reference as it should be obsolete
            if (index is double || index is float || index is ScalarValue)
            {
                index = Convert.ToInt32(index);  // allow expressions like (1.0) to be indexes
            }
            if (!(index is int)) throw new Exception("The index must be an integer number");

            return collection[(int)index];
        }

        public void SetIndex(object index, object value)
        {
            // TODO: remove double and float reference as it should be obsolete
            if (index is double || index is float || index is ScalarValue)
            {
                index = Convert.ToInt32(index);  // allow expressions like (1.0) to be indexes
            }

            if (!(index is int)) throw new KOSException("The index must be an integer number");

            collection[(int)index] = (T)value;
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