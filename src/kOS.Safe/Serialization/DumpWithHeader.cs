using System.Collections.Generic;

namespace kOS.Safe.Serialization
{
    public class DumpWithHeader : Dump
    {
        public string Header { get; set; }
        public bool ShouldHideValues { get; set; }

        public override DumpKeyType KeyType
        {
            get
            {
                if (ShouldHideValues)
                    return DumpKeyType.Hidden;
                return base.KeyType;
            }
        }
        public DumpWithHeader()
        {
            ShouldHideValues = false;
        }

        public DumpWithHeader(IDictionary<object, object> dictionary) : base(dictionary)
        {
            ShouldHideValues = false;
        }
    }
}

