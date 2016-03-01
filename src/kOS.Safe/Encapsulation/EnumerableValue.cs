﻿using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace kOS.Safe.Encapsulation
{
    [kOS.Safe.Utilities.KOSNomenclature("Enumerable")]
    public abstract class EnumerableValue<T, TE> : SerializableStructure, IEnumerable<T>
        where TE : IEnumerable<T>
        where T : Structure
    {
        protected TE InnerEnumerable { get; private set; }
        private readonly string label;

        protected EnumerableValue(string label, TE enumerable)
        {
            this.label = label;
            InnerEnumerable = enumerable;

            InitializeEnumerableSuffixes();
        }

        public virtual IEnumerator<T> GetEnumerator()
        {
            return InnerEnumerable.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Contains(T item)
        {
            return InnerEnumerable.Contains(item);
        }

        public int Count()
        {
            return InnerEnumerable.Count();
        }

        public override string ToString()
        {
            return new SafeSerializationMgr().ToString(this);
        }

        public override Dump Dump()
        {
            var result = new DumpWithHeader
            {
                Header = label + " of " + InnerEnumerable.Count() + " items:"
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
            AddSuffix("ITERATOR",        new NoArgsSuffix<Enumerator>(() => new Enumerator(InnerEnumerable.GetEnumerator())));
            AddSuffix("REVERSEITERATOR", new NoArgsSuffix<Enumerator>(() => new Enumerator(Enumerable.Reverse(InnerEnumerable).GetEnumerator())));
            AddSuffix("LENGTH",          new NoArgsSuffix<ScalarValue>(() => InnerEnumerable.Count()));
            AddSuffix("CONTAINS",        new OneArgsSuffix<BooleanValue, T>((n) => Contains(n)));
            AddSuffix("EMPTY",           new NoArgsSuffix<BooleanValue>(() => !InnerEnumerable.Any()));
            AddSuffix("DUMP",            new NoArgsSuffix<StringValue>(() => new StringValue(ToString())));
        }
    }
}