using System;
using System.Collections.Generic;
using kOS.Safe.Compilation.IR.Optimization;
using kOS.Safe.Utilities;

namespace kOS.Safe.Compilation.IR
{
    [AssemblyWalk(InterfaceType = typeof(IOptimizationPass), StaticRegisterMethod = "RegisterMethod")]
    public class IROptimizer
    {
        private static readonly SortedSet<IOptimizationPass> optimizationPasses = new SortedSet<IOptimizationPass>(
            Comparer<IOptimizationPass>.Create((a, b) => a.SortIndex.CompareTo(b.SortIndex)));
        public OptimizationLevel OptimizationLevel { get; }

        public IROptimizer(OptimizationLevel optimizationLevel)
        {
            OptimizationLevel = optimizationLevel;
        }
        public static void RegisterMethod(Type type)
        {
            optimizationPasses.Add((IOptimizationPass)Activator.CreateInstance(type));
        }

        public List<BasicBlock> Optimize(List<BasicBlock> blocks)
        {
            if (blocks.Count == 0)
                return blocks;

            ExtendedBasicBlock extendedRootBlock = ExtendedBasicBlock.CreateExtendedBlockTree(blocks[0]);
            List<ExtendedBasicBlock> extendedBlocks = new List<ExtendedBasicBlock>(ExtendedBasicBlock.DumpTree(extendedRootBlock));

            foreach (IOptimizationPass pass in optimizationPasses)
            {
                if (pass.OptimizationLevel > OptimizationLevel)
                    continue;

                SafeHouse.Logger.Log($"Applying optimization pass: {pass.GetType()}.");
                try
                {
                    switch (pass)
                    {
                        case IOptimizationPass<BasicBlock> blockPass:
                            blockPass.ApplyPass(blocks);
                            break;
                        case IOptimizationPass<ExtendedBasicBlock> extendedBlockPass:
                            extendedBlockPass.ApplyPass(extendedBlocks);
                            break;
                        case IOptimizationPass<IRInstruction> instructionPass:
                            foreach (BasicBlock block in blocks)
                                instructionPass.ApplyPass(block.Instructions);
                            break;
                        default:
                            SafeHouse.Logger.LogWarning($"{pass.GetType()}, implementing IOptimizingPass<T>, uses an unsupported generic parameter.");
                            break;
                    }
                }
                catch (Exception e)
                {
                    throw new Exceptions.KOSCompileException(new KS.LineCol(0, 0), e.Message);
                }
            }
            return blocks;
        }
    }
}
