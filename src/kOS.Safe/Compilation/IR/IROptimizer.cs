using System;
using System.Collections.Generic;
using kOS.Safe.Compilation.IR.Optimization;
using kOS.Safe.Function;
using kOS.Safe.Utilities;

namespace kOS.Safe.Compilation.IR
{
    [AssemblyWalk(InterfaceType = typeof(IOptimizationPass), StaticRegisterMethod = "RegisterMethod")]
    public class IROptimizer
    {
        internal static InterimCPU InterimCPU { get; } = new InterimCPU();
        private static readonly SafeSharedObjects shared = new SafeSharedObjects() { Cpu = InterimCPU };
        public static IFunctionManager FunctionManager => shared.FunctionManager;

        private static readonly SortedSet<IOptimizationPass> optimizationPasses = new SortedSet<IOptimizationPass>(
            Comparer<IOptimizationPass>.Create((a, b) => a.SortIndex.CompareTo(b.SortIndex)));
        public OptimizationLevel OptimizationLevel { get; }

        static IROptimizer()
        {
            shared.FunctionManager = new FunctionManager(shared);
        }
        public IROptimizer(OptimizationLevel optimizationLevel)
        {
            OptimizationLevel = optimizationLevel;
        }
        public static void RegisterMethod(Type type)
        {
            optimizationPasses.Add((IOptimizationPass)Activator.CreateInstance(type));
        }

        public List<BasicBlock> Optimize(IRCodePart codePart)
        {
            List<BasicBlock> blocks = new List<BasicBlock>();
            blocks.AddRange(codePart.InitializationCode);
            blocks.AddRange(codePart.FunctionsCode);
            blocks.AddRange(codePart.MainCode);

            ExtendedBasicBlock initializationRoot = codePart.InitializationCode.Count > 0 ? ExtendedBasicBlock.CreateExtendedBlockTree(codePart.InitializationCode[0]) : null;
            ExtendedBasicBlock functionsRoot = codePart.FunctionsCode.Count > 0 ? ExtendedBasicBlock.CreateExtendedBlockTree(codePart.FunctionsCode[0]) : null;
            ExtendedBasicBlock mainRoot = codePart.MainCode.Count > 0 ? ExtendedBasicBlock.CreateExtendedBlockTree(codePart.MainCode[0]) : null;

            List<ExtendedBasicBlock> extendedBlocks = new List<ExtendedBasicBlock>();
            if (initializationRoot != null)
                extendedBlocks.AddRange(ExtendedBasicBlock.DumpTree(initializationRoot));
            if (functionsRoot != null)
                extendedBlocks.AddRange(ExtendedBasicBlock.DumpTree(functionsRoot));
            if (mainRoot != null)
                extendedBlocks.AddRange(ExtendedBasicBlock.DumpTree(mainRoot));

            foreach (IOptimizationPass pass in optimizationPasses)
            {
                if (pass.OptimizationLevel > OptimizationLevel)
                    continue;

                SafeHouse.Logger.Log($"Applying optimization pass: {pass.GetType()}.");
                try
                {
                    switch (pass)
                    {
                        case IHolisticOptimizationPass codePartpass:
                            codePartpass.ApplyPass(codePart);
                            break;
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
                    if (e is Exceptions.KOSCompileException)
                        throw;
                    throw new Exceptions.KOSCompileException(new KS.LineCol(0, 0), e.Message);
                }
            }
            return blocks;
        }
    }
}
