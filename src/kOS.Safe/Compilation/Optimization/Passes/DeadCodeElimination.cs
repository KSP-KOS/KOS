using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Compilation.IR;

namespace kOS.Safe.Compilation.Optimization.Passes
{
    internal class DeadCodeElimination : ILinkedOptimizationPass
    {
        public Optimizer Optimizer { set => optimizer = value; }
        public OptimizationLevel OptimizationLevel => OptimizationLevel.Minimal;
        public short SortIndex => 50;

        private Optimizer optimizer;

        public void ApplyPass()
        {
            List<BasicBlock> blocksToRemove = new List<BasicBlock>();
            foreach (BasicBlock block in optimizer.Blocks)
            {
                if (block.Predecessors.Any() || optimizer.RootBlocks.Contains(block))
                    continue;
                blocksToRemove.Add(block);
            }
            foreach (BasicBlock block in blocksToRemove)
            {
                optimizer.Blocks.Remove(block);
                foreach (BasicBlock successor in block.Successors.ToArray())
                    block.RemoveSuccessor(successor);
            }
        }
    }
}
