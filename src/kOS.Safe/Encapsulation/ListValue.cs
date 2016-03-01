using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Properties;
using kOS.Safe.Serialization;

namespace kOS.Safe.Encapsulation
{
    [kOS.Safe.Utilities.KOSNomenclature("List")]
    public class ListValue<T> : CollectionValue<T, IList<T>>, IIndexable
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

        public void RemoveAt(int index)
        {
            Collection.RemoveAt(index);
        }

        public T this[int index]
        {
            get { return Collection[index]; }
            set { Collection[index] = value; }
        }
            
        public override Dump Dump()
        {
            var result = new DumpWithHeader
            {
                Header = "LIST of " + Collection.Count() + " items:"
            };
            
            // This conversion is needed because TerminalFormatter.WriteIndented() demands to only
            // work with exactly List<object> and bombs out on List<Structure>'s:
            List<object> list = new List<object>();
            foreach (object entry in Collection)
                list.Add(entry);
            
            result.Add(kOS.Safe.Dump.Items, list);
            return result;
        }

        public override void LoadDump(Dump dump)
        {
            Collection.Clear();

            List<object> values = (List<object>)dump[kOS.Safe.Dump.Items];

            foreach (object item in values)
            {
                Collection.Add((T)FromPrimitive(item));
            }
        }

        private void ListInitializeSuffixes()
        {
            AddSuffix("COPY",     new NoArgsSuffix<ListValue<T>>        (() => new ListValue<T>(this)));
            AddSuffix("ADD",      new OneArgsSuffix<T>                  (toAdd => Collection.Add(toAdd), Resources.ListAddDescription));
            AddSuffix("INSERT",   new TwoArgsSuffix<ScalarValue, T>     ((index, toAdd) => Collection.Insert(index, toAdd)));
            AddSuffix("REMOVE",   new OneArgsSuffix<ScalarValue>        (toRemove => Collection.RemoveAt(toRemove)));
            AddSuffix("SUBLIST",  new TwoArgsSuffix<ListValue, ScalarValue, ScalarValue>(SubListMethod));
            AddSuffix("JOIN",     new OneArgsSuffix<StringValue, StringValue>(Join));
       }

        // This test case was added to ensure there was an example method with more than 1 argument.
        private ListValue SubListMethod(ScalarValue start, ScalarValue runLength)
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
            return Collection[index];
        }

        public Structure GetIndex(Structure index)
        {
            if (index is ScalarValue)
            {
                int i = Convert.ToInt32(index);  // allow expressions like (1.0) to be indexes
                return GetIndex(i);
            }
            // Throw cast exception with ScalarIntValue, instead of just any ScalarValue
            throw new KOSCastException(index.GetType(), typeof(ScalarIntValue));
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

        public void SetIndex(int index, Structure value)
        {
            Collection[index] = (T)value;
        }

        private StringValue Join(StringValue separator)
        {
            return string.Join(separator, Collection.Select(i => i.ToString()).ToArray());
        }
    }

    [kOS.Safe.Utilities.KOSNomenclature("List", KOSToCSharp = false)] // one-way because the generic templated ListValue<T> is the canonical one.  
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
            return new ListValue(toCopy.Select(x => FromPrimitiveWithAssert(x)));
        }
    }
}