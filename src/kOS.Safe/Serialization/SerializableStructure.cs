using System;
using kOS.Safe.Encapsulation;

namespace kOS.Safe.Serialization
{
    public abstract class SerializableStructure : Structure
    {
        public abstract Dump Dump();
        public abstract void LoadDump(Dump dump);
    }
}

