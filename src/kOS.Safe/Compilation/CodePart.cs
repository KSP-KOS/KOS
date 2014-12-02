using System.Collections.Generic;
using System;

namespace kOS.Safe.Compilation
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
            AssignSourceNameToSection(sourceName.ToLower(), FunctionsCode);
            AssignSourceNameToSection(sourceName.ToLower(), InitializationCode);
            AssignSourceNameToSection(sourceName.ToLower(), MainCode);
        }

        private void AssignSourceNameToSection(string sourceName, IEnumerable<Opcode> section)
        {
            foreach (Opcode opcode in section)
            {
                opcode.SourceName = string.Intern(sourceName.ToLower()); // Intern ensures we don't waste space storing the filename again and again per opcode.
            }
        }

    }
}
