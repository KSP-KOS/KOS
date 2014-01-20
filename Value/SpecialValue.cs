using System;

namespace kOS.Value
{
    public abstract class SpecialValue
    {
        public virtual bool SetSuffix(String suffixName, object value)
        {
            return false;
        }

        public virtual object GetIndex(int index)
        {
            return null;
        }

        public virtual object GetSuffix(String suffixName)
        {
            return null;
        }

        public virtual object TryOperation(string op, object other, bool reverseOrder)
        {
            return null;
        }
    }
}
