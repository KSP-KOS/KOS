using System.Collections.Generic;
using System.Linq;

namespace kOS.Safe.Compilation.IR
{
    public static class IREmitter
    {
        public static List<Opcode> Emit(List<BasicBlock> blocks)
        {
            List<Opcode> result = new List<Opcode>();
            Dictionary<string, int> jumpLabels = new Dictionary<string, int>();
            // Emit Opcodes for each block
            foreach (BasicBlock block in blocks)
                LabelAndEmit(block, jumpLabels, result);
            // Remove single-line jumps from fallthrough blocks
            for (int i = 0; i < result.Count - 1; i++)
            {
                Opcode opcode = result[i];
                if (opcode is OpcodeBranchJump jump &&
                    jump.DestinationLabel != null && jump.DestinationLabel == result[i + 1].Label)
                {
                    foreach (var key in jumpLabels.Keys.ToArray())
                        if (jumpLabels[key] >= i)
                            jumpLabels[key] = jumpLabels[key] - 1;
                    result.RemoveAt(i);
                    i--;
                }
            }
            // Restore original labels
            foreach (Opcode opcode in result)
            {
                if (opcode.Label != null && jumpLabels.ContainsKey(opcode.Label))
                    opcode.Label = CreateLabel(jumpLabels[opcode.Label]);
                if (opcode.DestinationLabel != null && jumpLabels.ContainsKey(opcode.DestinationLabel))
                    opcode.DestinationLabel = CreateLabel(jumpLabels[opcode.DestinationLabel]);
            }
            return result;
        }

        private static void LabelAndEmit(BasicBlock block, Dictionary<string, int> jumpLabels, List<Opcode> result)
        {
            jumpLabels.Add(block.Label, result.Count);
            result.AddRange(block.EmitOpCodes());
        }

        private static string CreateLabel(int index)
            => string.Format("@{0:0000}", index);   // TODO: + 1
    }
}
