using System;
using System.Collections.Generic;

namespace kOS.Safe
{
    public class Dump : Dictionary<object, object>
    {
        public Dump()
        {
        }

        public Dump(IDictionary<object, object> dictionary) : base(dictionary)
        {
        }

    }

}

