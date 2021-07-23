using System;
using System.Collections.Generic;
using kOS.Safe.Encapsulation;
using kOS.Safe.Serialization;
using kOS.Safe.Exceptions;
using System.Reflection;
using System.Linq;

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

        public virtual JsonObject ToJsonObject()
        {
            throw new NotImplementedException();
        }

        public virtual void WriteReadable(IndentedStringBuilder sb)
        {
            sb.Append("<unknown>");
        }

        public virtual Structure ToStructure(SafeSharedObjects sharedObjects)
        {
            return null;
        }

        public static Dump FromJson(JsonObject json)
        {
            string typename = json["$type"] as string;
            if (typename == null)
                throw new KOSSerializationException("Unable to deserialize object without $type string.");

            var destinationTypes = AppDomain.CurrentDomain.GetAssemblies().
                Where(a => !a.IsDynamic).
                Select(a => a.GetType(typename)).
                ToList();
            if (destinationTypes.Count > 1)
                throw new KOSYouShouldNeverSeeThisException("Multiple definitions found of " + typename);
            if (!destinationTypes.Any())
                throw new KOSSerializationException("Unable to deserialize class with type " + typename);
            Type destinationType = destinationTypes.First();

            Type dumpType = null;

            foreach (var method in destinationType.GetMethods())
            {
                if (!method.IsStatic)
                    continue;

                var printAttr = method.GetCustomAttribute<DumpPrinter>();
                if (printAttr == null)
                    continue;

                dumpType = printAttr.DumpType;
                break;
            }

            if (dumpType == null)
                throw new KOSSerializationException(string.Format("Objects of type {0} do not support deserialization.", typename));

            if (!dumpType.IsSubclassOf(typeof(DeserializableDump)))
                throw new KOSYouShouldNeverSeeThisException("Not sure how this can happen but it would be bad.");

            // TODO, do switch statement and call static CreateFromJson
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
        protected Type deserializer { get; private set; }
        protected Delegate deserializerDelegate { get; private set; }
        protected Delegate printerDelegate { get; private set; }

        public DeserializableDump(Type deserializer)
        {
            if (deserializer == null)
                throw new ArgumentNullException("Deserializer cannot be null", "deserializer");
            this.deserializer = deserializer;

            MethodInfo deserializeMethod = null;
            MethodInfo printMethod = null;

            var methods = deserializer.GetMethods();
            foreach (var method in methods)
            {
                if (!method.IsStatic)
                    continue;

                var printAttr = method.GetCustomAttribute<DumpPrinter>();
                var deserializeAttr = method.GetCustomAttribute<DumpDeserializer>();

                if (printAttr != null)
                {
                    if (printMethod != null)
                        throw new KOSYouShouldNeverSeeThisException("Duplicate PrintDump function defined on " + deserializer.FullName);
                    printMethod = method;
                }

                if (deserializeAttr != null)
                {
                    if (deserializeMethod != null)
                        throw new KOSYouShouldNeverSeeThisException("Duplicate DeserializeDump function defined on " + deserializer.FullName);
                    deserializeMethod = method;
                }
            }

            if (printMethod == null)
                throw new KOSYouShouldNeverSeeThisException("Dump created without corresponding print method in: " + deserializer.FullName);

            var finalPrintAttr = printMethod.GetCustomAttribute<DumpPrinter>();

            if (deserializeMethod != null)
            {
                if (finalPrintAttr.DumpType != deserializeMethod.GetCustomAttribute<DumpDeserializer>().DumpType)
                    throw new KOSYouShouldNeverSeeThisException("Deserializable class contains conflicting Dump types in PrintDump and DeserializeDump: " + deserializer.FullName);
            }

            if (finalPrintAttr.DumpType != GetType())
                throw new KOSYouShouldNeverSeeThisException(String.Format(
                    "Deserializable class {0} expects to print using {1} but created a {2}.",
                    deserializer.FullName,
                    finalPrintAttr.DumpType.FullName,
                    GetType().FullName
                ));

            //void Print(T dump, IndentedStringBuilder sb)
            Type printType = typeof(Action<,>).MakeGenericType(finalPrintAttr.DumpType, typeof(IndentedStringBuilder));
            //StringValue CreateFromDump(T d, SafeSharedObjects shared)
            Type deserializeType = typeof(Func<,,>).MakeGenericType(finalPrintAttr.DumpType, typeof(SafeSharedObjects), deserializer);

            try
            {
                printerDelegate = printMethod.CreateDelegate(printType);
            }
            catch (System.ArgumentException) { }
            if (printerDelegate == null)
                throw new KOSYouShouldNeverSeeThisException(string.Format(
                    "Class {0} defines a Printer function with the wrong type. Please use void Print({1} dump, IndentedStringBuilder sb).",
                    deserializer.FullName,
                    finalPrintAttr.DumpType.FullName
                ));

            if (deserializeMethod != null)
            {
                try {
                deserializerDelegate = deserializeMethod.CreateDelegate(deserializeType);
                }
                catch (System.ArgumentException) { }

                if (deserializerDelegate == null)
                    throw new KOSYouShouldNeverSeeThisException(string.Format(
                        "Class {0} defines a Deserialization function with the wrong type. Please use StringValue CreateFromDump(SafeSharedObjects shared, {1} d)",
                        deserializer.FullName,
                        finalPrintAttr.DumpType.FullName
                    ));
            }
        }

        protected Structure Deserialize<T>(T dump, SafeSharedObjects safeSharedObjects) where T : Dump
        {
            if (typeof(T) != GetType())
                throw new KOSYouShouldNeverSeeThisException("Tried to deserialize with the wrong Dump type.");
            return (Structure)deserializerDelegate.DynamicInvoke(dump, safeSharedObjects);
        }

        protected void Print<T>(T dump, IndentedStringBuilder sb) where T : Dump
        {
            if (typeof(T) != GetType())
                throw new KOSYouShouldNeverSeeThisException("Tried to deserialize with the wrong Dump type.");
            printerDelegate.DynamicInvoke(dump, sb);
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
        public override JsonObject ToJsonObject()
        {
            var result = new JsonObject();

            foreach (var primitiveKv in primitiveItems)
                result.Add(primitiveKv.Key, primitiveKv.Value);
            foreach (var dumpKv in dumpItems)
                result.Add(dumpKv.Key, dumpKv.Value.ToJsonObject());

            result.Add("$type", deserializer.FullName);
            return result;
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

        public Dump this[int i]
        {
            get
            {
                return items[i];
            }
        }

        public Structure GetDeserialized(int i, SafeSharedObjects sharedObjects)
        {
            return items[i].ToStructure(sharedObjects);
        }

        public override JsonObject ToJsonObject()
        {
            var result = new JsonObject();

            var arr = new JsonArray();
            result.Add("items", arr);

            foreach (var i in items)
            {
                arr.Add(i.ToJsonObject());
            }

            result.Add("$type", deserializer.FullName);
            return result;
        }
    }

    public class DumpLexicon : DeserializableDump
    {
        public DumpLexicon(Type deserializer) : base(deserializer) { }

        public int Count { get { return keys.Count; } }
        public override bool IsSerializable { get { return !hasUnserializableChildren; } }


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

        public override JsonObject ToJsonObject()
        {
            var result = new JsonObject();

            var entries = new JsonArray();
            result.Add("entries", entries);

            foreach(var kv in GetItems())
            {
                entries.Add(kv.Key.ToJsonObject());
                entries.Add(kv.Value.ToJsonObject());
            }

            result.Add("$type", deserializer.FullName);
            return result;
        }
    }
}
