using System;

namespace kOS.Value
{
    public interface ISuffixed
    {
        bool SetSuffix(String suffixName, object value);
        object GetSuffix(String suffixName);
    }
}