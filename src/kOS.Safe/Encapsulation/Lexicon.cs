using System;
using System.Collections;
using System.Collections.Generic;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Safe.Encapsulation
{
    public class Lexicon<T, TU> : Structure, IDictionary<T,TU>, ILexicon
    {
        public class LexiconComparer<TI> : IEqualityComparer<TI>
        {
            public bool Equals(TI x, TI y)
            {
                throw new NotImplementedException();
            }

            public int GetHashCode(TI obj)
            {
                throw new NotImplementedException();
            }
        }

        private readonly IDictionary<T, TU> internalDictionary;

        public Lexicon()
        {
            internalDictionary = new Dictionary<T, TU>(new LexiconComparer<T>());
            InitalizeSuffixes();
        }

        private void InitalizeSuffixes()
        {
            AddSuffix("CLEAR", new NoArgsSuffix(Clear,"Removes all items from Lexicon"));
            AddSuffix("KEYS", new Suffix<ListValue<object>>(GetKeys ,"Returns the available keys"));
            AddSuffix("VALUES", new Suffix<ListValue<object>>(GetValues ,"Returns the available list values"));
            AddSuffix("REMOVE", new OneArgsSuffix<bool, object>(one => Remove((T) one)));
        }

        public ListValue<object> GetValues()
        {
            return ListValue.CreateList(Values);
        }

        public ListValue<object> GetKeys()
        {
            return ListValue.CreateList(Keys);
        }

        public IEnumerator<KeyValuePair<T, TU>> GetEnumerator()
        {
            return internalDictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<T, TU> item)
        {
            internalDictionary.Add(item);
        }

        public void Clear()
        {
            internalDictionary.Clear();
        }

        public bool Contains(KeyValuePair<T, TU> item)
        {
            return internalDictionary.Contains(item);
        }

        public void CopyTo(KeyValuePair<T, TU>[] array, int arrayIndex)
        {
            internalDictionary.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<T, TU> item)
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

        public bool ContainsKey(T key)
        {
            return internalDictionary.ContainsKey(key);
        }

        public void Add(T key, TU value)
        {
            internalDictionary.Add(key, value);
        }

        public bool Remove(T key)
        {
            return internalDictionary.Remove(key);
        }

        public bool TryGetValue(T key, out TU value)
        {
            return internalDictionary.TryGetValue(key, out value);
        }

        public TU this[T key]
        {
            get { return internalDictionary[key]; }
            set { internalDictionary[key] = value; }
        }

        public ICollection<T> Keys { get { return internalDictionary.Keys; } }
        public ICollection<TU> Values { get { return internalDictionary.Values; } }

        public object GetKey(object key)
        {
            return internalDictionary[(T) key];
        }

        public void SetKey(object index, object value)
        {
            internalDictionary[(T) index] = (TU) value;
        }
    }

    public interface ILexicon
    {
        object GetKey(object key);
        void SetKey(object index, object value);
    }
}