using System.Collections.Generic;

namespace kOS.Safe.Compilation.KS
{
    public class LockFunction
    {
        public List<Opcode> Code { get; private set; }

        public LockFunction()
        {
            Code = new List<Opcode>();
        }
    }
}