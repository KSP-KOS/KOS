using System;
using System.Collections.Generic;
using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;
using System.Linq;
using kOS.Safe.Utilities;

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
            var valueDumper = value as IDumper;

            if (valueDumper != null) {
                return Dump(valueDumper, includeType);
            } else if (value is Dump) {
                return value;
            } else if (value is List<object>) {
                return (value as List<object>).Select((v) => DumpValue(v, includeType)).ToList();
            } else if (IsSerializablePrimitive(value)) {
                return Structure.ToPrimitive(value);
            } else {
                return value.ToString();
            }
        }

        public Dump Dump(IDumper dumper, bool includeType = true)
        {
            var dump = dumper.Dump();

            List<object> keys = new List<object>(dump.Keys);

            foreach (object key in keys)
            {
                dump[key] = DumpValue(dump[key], includeType);
            }

            if (includeType)
            {
                dump.Add(TYPE_KEY, dumper.GetType().Namespace + "." + dumper.GetType().Name);
            }

            return dump;
        }

        public string Serialize(IDumper serialized, IFormatWriter formatter, bool includeType = true)
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

        public IDumper CreateFromDump(Dump dump)
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

        public virtual IDumper CreateAndLoad(string typeFullName, Dump data)
        {
            IDumper instance = CreateInstance(typeFullName);

            instance.LoadDump(data);

            return instance;
        }

        public virtual IDumper CreateInstance(string typeFullName)
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

            return Activator.CreateInstance(deserializedType) as IDumper;
        }

        public IDumper Deserialize(string input, IFormatReader formatter)
        {
            Dump dump = formatter.Read(input);

            return dump == null ? null : CreateFromDump(dump);
        }

        public string ToString(IDumper dumper)
        {
            return Serialize(dumper, TerminalFormatter.Instance, false);
        }
    }
}

