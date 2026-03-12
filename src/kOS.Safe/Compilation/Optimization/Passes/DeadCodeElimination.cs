using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Compilation.IR;

namespace kOS.Safe.Compilation.Optimization.Passes
{
    internal class DeadCodeElimination : IHolisticOptimizationPass
    {
        public OptimizationLevel OptimizationLevel => OptimizationLevel.Minimal;
        public short SortIndex => 50;

        public void ApplyPass(IRCodePart code)
        {
            HashSet<BasicBlock> rootBlocks = new HashSet<BasicBlock>(code.RootBlocks);

            RemoveDeadBlocks(code.Blocks, rootBlocks);

            foreach (IRCodePart.IRFunction function in code.Functions)
            {
                RemoveDeadBlocks(function.InitializationCode, rootBlocks);
                foreach (IRCodePart.IRFunction.IRFunctionFragment fragment in function.Fragments)
                {
                    RemoveDeadBlocks(fragment.FunctionCode, rootBlocks);
                }
            }
        }

        private static void RemoveDeadBlocks(List<BasicBlock> blocks, HashSet<BasicBlock> rootBlocks)
        {
            List<BasicBlock> blocksToRemove = new List<BasicBlock>();
            foreach (BasicBlock block in blocks)
            {
                if (block.Predecessors.Any() || rootBlocks.Contains(block))
                    continue;
                blocksToRemove.Add(block);
            }
            foreach (BasicBlock block in blocksToRemove)
            {
                blocks.Remove(block);
                foreach (BasicBlock successor in block.Successors.ToArray())
                    block.RemoveSuccessor(successor);
            }
        }
    }
}
