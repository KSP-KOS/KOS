using System;
using System.Collections;
using System.Collections.Generic;
using kOS.Safe.Exceptions;
using kOS.Safe.Encapsulation;

namespace kOS.Safe.Serialization
{
    public class SerializationMgr
    {
        public static string TYPE_KEY = "$type";

        private static readonly SerializationMgr instance = new SerializationMgr();

        public static SerializationMgr Instance
        {
            get 
            {
                return instance; 
            }
        }

        private SerializationMgr()
        {

        }

        private bool IsValue(object serialized)
        {
            return serialized.GetType().IsPrimitive || serialized is string;
        }

        private IDictionary<object, object> Dump(IDumper dumper, bool includeType = true)
        {
            var dumped = dumper.Dump();

            List<object> keys = new List<object>(dumped.Keys);

            foreach (object key in keys)
            {
                if (dumped[key] is IDumper)
                {
                    dumped[key] = Dump(dumped[key] as IDumper, includeType);
                } else if (IsValue(dumped[key]))
                {
                    dumped[key] = dumped[key];
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

        public string Serialize(IDumper serialized, Formatter formatter, bool includeType = true)
        {
            return formatter.Write(Dump(serialized, includeType));
        }

        private object CreateFromDump(IDictionary<object, object> dump)
        {
            Dictionary<object, object> data = new Dictionary<object, object>();
            foreach (KeyValuePair<object, object> entry in dump)
            {
                if (entry.Key.Equals(TYPE_KEY))
                {
                    continue;
                }

                if (entry.Value is IDictionary<object, object>)
                {
                    data[entry.Key] = CreateFromDump(entry.Value as IDictionary<object, object>);
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

            var deserializedType = Type.GetType(typeFullName);

            if (deserializedType == null)
            {
                throw new KOSSerializationException("Unrecognized type: " + typeFullName);
            }

            IDumper instance = Activator.CreateInstance(deserializedType) as IDumper;

            instance.LoadDump(data);

            return instance;
        }

        public object Deserialize(string input, Formatter formatter)
        {
            object serialized = formatter.Read(input);

            if (serialized is IDictionary<object, object>)
            {
                return CreateFromDump(serialized as IDictionary<object, object>);
            }

            return serialized;
        }
    }
}

