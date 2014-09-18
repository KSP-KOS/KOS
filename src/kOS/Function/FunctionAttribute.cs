using System;

namespace kOS.Function
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
