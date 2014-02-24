using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS.Execution
{
    public class Variable
    {
        public string Name;
        public virtual object Value { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
