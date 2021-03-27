using System;
using System.Collections.Generic;

namespace kOS.Safe
{
    public class Dump : Dictionary<object, object>
    {
        public const string Items = "items";
        public const string Entries = "entries";

        public DumpKeyType KeyType { get
            {
                if (ContainsKey(Items))
                    return DumpKeyType.List;
                if (ContainsKey(Entries))
                    return DumpKeyType.KeyValue;
                if (ContainsKey("value"))
                    return DumpKeyType.Value;
                return DumpKeyType.Default;
            }
        }
        public Dictionary<object, string> Annotations { get; private set; }

        public Dump()
        {
            Annotations = new Dictionary<object, string>();
        }

        public Dump(IDictionary<object, object> dictionary) : base(dictionary)
        {
            Annotations = new Dictionary<object, string>();
        }

    }

    public enum DumpKeyType
    {
        Default = 0,
        KeyValue,
        List,
        Value,
    }

}

