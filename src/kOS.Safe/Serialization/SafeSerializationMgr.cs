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

        public IDictionary<object, object> Dump(IDumper dumper, bool includeType = true)
        {
            var dumped = dumper.Dump();

            List<object> keys = new List<object>(dumped.Keys);

            foreach (object key in keys)
            {
                var dump = dumped[key] as IDumper;
                if (dump != null)
                {
                    dumped[key] = Dump(dump, includeType);
                } else if (IsEncapsulatedValue(dumped[key]) || IsValue(dumped[key]))
                {
                    dumped[key] = Structure.ToPrimitive(dumped[key]);
                } else
                {
                    throw new KOSException("This type can't be serialized: " + dumped[key].GetType().Name);
                }
            }

            if (includeType)
            {
                dumped.Add(TYPE_KEY, dumper.GetType().Namespace + "." + dumper.GetType().Name);
            }

            return dumped;
        }

        public string Serialize(IDumper serialized, IFormatWriter formatter, bool includeType = true)
        {
            return formatter.Write(Dump(serialized, includeType));
        }

        public IDumper CreateFromDump(IDictionary<object, object> dump)
        {
            var data = new Dictionary<object, object>();
            foreach (KeyValuePair<object, object> entry in dump)
            {
                if (entry.Key.Equals(TYPE_KEY))
                {
                    continue;
                }

                var objects = entry.Value as IDictionary<object, object>;
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

        public virtual IDumper CreateInstance(string typeFullName, IDictionary<object, object> data)
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
            IDictionary<object, object> dump = formatter.Read(input);

            return dump == null ? null : CreateFromDump(dump);
        }

        public string ToString(IDumper dumper)
        {
            return Serialize(dumper, TerminalFormatter.Instance, false);
        }
    }
}

