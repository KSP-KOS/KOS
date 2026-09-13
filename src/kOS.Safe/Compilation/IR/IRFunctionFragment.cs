using System;
using System.Collections.Generic;
using kOS.Safe.Compilation.KS;

namespace kOS.Safe.Compilation.IR
{
    /// <summary>
    /// This class represents a function fragment. See <seealso cref="UserFunctionCodeFragment"/>.
    /// </summary>
    /// <seealso cref="CodeElement" />
    public class IRFunctionFragment : CodeElement
    {
        private readonly UserFunctionCodeFragment fragment;
        public IRFunction Function { get; }
        public override HashSet<string> ExternalReads
        {
            get => Function.ExternalReads;
            set => Function.ExternalReads = value;
        }
        public override HashSet<string> ExternalWrites => Function.ExternalWrites;
        public override HashSet<(string Name, IRUnset Instruction)> ExternalUnsets => Function.ExternalUnsets;
        public override HashSet<IInterimFunction> FunctionCalls => Function.FunctionCalls;
        public override HashSet<IRInstruction> UnresolvedCallSites => Function.UnresolvedCallSites;
        public override HashSet<IRTrigger> TriggersCreated => Function.TriggersCreated;
        public override BasicBlock TerminalBlock
        {
            get => Function.TerminalBlock;
            set => Function.TerminalBlock = value;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="IRFunctionFragment"/> class.
        /// </summary>
        /// <param name="codeFragment">The function code fragment to convert.</param>
        /// <param name="codePart">The parent IRCodePart object.</param>
        /// <param name="function">The parent function object.</param>
        public IRFunctionFragment(UserFunctionCodeFragment codeFragment, IRCodePart codePart, IRFunction function) : base(codePart, function.ClosureScope)
        {
            fragment = codeFragment;
            Function = function;
            Lower(codeFragment.Code);
        }
        public void EmitCode(IREmitter emitter)
            => EmitCode(emitter, fragment.Code);
        public override string ToString()
            => $"IRFunctionFragment: {Function.Identifier}";
    }
}
