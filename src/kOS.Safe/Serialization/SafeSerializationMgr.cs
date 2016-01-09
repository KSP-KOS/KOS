using System;
using System.Collections.Generic;
using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;

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

        public Dump Dump(IDumper dumper, bool includeType = true)
        {
            var dump = dumper.Dump();

            List<object> keys = new List<object>(dump.Keys);

            foreach (object key in keys)
            {
                var value = dump[key];
                var valueDumper = value as IDumper;

                if (valueDumper != null)
                {
                    dump[key] = Dump(valueDumper, includeType);
                } else if (IsEncapsulatedValue(value) || IsValue(value))
                {
                    dump[key] = Structure.ToPrimitive(value);
                } else
                {
                    dump[key] = value.ToString();
                }
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

        public IDumper CreateFromDump(Dump dump)
        {
            var data = new Dump();
            foreach (KeyValuePair<object, object> entry in dump)
            {
                if (entry.Key.Equals(TYPE_KEY))
                {
                    continue;
                }

                var objects = entry.Value as Dump;
                if (objects != null)
                {
                    data[entry.Key] = CreateFromDump(objects);
                } else
                {
                    data[entry.Key] = entry.Value;
                }
            }

            string typeFullName = dump[TYPE_KEY] as string;

            if (String.IsNullOrEmpty(typeFullName))
            {
                throw new KOSSerializationException("Type information missing");
            }

            return CreateInstance(typeFullName, data);
        }

        public virtual IDumper CreateInstance(string typeFullName, Dump data)
        {
            var deserializedType = Type.GetType(typeFullName);

            if (deserializedType == null)
            {
                throw new KOSSerializationException("Unrecognized type: " + typeFullName);
            }

            IDumper instance = Activator.CreateInstance(deserializedType) as IDumper;

            instance.LoadDump(data);

            return instance;
        }

        public object Deserialize(string input, IFormatReader formatter)
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

