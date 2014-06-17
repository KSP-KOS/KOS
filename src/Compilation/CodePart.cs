using System.Collections.Generic;

namespace kOS.Compilation
{
    public class CodePart
    {
        public CodePart()
        {
            FunctionsCode = new List<Opcode>(); 
            InitializationCode = new List<Opcode>();
            MainCode = new List<Opcode>();
        }

        public List<Opcode> FunctionsCode { get; set; }
        public List<Opcode> InitializationCode { get; set; }
        public List<Opcode> MainCode { get; set; }

        public List<Opcode> MergeSections()
        {
            var mergedCode = new List<Opcode>();
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

        private void AssignInstructionIdToSection(int instructionId, IEnumerable<Opcode> section)
        {
            foreach (Opcode opcode in section)
            {
                opcode.InstructionId = instructionId;
            }
        }
    }
}
