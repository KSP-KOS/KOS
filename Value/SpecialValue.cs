using System;

namespace kOS.Value
{
    public abstract class SpecialValue : ISuffixed
    {
        public virtual bool SetSuffix(string suffixName, object value)
        {
            return false;
        }

        public virtual object GetSuffix(string suffixName)
        {
            return null;
        }
    }
}
