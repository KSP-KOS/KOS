using System.Collections.Generic;

namespace kOS.Safe.Encapsulation
{
    /// <summary>
    /// Classes implementing this interface can dump their data to a dictionary.
    /// </summary>
    public interface IDumper : ISuffixed
    {
        IDictionary<object, object> Dump();
        void LoadDump(IDictionary<object, object> dump);
    }
}