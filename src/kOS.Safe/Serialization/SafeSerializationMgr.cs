using System;
using System.Collections.Generic;
using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;
using System.Linq;

namespace kOS.Safe.Serialization
{
    public class SafeSerializationMgr
    {
        public static string TYPE_KEY = "$type";
        private static HashSet<string> assemblies = new HashSet<string>();

        public static void AddAssembly(string assembly)
        {
            assemblies.Add(assembly);
        }

        public static bool IsSerializablePrimitive(object serialized)
        {
            return serialized.GetType().IsPrimitive || serialized is string || IsPrimitiveStructure(serialized);
        }

        public static bool IsPrimitiveStructure(object serialized)
        {
            return serialized is PrimitiveStructure;
        }

        private object DumpValue(object value, bool includeType)
        {
            var valueDumper = value as SerializableStructure;

            if (valueDumper != null) {
                return Dump(valueDumper, includeType);
            } else if (value is List<object>) {
                return (value as List<object>).Select((v) => DumpValue(v, includeType)).ToList();
            } else if (IsSerializablePrimitive(value)) {
                return Structure.ToPrimitive(value);
            } else {
                return value.ToString();
            }
        }

        public Dump Dump(SerializableStructure serializableStructure, bool includeType = true)
        {
            var dump = serializableStructure.Dump();

            List<object> keys = new List<object>(dump.Keys);

            foreach (object key in keys)
            {
                dump[key] = DumpValue(dump[key], includeType);
            }

            if (includeType)
            {
                dump.Add(TYPE_KEY, serializableStructure.GetType().Namespace + "." + serializableStructure.GetType().Name);
            }

            return dump;
        }

        public string Serialize(SerializableStructure serialized, IFormatWriter formatter, bool includeType = true)
        {
            return formatter.Write(Dump(serialized, includeType));
        }

        public object CreateValue(object value)
        {
            var objects = value as Dump;
            if (objects != null)
            {
                return CreateFromDump(objects);
            } else if (value is List<object>)
            {
                return (value as List<object>).Select(item => CreateValue(item)).ToList();
            }

            return value;
        }

        public SerializableStructure CreateFromDump(Dump dump)
        {
            var data = new Dump();

            foreach (KeyValuePair<object, object> entry in dump)
            {
                if (entry.Key.Equals(TYPE_KEY))
                {
                    continue;
                }

                data[entry.Key] = CreateValue(entry.Value);
            }

            if (!dump.ContainsKey(TYPE_KEY))
            {
                throw new KOSSerializationException("Type information missing");
            }

            string typeFullName = dump[TYPE_KEY] as string;

            return CreateAndLoad(typeFullName, data);
        }

        public virtual SerializableStructure CreateAndLoad(string typeFullName, Dump data)
        {
            SerializableStructure instance = CreateInstance(typeFullName);

            instance.LoadDump(data);

            return instance;
        }

        public virtual SerializableStructure CreateInstance(string typeFullName)
        {
            var deserializedType = Type.GetType(typeFullName);

            if (deserializedType == null)
            {
                foreach (string assembly in assemblies)
                {
                    deserializedType = Type.GetType(typeFullName + ", " + assembly);
                    if (deserializedType != null)
                    {
                        break;
                    }
                }
            }

            return Activator.CreateInstance(deserializedType) as SerializableStructure;
        }

        public SerializableStructure Deserialize(string input, IFormatReader formatter)
        {
            Dump dump = formatter.Read(input);

            return dump == null ? null : CreateFromDump(dump);
        }

        public string ToString(SerializableStructure dumper)
        {
            return Serialize(dumper, TerminalFormatter.Instance, false);
        }
    }
}

