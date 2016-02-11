using System.Collections;
using System.Collections.Generic;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Serialization;
using System.Linq;

namespace kOS.Safe.Encapsulation
{
    public abstract class EnumerableValue<T, TE> : SerializableStructure, IEnumerable<T> where TE : IEnumerable<T> where T : Structure
    {
        protected TE Enum { get; private set; }
        private readonly string label;

        protected EnumerableValue(string label, TE enumerable)
        {
            this.label = label;
            Enum = enumerable;

            InitializeEnumerableSuffixes();
        }

        public virtual IEnumerator<T> GetEnumerator()
        {
            return Enum.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Contains(T item)
        {
            return Enum.Contains(item);
        }

        public int Count()
        {
            return Enum.Count();
        }

        public override string ToString()
        {
            return new SafeSerializationMgr().ToString(this);
        }

        public override Dump Dump()
        {
            var result = new DumpWithHeader
            {
                Header = label + " of " + Enum.Count() + " items:"
            };

            int i = 0;
            foreach (T item in this)
            {
                result.Add(i, item);
                i++;
            }

            return result;
        }

        private void InitializeEnumerableSuffixes()
        {
            AddSuffix("ITERATOR",           new NoArgsSuffix<Enumerator>          (() => new Enumerator(Enum.GetEnumerator())));
            AddSuffix("REVERSEITERATOR",    new NoArgsSuffix<Enumerator>          (() => new Enumerator(Enumerable.Reverse(Enum).GetEnumerator())));
            AddSuffix("LENGTH",             new NoArgsSuffix<ScalarValue>         (() => Enum.Count()));
            AddSuffix("CONTAINS",           new OneArgsSuffix<BooleanValue, T>    ((n) => Contains(n)));
            AddSuffix("EMPTY",              new NoArgsSuffix<BooleanValue>        (() => !Enum.Any()));
            AddSuffix("DUMP",               new NoArgsSuffix<StringValue>         (() => new StringValue(ToString())));
        }
    }
}