using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Compilation.IR.Optimization;

namespace kOS.Safe.Compilation.IR
{
    public class IROptimizer
    {
        public OptimizationLevel OptimizationLevel { get; }

        public IROptimizer(OptimizationLevel optimizationLevel)
        {
            OptimizationLevel = optimizationLevel;
        }

        public List<BasicBlock> Optimize(List<BasicBlock> blocks)
        {
            if (blocks.Count == 0)
                return blocks;

            ExtendedBasicBlock extendedRootBlock = ExtendedBasicBlock.CreateExtendedBlockTree(blocks[0]);
            HashSet<ExtendedBasicBlock> extendedBlocks = new HashSet<ExtendedBasicBlock>(ExtendedBasicBlock.DumpTree(extendedRootBlock));
            return blocks;
        }
    }
}
