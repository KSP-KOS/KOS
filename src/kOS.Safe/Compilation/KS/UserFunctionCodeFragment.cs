using System.Collections.Generic;

namespace kOS.Safe.Compilation.KS
{
    public class UserFunctionCodeFragment
    {
        public List<Opcode> Code { get; private set; }

        public UserFunctionCodeFragment()
        {
            Code = new List<Opcode>();
        }
    }
}