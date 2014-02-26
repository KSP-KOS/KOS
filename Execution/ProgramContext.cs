using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Binding;
using kOS.Compilation;

namespace kOS.Execution
{
    public class ProgramContext
    {
        private Dictionary<string, bool> _flyByWire;
        
        public List<Opcode> Program;
        public int InstructionPointer;
        public List<int> Triggers;
        public bool Silent;

        public ProgramContext()
        {
            Program = new List<Opcode>();
            InstructionPointer = 0;
            Triggers = new List<int>();
            _flyByWire = new Dictionary<string, bool>();
        }

        public ProgramContext(List<Opcode> program)
            : this()
        {
            this.Program = program;
        }


        public void UpdateProgram(List<Opcode> newProgram)
        {
            List<Opcode> oldProgram = Program;
            Program = newProgram;
            UpdateInstructionPointer(oldProgram);
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
            if (!_flyByWire.ContainsKey(paramName))
            {
                _flyByWire.Add(paramName, enabled);
            }
            else
            {
                _flyByWire[paramName] = enabled;
            }
        }

        public void DisableActiveFlyByWire(BindingManager manager)
        {
            foreach (KeyValuePair<string, bool> kvp in _flyByWire)
            {
                if (kvp.Value)
                {
                    manager.ToggleFlyByWire(kvp.Key, false);
                }
            }
        }

    }
}
