using System;
using System.Collections.Generic;

namespace kOS.Safe.Serialization
{
    public class DictionaryWithHeader : Dictionary<object, object>
    {
        public string Header { get; set; }

        public DictionaryWithHeader()
        {
        }

        public DictionaryWithHeader(IDictionary<object, object> dictionary) : base(dictionary)
        {
        }
    }
}

