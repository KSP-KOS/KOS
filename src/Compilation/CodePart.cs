using System.Collections.Generic;

namespace kOS.Compilation
{
    public class CodePart
    {
        public CodePart(string fromFile = "")
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

        public void AssignSourceName(string sourceName)
        {
            AssignSourceNameToSection(sourceName, FunctionsCode);
            AssignSourceNameToSection(sourceName, InitializationCode);
            AssignSourceNameToSection(sourceName, MainCode);
        }

        private void AssignSourceNameToSection(string sourceName, IEnumerable<Opcode> section)
        {
            foreach (Opcode opcode in section)
            {
                opcode.SourceName = string.Intern(sourceName); // Intern ensures we don't waste space storing the filename again and again per opcode.
            }
        }

    }
}
