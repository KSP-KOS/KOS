using System;

namespace kOS.Value
{
    public interface ISuffixed
    {
        bool SetSuffix(string suffixName, object value);
        object GetSuffix(string suffixName);
    }
}