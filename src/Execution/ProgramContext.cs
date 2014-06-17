using System;
using System.Collections.Generic;
using kOS.Binding;
using kOS.Compilation;

namespace kOS.Execution
{
    public class ProgramContext
    {
        private readonly Dictionary<string, bool> flyByWire;
        private readonly ProgramBuilder builder;

        public List<Opcode> Program;
        public int InstructionPointer;
        public List<int> Triggers;
        public bool Silent;

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
            var codeFragment = new List<string>();

            for (int index = (InstructionPointer - contextLines); index <= (InstructionPointer + contextLines); index++)
            {
                if (index >= 0 && index < Program.Count)
                {
                    codeFragment.Add(string.Format("{0:0000}    {1}    {2}", index, Program[index], (index == InstructionPointer ? "<<" : "")));
                }
            }

            return codeFragment;
        }

    }
}
