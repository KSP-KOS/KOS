using System.Collections.Generic;

namespace kOS.Safe.Encapsulation
{
    /// <summary>
    /// Classes implementing this interface can dump their data to a dictionary.
    ///
    /// Dumps should not contain any encapsulation types, only Dictionaries, primitives and strings.
    /// SerializationMgr, for convenience, will handle any encapsulation types that implement
    /// ISerializableValue when serializing.
    ///
    /// Types implementing IDumper should make sure that proper encapsulation types are created in LoadDump whenever
    /// necessary.
    /// </summary>
    public interface IDumper : ISuffixed
    {
        IDictionary<object, object> Dump();
        void LoadDump(IDictionary<object, object> dump);
    }
}