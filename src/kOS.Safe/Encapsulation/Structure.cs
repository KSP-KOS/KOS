using System;
using System.Collections.Generic;

namespace kOS.Safe.Encapsulation
{
    public abstract class Structure : ISuffixed, IOperable 
    {
        private readonly IDictionary<string, ISuffix> suffixes;

        protected Structure()
        {
            suffixes = new Dictionary<string, ISuffix>();
        }

        protected void AddSuffix(string suffixName, ISuffix suffixToAdd)
        {
            if (suffixes.ContainsKey(suffixName))
            {
                suffixes[suffixName] = suffixToAdd;
            }
            else
            {
                suffixes.Add(suffixName, suffixToAdd);
            }
        }

        public virtual bool SetSuffix(string suffixName, object value)
        {
            ISuffix suffix;
            if (suffixes.TryGetValue(suffixName, out suffix))
            {
                var settable = suffix as ISetSuffix;
                if (settable == null)
                {
                    return false;
                }
                return settable.Set(value);
            }
            return false;
        }

        public virtual object GetSuffix(string suffixName)
        {
            ISuffix suffix;
            return !suffixes.TryGetValue(suffixName, out suffix) ? null : suffix.Get();
        }

        public virtual object TryOperation(string op, object other, bool reverseOrder)
        {
            return null;
        }

        protected object ConvertToDoubleIfNeeded(object value)
        {
            if (!(value is Structure) && !(value is double))
            {
                value = Convert.ToDouble(value);
            }

            return value;
        }

        public override string ToString()
        {
            return "Structure ";
        }
    }
}
