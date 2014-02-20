using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS
{
    public class ProgramBuilder
    {
        private List<CodePart> _parts = new List<CodePart>();
        private CodePart _lastBuild = null;

        public void AddRange(IEnumerable<CodePart> parts)
        {
            _parts.AddRange(parts);
        }

        public void Add(CodePart part)
        {
            _parts.Add(part);
        }

        public List<Opcode> BuildProgram(bool interpreted)
        {
            CodePart program = new CodePart();

            foreach (CodePart part in _parts)
            {
                if (interpreted)
                {
                    program.MainCode.AddRange(part.InitializationCode);
                }
                else
                {
                    program.InitializationCode.AddRange(part.InitializationCode);
                }

                program.FunctionsCode.AddRange(part.FunctionsCode);
                program.MainCode.AddRange(part.MainCode);
            }

            AddJumpToEntryPoint(program);
            AddEndOfProgram(program, interpreted);
            List<Opcode> mergedProgram = program.MergeSections();
            ReplaceLabels(mergedProgram);

            return mergedProgram;
        }

        private void AddJumpToEntryPoint(CodePart program)
        {
            if (program.MainCode.Count > 0)
            {
                OpcodeBranchJump jumpOpcode = new OpcodeBranchJump();
                jumpOpcode.DestinationLabel = program.MainCode[0].Label;
                program.FunctionsCode.Insert(0, jumpOpcode);
            }
        }

        private void AddEndOfProgram(CodePart program, bool interpreted)
        {
            if (interpreted)
            {
                program.MainCode.Add(new OpcodeEOF());
            }
            else
            {
                program.MainCode.Add(new OpcodeEOP());
            }
        }

        private void ReplaceLabels(List<Opcode> program)
        {
            Dictionary<string, int> labels = new Dictionary<string, int>();

            // get the index of every label
            for (int index = 0; index < program.Count; index++)
            {
                if (program[index].Label != string.Empty)
                {
                    labels.Add(program[index].Label, index);
                }
            }

            // replace destination labels with the corresponding index
            for (int index = 0; index < program.Count; index++)
            {
                Opcode opcode = program[index];
                if (opcode.DestinationLabel != string.Empty)
                {
                    if (opcode is BranchOpcode)
                    {
                        int destinationIndex = labels[opcode.DestinationLabel];
                        ((BranchOpcode)opcode).distance = destinationIndex - index;
                    }
                    else if (opcode is OpcodePush)
                    {
                        int destinationIndex = labels[opcode.DestinationLabel];
                        ((OpcodePush)opcode).argument = destinationIndex;
                    }
                }
            }
        }

    }
}
