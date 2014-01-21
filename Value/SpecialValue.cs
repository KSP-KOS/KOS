using System;

namespace kOS.Value
{
    public abstract class SpecialValue : ISuffixed
    {
        public virtual bool SetSuffix(String suffixName, object value)
        {
            return false;
        }

        public virtual object GetSuffix(String suffixName)
        {
            return null;
        }
    }
}
