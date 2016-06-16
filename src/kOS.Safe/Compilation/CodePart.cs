using System.Collections.Generic;
using System;
using kOS.Safe.Persistence;

namespace kOS.Safe.Compilation
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

        public void AssignSourceName(GlobalPath filePath)
        {
            AssignSourcePathToSection(filePath, FunctionsCode);
            AssignSourcePathToSection(filePath, InitializationCode);
            AssignSourcePathToSection(filePath, MainCode);
        }

        private void AssignSourcePathToSection(GlobalPath filePath, IEnumerable<Opcode> section)
        {
            foreach (Opcode opcode in section)
            {
                opcode.SourcePath = filePath;
            }
        }

    }
}
