using System;

namespace kOS.Safe.Function
{
    public class FunctionAttribute : Attribute
    {
        public string[] Names { get; set; }
        public string[] Contexts { get; set; }

        public FunctionAttribute(params string[] names)
        {
            Names = names;
            Contexts = new string[] { "ksp" };
        }
    }
}
