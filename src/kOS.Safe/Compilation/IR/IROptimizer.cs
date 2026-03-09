using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Compilation.IR.Optimization;
using kOS.Safe.Function;
using kOS.Safe.Utilities;

namespace kOS.Safe.Compilation.IR
{
    [AssemblyWalk(InterfaceType = typeof(IOptimizationPass), StaticRegisterMethod = "RegisterMethod")]
    public class IROptimizer
    {
        public OptimizationLevel OptimizationLevel { get; }
        public List<BasicBlock> Blocks { get; private set; }
        public List<ExtendedBasicBlock> ExtendedBlocks { get; private set; }
        public HashSet<BasicBlock> RootBlocks { get; private set; }
        public IRCodePart Code { get; private set; }
        internal static InterimCPU InterimCPU { get; } = new InterimCPU();
        public static IFunctionManager FunctionManager => shared.FunctionManager;

        private static readonly SafeSharedObjects shared = new SafeSharedObjects() { Cpu = InterimCPU };
        private readonly SortedSet<IOptimizationPass> optimizationPasses = new SortedSet<IOptimizationPass>(
            Comparer<IOptimizationPass>.Create((a, b) => a.SortIndex.CompareTo(b.SortIndex)));
        private readonly static HashSet<Type> availablePassTypes = new HashSet<Type>();

        static IROptimizer()
        {
            shared.FunctionManager = new FunctionManager(shared);
        }

        public IROptimizer(OptimizationLevel optimizationLevel)
        {
            OptimizationLevel = optimizationLevel;
            foreach (Type type in availablePassTypes)
            {
                IOptimizationPass pass = (IOptimizationPass)Activator.CreateInstance(type);
                optimizationPasses.Add(pass);
                if (pass is ILinkedOptimizationPass linkedPass)
                    linkedPass.Optimizer = this;
            }
        }
        public static void RegisterMethod(Type type)
        {
            availablePassTypes.Add(type);
        }

        public List<BasicBlock> Optimize(IRCodePart codePart)
        {
            Code = codePart;
            Blocks = codePart.Blocks;
            RootBlocks = new HashSet<BasicBlock>(codePart.RootBlocks);

            List<ExtendedBasicBlock> rootExtendedBlocks = new List<ExtendedBasicBlock>(
                codePart.RootBlocks.Select(b => ExtendedBasicBlock.CreateExtendedBlockTree(b)));
            ExtendedBlocks = new List<ExtendedBasicBlock>(
                rootExtendedBlocks.SelectMany(ExtendedBasicBlock.DumpTree));

            foreach (IOptimizationPass pass in optimizationPasses)
            {
                if (pass.OptimizationLevel > OptimizationLevel)
                    continue;

                SafeHouse.Logger.Log($"Applying optimization pass: {pass.GetType()}.");
                switch (pass)
                {
                    case ILinkedOptimizationPass linkedPass:
                        linkedPass.ApplyPass();
                        break;
                    case IHolisticOptimizationPass codePartpass:
                        codePartpass.ApplyPass(Code);
                        break;
                    case IOptimizationPass<BasicBlock> blockPass:
                        blockPass.ApplyPass(Blocks);
                        break;
                    case IOptimizationPass<ExtendedBasicBlock> extendedBlockPass:
                        extendedBlockPass.ApplyPass(ExtendedBlocks);
                        break;
                    case IOptimizationPass<IRInstruction> instructionPass:
                        foreach (BasicBlock block in Blocks)
                            instructionPass.ApplyPass(block.Instructions);
                        break;
                    default:
                        SafeHouse.Logger.LogWarning($"{pass.GetType()}, implementing IOptimizingPass<T>, uses an unsupported generic parameter.");
                        break;
                }
            }
            return Blocks;
        }
    }
}
