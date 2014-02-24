using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Compilation;

namespace kOS.InterProcessor
{
    public class RunCommand : InterProcCommand
    {
        public List<Opcode> Program;

        public override void Execute(SharedObjects shared)
        {
            if (shared.Cpu != null)
            {
                shared.Cpu.RunProgram(this.Program);
            }
        }
    }
}
