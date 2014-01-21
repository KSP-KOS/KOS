using System;

namespace kOS.Command
{
    public class CommandAttribute : Attribute
    {
        public string[] Values { get; set; }
        public CommandAttribute(params string[] values) { Values = values; }

        public override string ToString()
        {
            return string.Join(",", Values);
        }
    }
}