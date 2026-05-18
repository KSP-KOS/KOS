using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Compilation.IR;
using kOS.Safe.Function;
using kOS.Safe.Utilities;

namespace kOS.Safe.Compilation.Optimization
{
    /// <summary>
    /// This class performs the actual optimization by applying all
    /// optimization passes, as identified by implementation of <see cref="IOptimizationPass"/>.
    /// </summary>
    [AssemblyWalk(InterfaceType = typeof(IOptimizationPass), StaticRegisterMethod = "RegisterMethod")]
    public class Optimizer
    {
#if DEBUG
        public static HashSet<Type> PassesToSkip { get; } = new HashSet<Type>();
        public static HashSet<Type> OnlyThesePasses { get; set; } = null;
#endif
        internal static InterimCPU InterimCPU { get; } = new InterimCPU();
        private static readonly SafeSharedObjects shared = new SafeSharedObjects() { Cpu = InterimCPU };
        private readonly SortedSet<IOptimizationPass> optimizationPasses = new SortedSet<IOptimizationPass>(
            Comparer<IOptimizationPass>.Create((a, b) => a.SortIndex.CompareTo(b.SortIndex)));
        private readonly static HashSet<Type> availablePassTypes = new HashSet<Type>();

        /// <summary>
        /// Gets the optimization level to be applied.
        /// </summary>
        public OptimizationLevel OptimizationLevel { get; }
        /// <summary>
        /// Gets a value indicating whether built-in names may be clobbered.
        /// </summary>
        public bool AllowClobberBuiltins { get; }
        /// <summary>
        /// Gets the collection of Basic Blocks being operated upon.
        /// </summary>
        public List<BasicBlock> Blocks { get; private set; }
        /// <summary>
        /// Gets the collection of extended basic blocks being operated upon.
        /// </summary>
        public List<ExtendedBasicBlock> ExtendedBlocks { get; private set; }
        /// <summary>
        /// Gets the collection of root blocks.
        /// </summary>
        public HashSet<BasicBlock> RootBlocks { get; private set; }
        /// <summary>
        /// Gets the IRCodePart object being operated upon.
        /// </summary>
        public IRCodePart Code { get; private set; }
        /// <summary>
        /// Gets the function manager.
        /// </summary>
        public static IFunctionManager FunctionManager => shared.FunctionManager;

        static Optimizer()
        {
            shared.FunctionManager = new FunctionManager(shared);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Optimizer"/> class.
        /// </summary>
        /// <param name="optimizationLevel">The optimization level to be applied.</param>
        public Optimizer(CompilerOptions options)
        {
            OptimizationLevel = options.OptimizationLevel;
            AllowClobberBuiltins = options.AllowClobberBuiltins;

            foreach (Type type in availablePassTypes)
            {
#if DEBUG
                if (type != typeof(Passes.SCCPWithTypePropagation))
                {
                    if (OnlyThesePasses != null)
                    {
                        if (!OnlyThesePasses.Contains(type))
                            continue;
                    }
                    else if (PassesToSkip.Contains(type))
                        continue;
                }
#endif
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

        /// <summary>
        /// Applies the optimization passes to the specified code part.
        /// </summary>
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
