using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS.Function
{
    public class FunctionAttribute : Attribute
    {
        public string functionName { get; set; }
        public FunctionAttribute(string functionName)
        {
            this.functionName = functionName;
        }
    }
}
