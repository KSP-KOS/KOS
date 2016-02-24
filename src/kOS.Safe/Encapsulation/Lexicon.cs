using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;

namespace kOS.Safe.Encapsulation
{
    public class Lexicon : Structure, IDictionary<object, object>, IIndexable, IDumper
    {
        public class LexiconComparer<TI> : IEqualityComparer<TI>
        {
            public bool Equals(TI x, TI y)
            {
                if (x == null || y == null)
                {
                    return false;
                }

                if (x.GetType() != y.GetType())
                {
                    return false;
                }

                if (x is string && y is string)
                {
                    var compare = string.Compare(x.ToString(), y.ToString(), StringComparison.InvariantCultureIgnoreCase);
                    return compare == 0;
                }

                return x.Equals(y);
            }

            public int GetHashCode(TI obj)
            {
                if (obj is string)
                {
                    return obj.ToString().ToLower().GetHashCode();
                }
                return obj.GetHashCode();
            }
        }

        private IDictionary<object, object> internalDictionary;
        private bool caseSensitive;

        public Lexicon()
        {
            internalDictionary = new Dictionary<object, object>(new LexiconComparer<object>());
            caseSensitive = false;
            InitalizeSuffixes();
        }

        private Lexicon(IEnumerable<KeyValuePair<object, object>> lexicon)
            : this()
        {
            foreach (var u in lexicon)
            {
                internalDictionary.Add(u);
            }
        }

        private void InitalizeSuffixes()
        {
            AddSuffix("CLEAR", new NoArgsSuffix(Clear, "Removes all items from Lexicon"));
            AddSuffix("KEYS", new Suffix<ListValue<object>>(GetKeys, "Returns the lexicon keys"));
            AddSuffix("HASKEY", new OneArgsSuffix<bool, object>(HasKey, "Returns true if a key is in the Lexicon"));
            AddSuffix("HASVALUE", new OneArgsSuffix<bool, object>(HasValue, "Returns true if value is in the Lexicon"));
            AddSuffix("VALUES", new Suffix<ListValue<object>>(GetValues, "Returns the lexicon values"));
            AddSuffix("COPY", new NoArgsSuffix<Lexicon>(() => new Lexicon(this), "Returns a copy of Lexicon"));
            AddSuffix("LENGTH", new NoArgsSuffix<int>(() => internalDictionary.Count, "Returns the number of elements in the collection"));
            AddSuffix("REMOVE", new OneArgsSuffix<bool, object>(Remove, "Removes the value at the given key"));
            AddSuffix("ADD", new TwoArgsSuffix<object, object>(Add, "Adds a new item to the lexicon, will error if the key already exists"));
            AddSuffix("DUMP", new NoArgsSuffix<string>(ToString, "Serializes the collection to a string for printing"));
            AddSuffix(new[] { "CASESENSITIVE", "CASE" }, new SetSuffix<bool>(() => caseSensitive, SetCaseSensitivity, "Lets you get/set the case sensitivity on the collection, changing sensitivity will clear the collection"));
        }

        private void SetCaseSensitivity(bool newCase)
        {
            if (newCase == caseSensitive)
            {
                return;
            }
            caseSensitive = newCase;

            internalDictionary = newCase ?
                new Dictionary<object, object>() :
            new Dictionary<object, object>(new LexiconComparer<object>());
        }

        private bool HasValue(object value)
        {
            return internalDictionary.Values.Contains(value);
        }

        private bool HasKey(object key)
        {
            return internalDictionary.ContainsKey(key);
        }

        public ListValue<object> GetValues()
        {
            return ListValue.CreateList(Values);
        }

        public ListValue<object> GetKeys()
        {
            return ListValue.CreateList(Keys);
        }

        public IEnumerator<KeyValuePair<object, object>> GetEnumerator()
        {
            return internalDictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<object, object> item)
        {
            if (internalDictionary.ContainsKey(item.Key))
            {
                throw new KOSDuplicateKeyException(item.Key.ToString(), caseSensitive);
            }
            internalDictionary.Add(item);
        }

        public void Clear()
        {
            internalDictionary.Clear();
        }

        public bool Contains(KeyValuePair<object, object> item)
        {
            return internalDictionary.Contains(item);
        }

        public void CopyTo(KeyValuePair<object, object>[] array, int arrayIndex)
        {
            internalDictionary.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<object, object> item)
        {
            return internalDictionary.Remove(item);
        }

        public int Count
        {
            get { return internalDictionary.Count; }
        }

        public bool IsReadOnly
        {
            get { return internalDictionary.IsReadOnly; }
        }

        public bool ContainsKey(object key)
        {
            return internalDictionary.ContainsKey(key);
        }

        public void Add(object key, object value)
        {
            if (internalDictionary.ContainsKey(key))
            {
                throw new KOSDuplicateKeyException(key.ToString(), caseSensitive);
            }
            internalDictionary.Add(key, value);
        }

        public bool Remove(object key)
        {
            return internalDictionary.Remove(key);
        }

        public bool TryGetValue(object key, out object value)
        {
            return internalDictionary.TryGetValue(key, out value);
        }

        public object this[object key]
        {
            get
            {
                if (internalDictionary.ContainsKey(key))
                {
                    return internalDictionary[key];
                }
                throw new KOSKeyNotFoundException(key.ToString(), caseSensitive);
            }
            set
            {
                internalDictionary[key] = value;
            }
        }

        public ICollection<object> Keys
        {
            get
            {
                return internalDictionary.Keys;
            }
        }

        public ICollection<object> Values
        {
            get
            {
                return internalDictionary.Values;
            }
        }

        public object GetIndex(object key)
        {
            return internalDictionary[key];
        }

        public void SetIndex(object index, object value)
        {
            internalDictionary[index] = value;
        }

        public override string ToString()
        {
            return new SafeSerializationMgr().ToString(this);
        }

        public IDictionary<object, object> Dump()
        {
            var result = new DictionaryWithHeader((Dictionary<object, object>)internalDictionary)
            {
                Header = "LEXICON of " + internalDictionary.Count + " items:"
            };

            return result;
        }

        public void LoadDump(IDictionary<object, object> dump)
        {
            internalDictionary.Clear();

            foreach (KeyValuePair<object, object> entry in dump)
            {
                internalDictionary.Add(Structure.FromPrimitive(entry.Key), Structure.FromPrimitive(entry.Value));
            }
        }
    }
}