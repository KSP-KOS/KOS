using System.Collections.Generic;

namespace kOS.Safe.Serialization
{
    public class DumpWithHeader : Dump
    {
        public string Header { get; set; }

        public DumpWithHeader()
        {
        }

        public DumpWithHeader(IDictionary<object, object> dictionary) : base(dictionary)
        {
        }
    }
}

