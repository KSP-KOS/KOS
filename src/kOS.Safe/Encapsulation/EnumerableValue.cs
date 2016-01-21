using System.Collections;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Serialization;

namespace kOS.Safe.Encapsulation
{
    public abstract class EnumerableValue<T, TC> : Structure, IEnumerable<T>, IDumper where TC : IEnumerable<T> where T : Structure
    {
        protected TC Collection { get; private set; }
        private readonly string label;

        protected EnumerableValue(string label, TC collection)
        {
            this.label = label;
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

        public IDictionary<object, object> Dump()
        {
            var result = new DictionaryWithHeader
            {
                Header = label + " of " + Collection.Count() + " items:"
            };


            int i = 0;
            foreach (T item in this)
            {
                result.Add(i, item);
                i++;
            }

            return result;
        }

        public abstract void LoadDump(IDictionary<object, object> dump);

        private void InitializeEnumerableSuffixes()
        {
            AddSuffix("ITERATOR",   new NoArgsSuffix<Enumerator>          (() => new Enumerator (Collection.GetEnumerator())));
            AddSuffix("CONTAINS",   new OneArgsSuffix<BooleanValue, T>    (item => Collection.Contains(item)));
            AddSuffix("EMPTY",      new NoArgsSuffix<BooleanValue>        (() => !Collection.Any()));
            AddSuffix("DUMP",       new NoArgsSuffix<StringValue>         (() => ToString()));
        }
    }
}

