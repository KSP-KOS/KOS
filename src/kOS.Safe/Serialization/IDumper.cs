using System;

namespace kOS.Safe.Serialization
{
    /// <summary>
    /// Classes implementing this interface can dump their data to a dictionary.
    ///
    /// Dumps should only contain primitives, strings, lists and other Dumps.
    /// SerializationMgr, for convenience, will handle any encapsulation types that implement
    /// PrimitiveStructure when serializing.
    ///
    /// Types implementing IDumper should make sure that proper encapsulation types are created in LoadDump whenever
    /// necessary.
    /// </summary>
    public interface IDumper
    {
        Dump Dump();
        void LoadDump(Dump dump);
    }
}
