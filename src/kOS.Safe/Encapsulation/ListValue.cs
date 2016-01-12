using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Properties;

namespace kOS.Safe.Encapsulation
{
    public class ListValue<T> : EnumerableValue<T, IList<T>>, IIndexable
        where T : Structure
    {

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
            Collection.Add(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Collection.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return Collection.Remove(item);
        }

        public void Clear()
        {
            Collection.Clear();
        }

        public override int Count
        {
            get { return Collection.Count; }
        }

        public void RemoveAt(int index)
        {
            Collection.RemoveAt(index);
        }

        public T this[int index]
        {
            get { return Collection[index]; }
            set { Collection[index] = value; }
        }
            
        public override void LoadDump(IDictionary<Structure, Structure> dump)
        {
            Collection.Clear();

            foreach (Structure item in dump.Values)
            {
                Collection.Add((T)Structure.FromPrimitive(item));
            }
        }

        private void ListInitializeSuffixes()
        {
            AddSuffix("COPY",     new NoArgsSuffix<ListValue<T>>        (() => new ListValue<T>(this)));
            AddSuffix("LENGTH",   new NoArgsSuffix<int>                 (() => Collection.Count));
            AddSuffix("CLEAR",    new NoArgsSuffix                      (() => Collection.Clear()));
            AddSuffix("ADD",      new OneArgsSuffix<T>                  (toAdd => Collection.Add(toAdd), Resources.ListAddDescription));
            AddSuffix("INSERT",   new TwoArgsSuffix<int, T>             ((index, toAdd) => Collection.Insert(index, toAdd)));
            AddSuffix("REMOVE",   new OneArgsSuffix<int>                (toRemove => Collection.RemoveAt(toRemove)));
            AddSuffix("SUBLIST",  new TwoArgsSuffix<ListValue, int, int>(SubListMethod));
       }

        // This test case was added to ensure there was an example method with more than 1 argument.
        private ListValue SubListMethod(int start, int runLength)
        {
            var subList = new ListValue();
            for (int i = start; i < Collection.Count && i < start + runLength; ++i)
            {
                subList.Add(Collection[i]);
            }
            return subList;
        }

        public static ListValue<T> CreateList<TU>(IEnumerable<TU> list)
        {
            return new ListValue<T>(list.Cast<T>());
        }

        public Structure GetIndex(int index)
        {
            return Collection[(int)index];
        }

        public Structure GetIndex(Structure index)
        {
            if (index is ScalarValue)
            {
                int i = Convert.ToInt32(index);  // allow expressions like (1.0) to be indexes
                return GetIndex(i);
            }
            throw new KOSCastException(index.GetType(), typeof(int)/*So the message will say it needs integer, not just any Scalar*/);
        }

        public void SetIndex(Structure index, Structure value)
        {
            int idx;
            try
            {
                idx = Convert.ToInt32(index);
            }
            catch
            {
                throw new KOSException("The index must be an integer number");
            }
            Collection[idx] = (T)value;
        }


    }

    public class ListValue : ListValue<Structure>
    {
        public ListValue()
        {
            InitializeSuffixes();
        }

        public ListValue(IEnumerable<Structure> toCopy)
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