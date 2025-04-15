using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Utilities;
using kOS.Safe.Function;

namespace kOS.Safe.Encapsulation
{
    [kOS.Safe.Utilities.KOSNomenclature("Lexicon")]
    [kOS.Safe.Utilities.KOSNomenclature("Lex", CSharpToKOS = false) ]
    public class Lexicon : SerializableStructure, IDictionary<Structure, Structure>, IIndexable
    {
        [Function("lex", "lexicon")]
        public class FunctionLexicon : SafeFunctionBase
        {
            public override void Execute(SafeSharedObjects shared)
            {

                Structure[] argArray = new Structure[CountRemainingArgs(shared)];
                for (int i = argArray.Length - 1; i >= 0; --i)
                    argArray[i] = PopStructureAssertEncapsulated(shared); // fill array in reverse order because .. stack args.
                AssertArgBottomAndConsume(shared);
                var lexicon = new Lexicon(argArray.ToList());
                ReturnValue = lexicon;
            }
        }

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

                if ((x is string || x is StringValue) && (y is string || y is StringValue))
                {
                    var compare = string.Compare(x.ToString(), y.ToString(), StringComparison.InvariantCultureIgnoreCase);
                    return compare == 0;
                }

                return x.Equals(y);
            }

            public int GetHashCode(TI obj)
            {
                if (obj is string || obj is StringValue)
                {
                    return obj.ToString().ToLower().GetHashCode();
                }
                return obj.GetHashCode();
            }
        }

        private IDictionary<Structure, Structure> internalDictionary;
        private IDictionary<Structure, SetSuffix<Structure>> keySuffixes;
        private bool caseSensitive;

        public Lexicon()
        {
            internalDictionary = new Dictionary<Structure, Structure>(new LexiconComparer<Structure>());
            keySuffixes = new Dictionary<Structure, SetSuffix<Structure>>(new LexiconComparer<Structure>());
            caseSensitive = false;
            InitalizeSuffixes();
        }

        public Lexicon(IEnumerable<Structure> values) : this()
        {
            FillWithEnumerableValues(values);
        }

        public Lexicon(IEnumerable<KeyValuePair<Structure, Structure>> lexicon)
            : this()
        {
            foreach (KeyValuePair<Structure, Structure> u in lexicon)
            {
                internalDictionary.Add(u);
            }
        }

        // Required for all IDumpers for them to work, but can't enforced by the interface because it's static:
        public static Lexicon CreateFromDump(SafeSharedObjects shared, Dump d)
        {
            var newObj = new Lexicon();
            newObj.LoadDump(d);
            return newObj;
        }

        private void FillWithEnumerableValues(IEnumerable<Structure> values)
        {
            if ((values.Count() == 1) && (values.First() is IEnumerable<Structure>)) {
                FillWithEnumerableValues(values.First() as IEnumerable<Structure>);
                return;
            }

            if (values.Count() % 2 == 1) {
                throw new KOSException("Lexicon constructor expects an even number of arguments or a single enumerable type");
            }

            values.Select((value, index) => new {Index = index, Value = value})
                .GroupBy(x => x.Index / 2).ForEach(g => internalDictionary[g.ElementAt(0).Value] = g.ElementAt(1).Value);

        }

        private void InitalizeSuffixes()
        {
            AddSuffix("CLEAR", new NoArgsVoidSuffix(Clear, "Removes all items from Lexicon"));
            AddSuffix("KEYS", new Suffix<ListValue>(GetKeys, "Returns the lexicon keys"));
            AddSuffix("HASKEY", new OneArgsSuffix<BooleanValue, Structure>(HasKey, "Returns true if a key is in the Lexicon"));
            AddSuffix("HASVALUE", new OneArgsSuffix<BooleanValue, Structure>(HasValue, "Returns true if value is in the Lexicon"));
            AddSuffix("VALUES", new Suffix<ListValue>(GetValues, "Returns the lexicon values"));
            AddSuffix("COPY", new NoArgsSuffix<Lexicon>(() => new Lexicon(this), "Returns a copy of Lexicon"));
            AddSuffix("LENGTH", new NoArgsSuffix<ScalarValue>(() => internalDictionary.Count, "Returns the number of elements in the collection"));
            AddSuffix("REMOVE", new OneArgsSuffix<BooleanValue, Structure>(one => Remove(one), "Removes the value at the given key"));
            AddSuffix("ADD", new TwoArgsSuffix<Structure, Structure>(Add, "Adds a new item to the lexicon, will error if the key already exists"));
            AddSuffix("DUMP", new NoArgsSuffix<StringValue>(() => ToString(), "Serializes the collection to a string for printing"));
            AddSuffix(new[] { "CASESENSITIVE", "CASE" }, new SetSuffix<BooleanValue>(() => caseSensitive, SetCaseSensitivity, "Lets you get/set the case sensitivity on the collection, changing sensitivity will clear the collection"));
        }

        private void SetCaseSensitivity(BooleanValue value)
        {
            bool newCase = value.Value;
            if (newCase == caseSensitive)
            {
                return;
            }
            caseSensitive = newCase;

            internalDictionary = newCase ?
                new Dictionary<Structure, Structure>() :
            new Dictionary<Structure, Structure>(new LexiconComparer<Structure>());

            // Regardless of whether or not the lexicon itself is case sensitive,
            // the key Suffixes have to be IN-sensitive because they are getting
            // values who's case got squashed by the compiler.  This needs to
            // be documented well in the user docs (i.e. using the suffix syntax
            // cannot detect the difference between keys that differ only in case).
            keySuffixes = new Dictionary<Structure, SetSuffix<Structure>>(new LexiconComparer<Structure>());
        }

        private BooleanValue HasValue(Structure value)
        {
            return internalDictionary.Values.Contains(value);
        }

        private BooleanValue HasKey(Structure key)
        {
            return internalDictionary.ContainsKey(key);
        }

        public ListValue GetValues()
        {
            return ListValue.CreateList(Values);
        }

        public ListValue GetKeys()
        {
            return ListValue.CreateList(Keys);
        }

        public IEnumerator<KeyValuePair<Structure, Structure>> GetEnumerator()
        {
            return internalDictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<Structure, Structure> item)
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

        public bool Contains(KeyValuePair<Structure, Structure> item)
        {
            return internalDictionary.Contains(item);
        }

        public void CopyTo(KeyValuePair<Structure, Structure>[] array, int arrayIndex)
        {
            internalDictionary.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<Structure, Structure> item)
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

        public bool ContainsKey(Structure key)
        {
            return internalDictionary.ContainsKey(key);
        }

        public void Add(Structure key, Structure value)
        {
            if (internalDictionary.ContainsKey(key))
            {
                throw new KOSDuplicateKeyException(key.ToString(), caseSensitive);
            }
            internalDictionary.Add(key, value);
        }

        public bool Remove(Structure key)
        {
            return internalDictionary.Remove(key);
        }

        public bool TryGetValue(Structure key, out Structure value)
        {
            return internalDictionary.TryGetValue(key, out value);
        }

        public Structure this[Structure key]
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

        public ICollection<Structure> Keys
        {
            get
            {
                return internalDictionary.Keys;
            }
        }

        public ICollection<Structure> Values
        {
            get
            {
                return internalDictionary.Values;
            }
        }

        public Structure GetIndex(Structure key)
        {
            return internalDictionary[key];
        }

        // Only needed because IIndexable demands it.  For a lexicon, none of the code is
        // actually trying to call this:
        public Structure GetIndex(int index, bool failOkay = false)
        {
            try { return internalDictionary[FromPrimitiveWithAssert(index)]; }
            catch { if (failOkay) return null; throw; }
        }

        public void SetIndex(Structure index, Structure value)
        {
            internalDictionary[index] = value;
        }

        // Only needed because IIndexable demands it.  For a lexicon, none of the code is
        // actually trying to call this:
        public void SetIndex(int index, Structure value)
        {
            internalDictionary[FromPrimitiveWithAssert(index)] = value;
        }

        public override string ToString()
        {
            return new SafeSerializationMgr(null).ToString(this);
        }

        // Try to call the normal SetSuffix that all structures do, but if that fails,
        // then try to use this suffix name as a key and set the value in the lexicon
        // at that key.  This can insert new key values in the lexicon, just like
        // doing `set x["foo"] to y.` can.
        public override bool SetSuffix(string suffixName, object value, bool failOkay = false)
        {
            if (base.SetSuffix(suffixName, value, true))
                return true;

            // If the above fails, then fallback on the key technique:
            internalDictionary[new StringValue(suffixName)] = FromPrimitiveWithAssert(value);
            return true;
        }

        // Try to get the suffix the normal way that all structures do, but if
        // that fails, then try to get the value in the lexicon who's key is
        // this suffix name. (This implements using keys with the "colon" suffix
        // syntax for issue #2551.)
        public override ISuffixResult GetSuffix(string suffixName, bool failOkay = false)
        {
            ISuffixResult baseResult = base.GetSuffix(suffixName, true);
            if (baseResult != null)
                return baseResult;

            // If the above fails, but this suffix IS the name of a key in the
            // dictionary, then try to use the key-suffix we made earlier
            // (or make a new one and use it now)
            // ---------------------------------------------------------------

            StringValue suffixAsStruct = new StringValue(suffixName);

            if (internalDictionary.ContainsKey(suffixAsStruct)) // even if keySuffixes has the value, it doesn't count if the key isn't there anymore.
            {
                SetSuffix<Structure> theSuffix;
                if (keySuffixes.TryGetValue(suffixAsStruct, out theSuffix))
                {
                    return theSuffix.Get();
                }
                else // make a new suffix then since this is the first time it got mentioned this way:
                {
                    theSuffix = new SetSuffix<Structure>(() => internalDictionary[suffixAsStruct], value => internalDictionary[suffixAsStruct] = value);
                    keySuffixes.Add(suffixAsStruct, theSuffix);
                    return theSuffix.Get();
                }
            }
            else
            {
                // This will error out, but we may as well also remove this key
                // from the list of suffixes:
                keySuffixes.Remove(suffixAsStruct);

                if (failOkay)
                    return null;
                else
                    throw new KOSSuffixUseException("get", suffixName, this);
            }
        }

        public override BooleanValue HasSuffix(StringValue suffixName)
        {
            if (base.HasSuffix(suffixName))
                return true;
            if (internalDictionary.ContainsKey(suffixName))
            {
                // It can only be a suffix if it is a valid identifier pattern, else the
                // parser won't let the colon suffix syntax see it to pass it to GetSuffix()
                // or SetSuffix():
                return StringUtil.IsValidIdentifier(suffixName);
            }
            return false;
        }

        /// <summary>
        /// Like normal Structure.GetSuffixNames except it also adds all
        /// the keys that would validly work with the colon suffix syntax
        /// to the list.
        /// </summary>
        /// <returns></returns>
        public override ListValue GetSuffixNames()
        {
            ListValue theList = base.GetSuffixNames();

            foreach (Structure key in internalDictionary.Keys)
            {
                StringValue keyStr = key as StringValue;
                if (keyStr != null && StringUtil.IsValidIdentifier(keyStr))
                {
                    theList.Add(keyStr);
                }
            }
            return new ListValue(theList.OrderBy(item => item.ToString()));
        }
        public override Dump Dump()
        {
            var result = new DumpWithHeader
            {
                Header = "LEXICON of " + internalDictionary.Count() + " items:"
            };

            List<object> list = new List<object>();

            foreach (KeyValuePair<Structure, Structure> entry in internalDictionary)
            {
                list.Add(entry.Key);
                list.Add(entry.Value);
            }

            result.Add(kOS.Safe.Dump.Entries, list);

            return result;
        }

        public override void LoadDump(Dump dump)
        {
            internalDictionary.Clear();

            List<object> values = (List<object>)dump[kOS.Safe.Dump.Entries];

            for (int i = 0; 2 * i < values.Count; i++)
            {
                internalDictionary.Add(Structure.FromPrimitiveWithAssert(values[2 * i]), Structure.FromPrimitiveWithAssert(values[2 * i + 1]));
            }
        }
    }
}