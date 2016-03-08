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

        public static bool IsValue(object serialized)
        {
            return serialized.GetType().IsPrimitive || serialized is string;
        }

        public static bool IsEncapsulatedValue(object serialized)
        {
            return serialized is ISerializableValue;
        }

        private object DumpValue(object value, bool includeType)
        {
            var valueDumper = value as SerializableStructure;

            if (valueDumper != null) {
                return Dump(valueDumper, includeType);
            } else if (value is List<object>) {
                return (value as List<object>).Select((v) => DumpValue(v, includeType)).ToList();
            } else if (IsEncapsulatedValue(value) || IsValue(value)) {
                return Structure.ToPrimitive(value);
            } else {
                return value.ToString();
            }
        }

        public Dump Dump(SerializableStructure dumper, bool includeType = true)
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

                data [entry.Key] = CreateValue (entry.Value);
            }

            string typeFullName = dump[TYPE_KEY] as string;

            if (String.IsNullOrEmpty(typeFullName))
            {
                throw new KOSSerializationException("Type information missing");
            }

            return CreateInstance(typeFullName, data);
        }

        public virtual SerializableStructure CreateInstance(string typeFullName, Dump data)
        {
            var deserializedType = Type.GetType(typeFullName);

            if (deserializedType == null)
            {
                throw new KOSSerializationException("Unrecognized type: " + typeFullName);
            }

            SerializableStructure instance = Activator.CreateInstance(deserializedType) as SerializableStructure;

            instance.LoadDump(data);

            return instance;
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

