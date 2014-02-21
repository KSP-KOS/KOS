using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS
{
    public class SpecialValue
    {
        public virtual bool SetSuffix(String suffixName, object value)
        {
            return false;
        }

        public virtual object GetSuffix(String suffixName)
        {
            return null;
        }

        public virtual object TryOperation(string op, object other, bool reverseOrder)
        {
            return null;
        }

        protected object ConvertToDoubleIfNeeded(object value)
        {
            if (!(value is SpecialValue) && !(value is double))
            {
                value = Convert.ToDouble(value);
            }

            return value;
        }
    }
}
