using System;

namespace kOS.Safe.Function
{
    public class FunctionAttribute : Attribute
    {
        public string[] Names { get; set; }

        public FunctionAttribute(params string[] names)
        {
            Names = names;
        }
    }
}
