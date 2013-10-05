using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS
{
    public class SpecialValue
    {
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
