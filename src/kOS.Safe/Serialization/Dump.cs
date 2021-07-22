using System;
using System.Collections.Generic;
using kOS.Safe.Encapsulation;
using kOS.Safe.Serialization;
using kOS.Safe.Exceptions;
using System.Reflection;

namespace kOS.Safe
{
    public class Dump
    {
        public virtual bool IsSerializable { get { return false; } }
        public virtual Dictionary<string, string> DebugInfo { get; private set; }

        public Dump()
        {
            DebugInfo = new Dictionary<string, string>();
        }

        public virtual object ToJsonObject()
        {
            return null;
        }

        public virtual void WriteReadable(IndentedStringBuilder sb)
        {
            sb.Append("<unknown>");
        }

        public virtual Structure ToStructure(SafeSharedObjects sharedObjects)
        {
            return null;
        }

    }

    public class DumpOpaque : Dump
    {
        public DumpOpaque(string classname)
        {
            this.classname = classname;
        }

        private string classname;

        public override void WriteReadable(IndentedStringBuilder sb)
        {
            sb.Append("<");
            sb.Append(classname);
            sb.Append(">");
        }
    }

    public class DumpRecursionPlaceholder : Dump
    {
        public override void WriteReadable(IndentedStringBuilder sb)
        {
            sb.Append("<recursion truncated>");
        }
    }

    public abstract class DeserializableDump : Dump
    {
        protected Type deserializer = null;
        public DeserializableDump(Type deserializer)
        {
            if (deserializer == null)
                throw new ArgumentNullException("Deserializer cannot be null", "deserializer");
            this.deserializer = deserializer;
        }

        protected Structure Deserialize<T>(T dump, SafeSharedObjects safeSharedObjects) where T : Dump
        {
            Type[] paramSignature = new Type[] { typeof(T), typeof(SafeSharedObjects) };
            MethodInfo method = deserializer.GetMethod("CreateFromDump", BindingFlags.Public | BindingFlags.Static, null, paramSignature, null);
            if (method == null)
                throw new KOSYouShouldNeverSeeThisException(String.Format("{0} is supposed to be able to deserialize {1} objects but it does not implement the `public static {0} CreateFromDump({1} dump, SafeSharedObjects shared)` method to do so.", deserializer.Name, typeof(T).Name));

            Structure instance;
            try
            {
                instance = (Structure)method.Invoke(null, new object[] { safeSharedObjects, dump });
            }
            catch (TargetInvocationException reflectiveCallException)
            {
                // When you call a method via reflection with MethodInfo.Invoke(),
                // it hides any exceptions that method tried to throw inside its own
                // wrapper called TargetInvocationException.  That would mask our
                // intended error messages to the user if we didn't re-throw the actual 
                // exception the method wanted to generate like so:
                throw reflectiveCallException.InnerException;
            }
            return instance;
        }

        protected void Print<T>(T dump, IndentedStringBuilder sb) where T : Dump
        {
            Type[] paramSignature = new Type[] { typeof(T), typeof(IndentedStringBuilder) };
            MethodInfo method = deserializer.GetMethod("PrintDump", BindingFlags.Public | BindingFlags.Static, null, paramSignature, null);
            if (method == null)
                throw new KOSYouShouldNeverSeeThisException(String.Format("{0} is supposed to be able to print {1} objects but it does not implement the `public static void Print({1} dump, IndentedStringBuilder sb)` method to do so.", deserializer.Name, typeof(T).Name));

            try
            {
                method.Invoke(null, new object[] { dump, sb });
            }
            catch (TargetInvocationException reflectiveCallException)
            {
                // When you call a method via reflection with MethodInfo.Invoke(),
                // it hides any exceptions that method tried to throw inside its own
                // wrapper called TargetInvocationException.  That would mask our
                // intended error messages to the user if we didn't re-throw the actual 
                // exception the method wanted to generate like so:
                throw reflectiveCallException.InnerException;
            }
        }
    }

    public class DumpDictionary : DeserializableDump
    {
        public DumpDictionary(Type deserializer) : base(deserializer) { }

        private Dictionary<string, Dump> dumpItems = new Dictionary<string, Dump>();
        private Dictionary<string, object> primitiveItems = new Dictionary<string, object>();
        private bool hasUnserializableChildren = false;
        public override bool IsSerializable { get { return !hasUnserializableChildren; } }
        public void Add(string key, IDumper value, IDumperContext c)
        {
            if (key == null)
                throw new ArgumentNullException("Unable to add items to a null key", "key");
            if (key == "items" || key == "entries")
                throw new ArgumentException(String.Format("Key cannot be {0}. Please use a different key or consider using DumpList or DumpLexicon.", key), "key");
            if (dumpItems.ContainsKey(key) || primitiveItems.ContainsKey(key))
                throw new ArgumentException("An element with the same key already exists in this Dictionary.", "key");
            if (value == null)
                throw new ArgumentNullException("Unable to add null value to dump", "value");
            if (c == null)
                throw new ArgumentNullException("A context is required to convert objects into Dumps.", "c");

            var valueDump = c.Convert(value);

            hasUnserializableChildren = hasUnserializableChildren || !valueDump.IsSerializable;

            dumpItems.Add(key, valueDump);
        }

