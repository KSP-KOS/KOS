using System.Collections;
using System.Collections.Generic;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Safe.Encapsulation
{
    public class Lexicon<T, TU> : Structure, IDictionary<T,TU>, ILexicon where T : struct where TU : struct 
    {

        private readonly IDictionary<T, TU> internalDictionary;

        public Lexicon()
        {
            internalDictionary = new Dictionary<T, TU>();
            InitalizeSuffixes();
        }

        private void InitalizeSuffixes()
        {
            AddSuffix("CLEAR", new NoArgsSuffix(Clear,"Removes all items from Lexicon"));
            AddSuffix("KEYS", new Suffix<ListValue<object>>(() => ListValue.CreateList(Keys) ,"Returns the available keys"));
            AddSuffix("VALUES", new Suffix<ListValue<object>>(() => ListValue.CreateList(Values) ,"Returns the available list values"));
            AddSuffix("REMOVE", new OneArgsSuffix<bool, object>(one => Remove((T) one)));
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