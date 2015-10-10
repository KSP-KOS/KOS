using System.Collections.Generic;

namespace kOS.Safe.Execution
{
    public class UserDelegateValue : UserDelegate
    {
        public string Identifier { get; private set; }

        public UserDelegateValue(ICpu cpu, IProgramContext context, int entryPoint, bool useClosure, string identifier) : base(cpu, context, entryPoint, useClosure)
        {
            Identifier = identifier;
        }

        public override string ToString()
        {
            return "Delegate('" + Identifier + "')";
        }

    }
}