        public void Add(string key, double value)
        {
            if (key == null)
                throw new ArgumentNullException("Unable to add items to a null key", "key");
            if (key == "items" || key == "entries")
                throw new ArgumentException(String.Format("Key cannot be {0}. Please use a different key or consider using DumpList or DumpLexicon.", key), "key");
            if (dumpItems.ContainsKey(key) || primitiveItems.ContainsKey(key))
                throw new ArgumentException("An element with the same key already exists in this Dictionary.", "key");

            primitiveItems.Add(key, value);
        }
        public void Add(string key, bool value)
        {
            if (key == null)
                throw new ArgumentNullException("Unable to add items to a null key", "key");
            if (key == "items" || key == "entries")
                throw new ArgumentException(String.Format("Key cannot be {0}. Please use a different key or consider using DumpList or DumpLexicon.", key), "key");
            if (dumpItems.ContainsKey(key) || primitiveItems.ContainsKey(key))
                throw new ArgumentException("An element with the same key already exists in this Dictionary.", "key");

            primitiveItems.Add(key, value);
        }

        public void Add(string key, string value)
        {
            if (key == null)
                throw new ArgumentNullException("Unable to add items to a null key", "key");
            if (dumpItems.ContainsKey(key) || primitiveItems.ContainsKey(key))
                throw new ArgumentException("An element with the same key already exists in this Dictionary.", "key");

            primitiveItems.Add(key, value);
        }

        public string GetString(string key)
        {
            if (!primitiveItems.ContainsKey(key))
                throw new KOSSerializationException(string.Format("Missing key {0} when trying to parse {1}", key, deserializer.Name));

            if (primitiveItems[key] is string)
                return (string)primitiveItems[key];

            throw new KOSSerializationException(string.Format("Key {0} was expected to be a string but is actually a {1}.", key, primitiveItems[key].GetType().Name));
        }

        public double GetDouble(string key)
        {
            if (!primitiveItems.ContainsKey(key))
                throw new KOSSerializationException(string.Format("Missing key {0} when trying to parse {1}", key, deserializer.Name));

            if (primitiveItems[key] is double)
                return (double)primitiveItems[key];

            throw new KOSSerializationException(string.Format("Key {0} was expected to be a double but is actually a {1}.", key, primitiveItems[key].GetType().Name));
        }

        public bool GetBool(string key)
        {
            if (!primitiveItems.ContainsKey(key))
                throw new KOSSerializationException(string.Format("Missing key {0} when trying to parse {1}", key, deserializer.Name));

            if (primitiveItems[key] is bool)
                return (bool)primitiveItems[key];

            throw new KOSSerializationException(string.Format("Key {0} was expected to be a bool but is actually a {1}.", key, primitiveItems[key].GetType().Name));
        }

        public Dump GetDump(string key)
        {
            if (!dumpItems.ContainsKey(key))
                throw new KOSSerializationException(string.Format("Missing key {0} when trying to parse {1}", key, deserializer.Name));

            return dumpItems[key];
        }

        public Structure GetStructure(string key, SafeSharedObjects sharedObjects)
        {
            return GetDump(key).ToStructure(sharedObjects);
        }
        public override object ToJsonObject()
        {
            throw new NotImplementedException();
        }

        public override void WriteReadable(IndentedStringBuilder sb)
        {
            Print(this, sb);
        }

        public override Structure ToStructure(SafeSharedObjects sharedObjects)
        {
            return Deserialize(this, sharedObjects);
        }
    }
    public class DumpList : DeserializableDump
    {
        // TODO: dump with Items
        public DumpList(Type deserializer) : base(deserializer) { }

        public override bool IsSerializable { get { return !hasUnserializableChildren; } }
        public int Count { get { return items.Count; } }

        private List<Dump> items = new List<Dump>();
        private bool hasUnserializableChildren = false;

        public void Add(IDumper value, IDumperContext c)
        {
            if (value == null)
                throw new ArgumentNullException("Unable to add null value to dump", "value");
            if (c == null)
                throw new ArgumentNullException("A context is required to convert objects into Dumps.", "c");

            var valueDump = c.Convert(value);

            hasUnserializableChildren = hasUnserializableChildren || !valueDump.IsSerializable;

            items.Add(valueDump);
        }

        public Structure Get(int i, SafeSharedObjects sharedObjects)
        {
            return items[i].ToStructure(sharedObjects);
        }
    }

    public class DumpLexicon : DeserializableDump
    {
        // TODO: dump with Entries
        public DumpLexicon(Type deserializer) : base(deserializer) { }

        public int Count { get { return keys.Count; } }

        private List<Dump> values = new List<Dump>();
        private List<Dump> keys = new List<Dump>();
        private bool hasUnserializableChildren = false;

        public void Add(IDumper key, IDumper value, IDumperContext c)
        {
            if (key == null)
                throw new ArgumentNullException("Unable to add null key to dump", "key");
            if (value == null)
                throw new ArgumentNullException("Unable to add null value to dump", "value");
            if (c == null)
                throw new ArgumentNullException("A context is required to convert objects into Dumps.", "c");

            var valueDump = c.Convert(value);
            var keyDump = c.Convert(key);

            hasUnserializableChildren = hasUnserializableChildren || !valueDump.IsSerializable;

            values.Add(valueDump);
            keys.Add(keyDump);
        }

        public IEnumerable<KeyValuePair<Dump, Dump>> GetItems()
        {
            var result = new List<KeyValuePair<Dump, Dump>>();

            for (int i = 0; i < values.Count; i++)
            {
                result.Add(new KeyValuePair<Dump, Dump>(keys[i], values[i]));
            }

            return result;
        }
        public IEnumerable<KeyValuePair<Structure, Structure>> GetStructures(SafeSharedObjects sharedObjects)
        {
            var result = new List<KeyValuePair<Structure, Structure>>();

            for (int i = 0; i < values.Count; i++)
            {
                result.Add(new KeyValuePair<Structure, Structure>(keys[i].ToStructure(sharedObjects), values[i].ToStructure(sharedObjects)));
            }

            return result;
        }
    }
}
