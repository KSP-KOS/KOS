using System.Collections;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Serialization;

namespace kOS.Safe.Encapsulation
{
    public abstract class EnumerableValue<T, TC> : SerializableStructure, IEnumerable<T> where TC : IEnumerable<T> where T : Structure
    {
        protected TC Collection { get; private set; }

        protected EnumerableValue(TC collection)
        {
            Collection = collection;

            InitializeEnumerableSuffixes();
        }

        public virtual IEnumerator<T> GetEnumerator()
        {
            return Collection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Contains(T item)
        {
            return Collection.Contains(item);
        }

        public abstract int Count { get; }

        public override string ToString()
        {
            return new SafeSerializationMgr().ToString(this);
        }

        private void InitializeEnumerableSuffixes()
        {
            AddSuffix("ITERATOR",   new NoArgsSuffix<Enumerator>          (() => new Enumerator(Collection.GetEnumerator())));
            AddSuffix("CONTAINS",   new OneArgsSuffix<BooleanValue, T>    (item => Collection.Contains(item)));
            AddSuffix("EMPTY",      new NoArgsSuffix<BooleanValue>        (() => !Collection.Any()));
            AddSuffix("DUMP",       new NoArgsSuffix<StringValue>         (() => new StringValue(ToString())));
        }
    }
}

