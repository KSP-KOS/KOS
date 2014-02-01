using System;

namespace kOS.Command
{
    public class CommandAttribute : Attribute
    {
        public CommandAttribute(params string[] values)
        {
            Values = values;
        }

        public string[] Values { get; set; }

        public override string ToString()
        {
            return string.Join(",", Values);
        }
    }
}