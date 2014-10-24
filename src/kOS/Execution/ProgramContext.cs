using System;
using System.Collections.Generic;
using kOS.Binding;
using kOS.Safe.Compilation;

namespace kOS.Execution
{
    public class ProgramContext
    {
        private readonly Dictionary<string, bool> flyByWire;
        private readonly ProgramBuilder builder;

        public List<Opcode> Program { get; set; }
        public int InstructionPointer { get; set; }
        public List<int> Triggers { get; set; }
        public bool Silent { get; set; } 
        
        public ProgramContext(bool interpreterContext)
        {
            Program = new List<Opcode>();
            InstructionPointer = 0;
            Triggers = new List<int>();
            builder = interpreterContext ? new ProgramBuilderInterpreter() : new ProgramBuilder();
            flyByWire = new Dictionary<string, bool>();
        }

        public ProgramContext(bool interpreterContext, List<Opcode> program)
            : this(interpreterContext)
        {
            Program = program;
        }

        public void AddParts(IEnumerable<CodePart> parts)
        {
            builder.AddRange(parts);
            List<Opcode> newProgram = builder.BuildProgram();
            UpdateProgram(newProgram);
        }

        public int AddObjectParts(IEnumerable<CodePart> parts)
        {
            Guid objectFileId = builder.AddObjectFile(parts);
            List<Opcode> newProgram = builder.BuildProgram();
            int entryPointAddress = builder.GetObjectFileEntryPointAddress(objectFileId);
            UpdateProgram(newProgram);
            return entryPointAddress;
        }
        
        private void UpdateProgram(List<Opcode> newProgram)
        {
            if (Program != null)
            {
                List<Opcode> oldProgram = Program;
                Program = newProgram;
                UpdateInstructionPointer(oldProgram);
            }
            else
            {
                Program = newProgram;
            }
        }

        private void UpdateInstructionPointer(List<Opcode> oldProgram)
        {
            if (oldProgram.Count > 1)
            {
                int delta = 0;

                if (InstructionPointer == (oldProgram.Count - 1))
                {
                    delta = 1;
                }

                int currentInstructionId = oldProgram[InstructionPointer - delta].Id;

                for (int index = 0; index < Program.Count; index++)
                {
                    if (Program[index].Id == currentInstructionId)
                    {
                        InstructionPointer = index + delta;
                        break;
                    }
                }
            }
        }

        public void ToggleFlyByWire(string paramName, bool enabled)
        {
            if (!flyByWire.ContainsKey(paramName))
            {
                flyByWire.Add(paramName, enabled);
            }
            else
            {
                flyByWire[paramName] = enabled;
            }
        }

        public void DisableActiveFlyByWire(BindingManager manager)
        {
            foreach (KeyValuePair<string, bool> kvp in flyByWire)
            {
                if (kvp.Value)
                {
                    manager.ToggleFlyByWire(kvp.Key, false);
                }
            }
        }

        public void EnableActiveFlyByWire(BindingManager manager)
        {
            foreach (KeyValuePair<string, bool> kvp in flyByWire)
            {
                manager.ToggleFlyByWire(kvp.Key, kvp.Value);
            }
        }
        
        public List<string> GetCodeFragment(int contextLines)
        {
            return GetCodeFragment( InstructionPointer - contextLines, InstructionPointer + contextLines );
        }
        
        public List<string> GetCodeFragment(int start, int stop)
        {
            var codeFragment = new List<string>();
            
            const string FORMAT_STR = "{0,-20} {1,4}:{2,-3} {3:0000} {4} {5}";
            codeFragment.Add(string.Format(FORMAT_STR, "File", "Line", "Col", "IP  ", "opcode", "operand" ));
            codeFragment.Add(string.Format(FORMAT_STR, "----", "----", "---", "----", "---------------------", "" ));

            for (int index = start; index <= stop; index++)
            {
                if (index >= 0 && index < Program.Count)
                {
                    codeFragment.Add(string.Format(FORMAT_STR,
                                                   Program[index].SourceName,
                                                   Program[index].SourceLine,
                                                   Program[index].SourceColumn,
                                                   index,
                                                   Program[index],
                                                   (index == InstructionPointer ? "<<--INSTRUCTION POINTER--" : "")));
                }
            }

            return codeFragment;
        }

    }
}
