using System;
using kOS.Safe.Encapsulation;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Serialization;

namespace kOS.Safe
{
    public abstract class EnumerableValue<T, C> : Structure, IEnumerable<T>, IDumper where C : IEnumerable<T>
    {
        private const int INDENT_SPACES = 2;
        protected readonly C collection;
        private string label;

        public EnumerableValue(string label, C collection)
        {
            this.label = label;
            this.collection = collection;

            InitializeEnumerableSuffixes();
        }

        public virtual IEnumerator<T> GetEnumerator()
        {
            return collection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Contains(T item)
        {
            return collection.Contains(item);
        }

        public abstract int Count { get; }

        public override string ToString()
        {
            return SerializationMgr.Instance.Serialize(this, TerminalFormatter.Instance, false);
        }

        public IDictionary<object, object> Dump()
        {
            DictionaryWithHeader result = new DictionaryWithHeader();

            result.Header = label + " of " + collection.Count() + " items:";

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
            AddSuffix("ITERATOR",   new NoArgsSuffix<Enumerator>          (() => new Enumerator (collection.GetEnumerator())));
            AddSuffix("CONTAINS",   new OneArgsSuffix<bool, T>            (item => collection.Contains(item)));
            AddSuffix("EMPTY",      new NoArgsSuffix<bool>                (() => !collection.Any()));
            AddSuffix("DUMP",       new NoArgsSuffix<string>              (() => ToString()));
        }
    }
}

