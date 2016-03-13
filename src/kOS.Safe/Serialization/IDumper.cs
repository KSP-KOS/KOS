using System;

namespace kOS.Safe.Serialization
{
    public interface IDumper
    {
        Dump Dump();
        void LoadDump(Dump dump);
    }
}

