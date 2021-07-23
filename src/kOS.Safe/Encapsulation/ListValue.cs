using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Properties;
using kOS.Safe.Serialization;
using kOS.Safe.Function;
using System.Globalization;

namespace kOS.Safe.Encapsulation
{
    [kOS.Safe.Utilities.KOSNomenclature("List")]
    public class ListValue<T> : CollectionValue<T, List<T>>, IList<T>, IIndexable
        where T : Structure
    {
        public ListValue()
            : this(new List<T>())
        {
        }

        public ListValue(IEnumerable<T> listValue) : base("LIST", new List<T>(listValue))
        {
            RegisterInitializer(ListInitializeSuffixes);
        }

        public int Count => Collection.Count;
        public void CopyTo(T[] array, int arrayIndex) =>
            Collection.CopyTo(array, arrayIndex);

        public void Add(T item)
        {
            CheckReadOnly();
            Collection.Add(item);
        }
        public bool Remove(T item)
        {
            CheckReadOnly();
            return Collection.Remove(item);
        }
        public void RemoveAt(int index)
        {
            CheckReadOnly();
            Collection.RemoveAt(index);
        }

        public T this[int index]
        {
            get { return Collection[index]; }
            set
            {
                CheckReadOnly();
                Collection[index] = value;
            }
        }

        private void ListInitializeSuffixes()
        {
            AddSuffix("COPY",     new NoArgsSuffix<ListValue<T>>        (() => new ListValue<T>(this)));
            AddSuffix("ADD",      new OneArgsSuffix<T>                  (toAdd => Add(toAdd), Resources.ListAddDescription));
            AddSuffix("INSERT",   new TwoArgsSuffix<ScalarValue, T>     ((index, toAdd) => Insert(index, toAdd)));
            AddSuffix("REMOVE",   new OneArgsSuffix<ScalarValue>        (toRemove => RemoveAt(toRemove)));
            AddSuffix("SUBLIST",  new TwoArgsSuffix<ListValue, ScalarValue, ScalarValue>(SubListMethod));
            AddSuffix("JOIN",     new OneArgsSuffix<StringValue, StringValue>(Join));

            AddSuffix(new[] { "INDEXOF", "FIND" }, new OneArgsSuffix<ScalarValue, T>(one => IndexOf(one)));
            AddSuffix(new[] { "LASTINDEXOF", "FINDLAST" }, new OneArgsSuffix<ScalarValue, T>(s => Collection.LastIndexOf(s)));
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

        public static ListValue<T> CreateList<TU>(IEnumerable<TU> list) =>
            new ListValue<T>(list.Cast<T>());

        public Structure GetIndex(int index) =>
            Collection[index];

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
            CheckReadOnly();
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
            CheckReadOnly();
            Collection[index] = (T)value;
        }

        private StringValue Join(StringValue separator) =>
            string.Join(separator, Collection.Select(i => i.ToString()).ToArray());

        public int IndexOf(T item) =>
            Collection.IndexOf(item);
        public void Insert(int index, T item)
        {
            CheckReadOnly();
            Collection.Insert(index, item);
        }

        public override Dump Dump(DumperState s)
        {
            var dump = new DumpList(typeof(ListValue<T>));
            PopulateDumpList(dump, s);
            return dump;
        }

        [DumpDeserializer(typeof(DumpList))]
        public static ListValue<T> CreateFromDump(DumpList d, SafeSharedObjects shared)
        {
            var result = new ListValue<T>();
            for (int i = 0; i < d.Count; i++)
            {
                T value = d.GetDeserialized(i, shared) as T;
                if (value == null)
                    throw new KOSSerializationException("Serialized object contains an object with the wrong type.");
                result.Add(value);
            }
            return result;
        }

        [DumpPrinter(typeof(DumpList))]
        public static void Print(DumpList d, IndentedStringBuilder sb)
        {
            sb.Append("LIST of ");
            sb.Append(d.Count.ToString());
            sb.Append("items: ");

            int maxwidth = (d.Count.ToString(CultureInfo.InvariantCulture) + ". ").Length;

            for (int i = 0; i < d.Count; i++)
            {
                sb.AppendLine();
                var keyBuilder = new SingleLineIndentedStringBuilder();

                string key = (i.ToString(CultureInfo.InvariantCulture) + ". ").PadRight(maxwidth);
                sb.Append(key);

                using (sb.Indent())
                {
                    d[i].WriteReadable(sb);
                }
            }
        }
    }

    [kOS.Safe.Utilities.KOSNomenclature("List", KOSToCSharp = false)] // one-way because the generic templated ListValue<T> is the canonical one.  
    public class ListValue : ListValue<Structure>
    {
        [Function("list")]
        public class FunctionList : SafeFunctionBase
        {
            public override void Execute(SafeSharedObjects shared)
            {
                Structure[] argArray = new Structure[CountRemainingArgs(shared)];
                for (int i = argArray.Length - 1; i >= 0; --i)
                    argArray[i] = PopStructureAssertEncapsulated(shared); // fill array in reverse order because .. stack args.
                AssertArgBottomAndConsume(shared);
                var listValue = new ListValue(argArray.ToList());
                ReturnValue = listValue;
            }
        }

        public ListValue()
        {
            RegisterInitializer(InitializeSuffixes);
        }

        public ListValue(IEnumerable<Structure> toCopy) : base(toCopy)
        {
            RegisterInitializer(InitializeSuffixes);
        }

        public override Dump Dump(DumperState s)
        {
            var dump = new DumpList(typeof(ListValue));
            PopulateDumpList(dump, s);
            return dump;
        }

        private void InitializeSuffixes() =>
            AddSuffix("COPY", new NoArgsSuffix<ListValue>(() => new ListValue(this)));

        public new static ListValue CreateList<T>(IEnumerable<T> toCopy) =>
            new ListValue(toCopy.Select(x => FromPrimitiveWithAssert(x)));
    }
}



