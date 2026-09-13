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
        public static IReadOnlyCollection<Type> WhiteListedPasses = new HashSet<Type>
        {
            typeof(Passes.SCCPWithTypePropagation),
            typeof(SingleStaticAssignment),
            typeof(Passes.CallSuffixElimination),
            typeof(Passes.TernaryOperandConstruction)
        };
        public static HashSet<Type> PassesToSkip { get; } = new HashSet<Type>();

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
                if (type != typeof(Passes.SCCPWithTypePropagation))
                {
                    if (PassesToSkip.Contains(type))
                        continue;
                }

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
        public void Optimize(IRCodePart codePart)
        {
            Code = codePart;

            foreach (IOptimizationPass pass in optimizationPasses)
            {
                if (pass.OptimizationLevel > OptimizationLevel)
                    continue;

                if (PassesToSkip.Contains(pass.GetType()) &&
                    !WhiteListedPasses.Contains(pass.GetType()))
                    continue;

                SafeHouse.Logger.Log($"Applying optimization pass: {pass.GetType()}.");
                switch (pass)
                {
                    case IHolisticOptimizationPass codePartpass:
                        codePartpass.ApplyPass(Code);
                        break;
                    case IOptimizationPass<BasicBlock> blockPass:
                        blockPass.ApplyPass(Code.Blocks);
                        break;
                    case IOptimizationPass<IRInstruction> instructionPass:
                        foreach (BasicBlock block in Code.Blocks)
                            instructionPass.ApplyPass(block.Instructions);
                        break;
                    case IOptimizationPass<CodeElement> codeUnitPass:
                        codeUnitPass.ApplyPass(codePart.Elements);
                        break;
                    case IOptimizationPass<CodeComponent> codeComponentPass:
                        codeComponentPass.ApplyPass(codePart.Components);
                        break;
                    case IOptimizationPass<IRTrigger> triggerPass:
                        triggerPass.ApplyPass(codePart.Triggers);
                        break;
                    case IOptimizationPass<IRFunction> functionPass:
                        functionPass.ApplyPass(codePart.Functions);
                        break;
                    case IOptimizationPass<IInterimFunction> allFunctionPass:
                        allFunctionPass.ApplyPass(codePart.Functions.Concat(codePart.Components.Where(e => e is IRAnonymousFunction).Cast<IInterimFunction>()));
                        break;
                    default:
                        SafeHouse.Logger.LogWarning($"{pass.GetType()}, implementing IOptimizingPass<T>, uses an unsupported generic parameter.");
                        break;
                }
            }
        }
    }
}
