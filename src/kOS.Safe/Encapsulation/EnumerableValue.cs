using System;
using kOS.Safe.Encapsulation;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using kOS.Safe.Encapsulation.Suffixes;

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

        public IEnumerator<T> GetEnumerator()
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

        public string[] Dump(int limit, int depth = 0)
        {
            var toReturn = new List<string>();

            var listString = string.Format(label + " of {0} items", Count);
            toReturn.Add(listString);

            if (limit <= 0) return toReturn.ToArray();

            int index = 0;
            foreach (var item in collection)
            {
                var dumper = item as IDumper;
                if (dumper != null)
                {
                    var entry = string.Empty.PadLeft(depth * INDENT_SPACES);

                    var itemDump = dumper.Dump(limit - 1, depth + 1);

                    var itemString = string.Format("  [{0,2}]= {1}", index, itemDump[0]);
                    entry += itemString;

                    toReturn.Add(entry);

                    for (int i = 1; i < itemDump.Length; i++)
                    {
                        var subEntry = string.Format("{0}", itemDump[i]);
                        toReturn.Add(subEntry);
                    }
                }
                else
                {
                    var entry = string.Empty.PadLeft(depth * INDENT_SPACES);
                    entry += string.Format("  [{0,2}]= {1}", index, item);
                    toReturn.Add(entry);
                }

                index++;
            }
            return toReturn.ToArray();
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, Dump(1));
        }

        private void InitializeEnumerableSuffixes()
        {
            AddSuffix("ITERATOR",   new NoArgsSuffix<Enumerator>          (() => new Enumerator (collection.GetEnumerator())));
            AddSuffix("CONTAINS",   new OneArgsSuffix<bool, T>            (item => collection.Contains(item)));
            AddSuffix("EMPTY",      new NoArgsSuffix<bool>                (() => !collection.Any()));
            AddSuffix("DUMP",       new NoArgsSuffix<string>              (() => string.Join(Environment.NewLine, Dump(99))));
        }
    }
}

