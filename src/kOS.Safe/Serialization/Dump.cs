using System;
using System.Collections.Generic;

namespace kOS.Safe
{
    public class Dump : Dictionary<object, object>
    {
        public const string Items = "items";
        public const string Entries = "entries";

        public Dump()
        {
        }

        public Dump(IDictionary<object, object> dictionary) : base(dictionary)
        {
        }

    }

}

