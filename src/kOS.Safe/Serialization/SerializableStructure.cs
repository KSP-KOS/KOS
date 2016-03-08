using System;
using kOS.Safe.Encapsulation;

namespace kOS.Safe.Serialization
{
    [kOS.Safe.Utilities.KOSNomenclature("Structure", KOSToCSharp = false)] // reports itself as "Structure" but won't be the canonical meaning of "Structure"
    public abstract class SerializableStructure : Structure
    {
        public abstract Dump Dump();
        public abstract void LoadDump(Dump dump);
    }
}

