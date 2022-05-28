using kOS.Safe.Serialization;
using System;

namespace kOS.Safe.Encapsulation
{
    [kOS.Safe.Utilities.KOSNomenclature("Structure", KOSToCSharp = false)]
    public abstract class PrimitiveStructure : SerializableStructure
    {
        public abstract object ToPrimitive();
    }
}

