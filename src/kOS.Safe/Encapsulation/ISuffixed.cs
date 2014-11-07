using System.Collections.Generic;

namespace kOS.Safe.Encapsulation
{
    public interface ISuffixed
    {
        bool SetSuffix(string suffixName, object value);
        object GetSuffix(string suffixName);
    }
}