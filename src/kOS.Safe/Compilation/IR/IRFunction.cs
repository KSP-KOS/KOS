using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Compilation.KS;
using kOS.Safe.Compilation.Optimization;

namespace kOS.Safe.Compilation.IR
{
    /// <summary>
    /// This class represents a user-defined function.
    /// </summary>
    /// <seealso cref="IClosureVariableUser" />
    public class IRFunction : IInterimFunction
    {
        private readonly UserFunction function;
        private readonly List<UserFunctionCodeFragment> userFunctionFragments;
        private readonly Dictionary<UserFunctionCodeFragment, IRFunctionFragment> fragments = new Dictionary<UserFunctionCodeFragment, IRFunctionFragment>();

        /// <summary>
        /// Gets the code part to which this function belongs.
        /// </summary>
        public IRCodePart CodePart { get; }
        /// <summary>
        /// Gets the identifier string for this function.
        /// </summary>
        public string Identifier => function.Identifier;
        /// <summary>
        /// Gets or sets a value indicating whether this instance
        /// is stored at the global scope.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is global; otherwise, <c>false</c>.
        /// </value>
        public bool IsGlobal { get; internal set; } = false;
        /// <summary>
        /// Gets the initialization code component
        /// </summary>
        public CodeElement InitializationCode { get; set; }
        /// <summary>
        /// Gets the collection of function fragments.
        /// </summary>
        public IReadOnlyCollection<IRFunctionFragment> Fragments => fragments.Values;
        public HashSet<string> ExternalReads { get; set; } = new HashSet<string>();
        public HashSet<string> ExternalWrites { get; } = new HashSet<string>();
        public HashSet<(string Name, IRUnset Instruction)> ExternalUnsets { get; } = new HashSet<(string, IRUnset)>();
        public HashSet<IRTrigger> TriggersCreated { get; } = new HashSet<IRTrigger>();
        public HashSet<IInterimFunction> FunctionCalls { get; } = new HashSet<IInterimFunction>();
        public HashSet<IRInstruction> UnresolvedCallSites { get; } = new HashSet<IRInstruction>(IRInstruction.ReferenceEqualityComparer);
        public IRScope ClosureScope { get; }
        /// <summary>
        /// Gets a value indicating whether this instance may be recursive.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance may be recursive; otherwise, <c>false</c>.
        /// </value>
        public bool IsRecursive => FunctionCalls.Contains(this) || UnresolvedCallSites.Any(inst => inst.Block.IsExecutable);
        /// <summary>
        /// Gets the return value of this function.
        /// </summary>
        public PhiOperand Returns { get; } = new PhiOperand();
        public bool IsInvariant
            => Returns.IsInvariant &&
            IsInert;
        public bool IsInert
            => IsSelfInert &&
            FunctionCalls.Where(f => f != this).All(function => function.IsSelfInert);
        public bool IsSelfInert
        {
            get
            {
                if (ExternalWrites.Count > 0 || ExternalUnsets.Count > 0)
                    return false;

                return Fragments.All(fragment =>
                    fragment.Blocks.Where(block => block.IsExecutable).All(block =>
                        block.Instructions.All(instruction =>
                        {
                            foreach (IRInstruction operation in instruction.DepthFirstInstructions())
                                if (operation is IActionInstruction actionInstruction &&
                                    !(actionInstruction is IRCall call &&
                                        call.TargetMethod is InterimUserFunction userFunc &&
                                        userFunc.Function == this) &&
                                    !actionInstruction.IsInert)
                                    return false;
                            return true;
                        })
                    )
                );
            }
        }

        public HashSet<IRCall> CallSites { get; } = new HashSet<IRCall>(IRInstruction.ReferenceEqualityComparer);
        public IEnumerable<BasicBlock> RootBlocks => Fragments.Select(CodeComponent.GetRootBlock);
        public BasicBlock TerminalBlock { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="IRFunction"/> class.
        /// </summary>
        /// <param name="builder">The IRBuilder object in use.</param>
        /// <param name="function">The user function object to convert.</param>
        public IRFunction(UserFunction function, IRCodePart codePart)
        {
            CodePart = codePart;
            this.function = function;
            (ClosureScope, IsGlobal) = codePart.GetClosureScope(Identifier);
            InitializationCode = new CodeElement(function.InitializationCode, CodePart, ClosureScope);
            userFunctionFragments = function.PeekNewCodeFragments().ToList();
            foreach (UserFunctionCodeFragment fragment in userFunctionFragments)
            {
                fragments.Add(fragment, new IRFunctionFragment(fragment, codePart, this));
            }
            userFunctionFragments.Reverse();

            foreach (IRFunctionFragment fragment in Fragments)
            {
                foreach (BasicBlock block in fragment.Blocks)
                {
                    if (block.Successors.Any(b => !(b is SyntheticReturnBlock)))
                        continue;
                    if (!(block.Instructions[block.Instructions.Count - 1] is IRReturn ret))
                        Returns.PossibleValues[block] = null;
                    else
                        Returns.PossibleValues[block] = ret;
                }
            }
        }

        /// <summary>
        /// Emits the code into Opcode representation back into the
        /// trigger's source object.
        /// </summary>
        /// <param name="emitter">The IREmitter object in use.</param>
        public void EmitCode(IREmitter emitter)
        {
            InitializationCode.EmitCode(emitter, function.InitializationCode);
            foreach (UserFunctionCodeFragment fragment in userFunctionFragments)
            {
                fragments[fragment].EmitCode(emitter);
            }
        }

        public override string ToString()
            => $"IRFunction: {Identifier}";
    }
}
