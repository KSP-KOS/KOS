using System.Collections.Generic;
using System.Linq;

namespace kOS.Safe.Compilation.IR
{
    /// <summary>
    /// This class is used to emit code back into Opcode representation
    /// from the Three-Address Code interim representation.
    /// </summary>
    public class IREmitter
    {
        int labelIndex = 0;
        /// <summary>
        /// Emits the specified blocks to Opcode representation.
        /// </summary>
        /// <param name="blocks">The interim representation basic blocks for which to emit.</param>
        /// <returns>The sequence of Opcodes representing the code.</returns>
        public List<Opcode> Emit(List<BasicBlock> blocks)
        {
            List<Opcode> result = new List<Opcode>();
            Dictionary<string, int> jumpLabels = new Dictionary<string, int>();
            // Emit Opcodes for each block
            foreach (BasicBlock block in blocks)
                LabelAndEmit(block, jumpLabels, result);
            bool changed = true;
            int iteration = 0;
            while (changed)
            {
                changed = false;
                if (iteration++ > 1000)
                    break;
                // Remove single-line jumps from fallthrough blocks
                for (int i = 0; i < result.Count; i++)
                {
                    Opcode opcode = result[i];
                    if (string.IsNullOrEmpty(opcode.Label) || opcode.Label.StartsWith("@"))
                        opcode.Label = CreateLabel(i + labelIndex);
                    if (i >= result.Count - 1)
                        continue;
                    if (opcode is OpcodeBranchJump jump &&
                        jump.DestinationLabel != null && jumpLabels[jump.DestinationLabel] == i + 1 + labelIndex)
                    {
                        foreach (var key in jumpLabels.Keys.ToArray())
                            if (jumpLabels[key] >= i + 1 + labelIndex)
                                jumpLabels[key] = jumpLabels[key] - 1;
                        result.RemoveAt(i);
                        i--;
                    }
                    else if (opcode.DestinationLabel != null &&
                        jumpLabels.TryGetValue(opcode.DestinationLabel, out int destination) &&
                        result[destination - labelIndex] is OpcodeBranchJump)
                    {
                        opcode.DestinationLabel = result[destination].DestinationLabel;
                        changed = true;
                    }
                }
            }
            // Restore original labels
            foreach (Opcode opcode in result)
            {
                if (opcode.DestinationLabel != null &&
                    opcode.DestinationLabel.StartsWith("@") &&
                    jumpLabels.ContainsKey(opcode.DestinationLabel))
                {
                    opcode.DestinationLabel = CreateLabel(jumpLabels[opcode.DestinationLabel]);
                }
            }
            labelIndex += result.Count;
            return result;
        }

        private void LabelAndEmit(BasicBlock block, Dictionary<string, int> jumpLabels, List<Opcode> result)
        {
            jumpLabels.Add(block.Label, result.Count + labelIndex);
            result.AddRange(block.EmitOpCodes());
        }

        private static string CreateLabel(int index)
            => string.Format("@{0:0000}", index + 1);
    }
}
