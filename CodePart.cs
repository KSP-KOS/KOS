using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS
{
    public class CodePart
    {
        public List<Opcode> FunctionsCode = new List<Opcode>();
        public List<Opcode> InitializationCode = new List<Opcode>();
        public List<Opcode> MainCode = new List<Opcode>();

        public List<Opcode> MergeSections()
        {
            List<Opcode> mergedCode = new List<Opcode>();
            mergedCode.AddRange(FunctionsCode);
            mergedCode.AddRange(InitializationCode);
            mergedCode.AddRange(MainCode);
            return mergedCode;
        }

        public void AssignInstructionId(int instructionId)
        {
            AssignInstructionIdToSection(instructionId, FunctionsCode);
            AssignInstructionIdToSection(instructionId, InitializationCode);
            AssignInstructionIdToSection(instructionId, MainCode);
        }

        private void AssignInstructionIdToSection(int instructionId, List<Opcode> section)
        {
            foreach (Opcode opcode in section)
            {
                opcode.InstructionId = instructionId;
            }
        }
    }
}
