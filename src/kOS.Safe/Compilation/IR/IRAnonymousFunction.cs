using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Compilation.Optimization;

namespace kOS.Safe.Compilation.IR
{
    /// <summary>
    /// This class represents an anonymous function, which
    /// is contained in a <see cref="CodeComponent"/>.
    /// </summary>
    /// <seealso cref="CodeComponent" />
    /// <seealso cref="ILabeledComponent" />
    /// <seealso cref="IInterimFunction" />
    public class IRAnonymousFunction : CodeComponent, ILabeledComponent, IInterimFunction
    {
        public string Identifier { get; }
        public string Label { get; private set; }
        public bool IsRecursive => FunctionCalls.Any(f => f.IsRecursive);
        public PhiOperand Returns { get; } = new PhiOperand();
        public bool IsInvariant
            => Returns.IsInvariant &&
            IsInert;
        public bool IsInert
            => IsSelfInert &&
            FunctionCalls.All(function => function.IsSelfInert);
        public bool IsSelfInert
        {
            get
            {
                if (ExternalWrites.Count > 0 || ExternalUnsets.Count > 0)
                    return false;

                return Blocks.Where(block => block.IsExecutable).All(block =>
                    block.Instructions.All(instruction =>
                    {
                        foreach (IRInstruction operation in instruction.DepthFirstInstructions())
                            if (operation is IActionInstruction actionInstruction &&
                                !actionInstruction.IsInert)
                                return false;
                        return true;
                    }
                    )
                );
            }
        }
        public HashSet<IRCall> CallSites { get; } = new HashSet<IRCall>(IRInstruction.ReferenceEqualityComparer);

        public IRAnonymousFunction(string identifier, IEnumerable<BasicBlock> blocks, IRCodePart codePart, IRScope parentScope) : base(codePart, parentScope)
        {
            Identifier = identifier;
            Label = Identifier;
            Blocks = new List<BasicBlock>(blocks);
            RootBlock = Blocks.FirstOrDefault();

            TerminalBlock = new SyntheticReturnBlock(this) { Scope = parentScope };
            foreach (BasicBlock block in Blocks)
            {
                block.CodeComponent = this;
                switch (block.Continuation)
                {
                    case JumpContinuation jump:
                        if (jump.Target is SyntheticReturnBlock)
                            jump.Target = TerminalBlock;
                        break;
                    case BranchContinuation branch:
                        if (branch.True is SyntheticReturnBlock)
                            branch.True = TerminalBlock;
                        if (branch.False is SyntheticReturnBlock)
                            branch.False = TerminalBlock;
                        break;
                    case JumpStackContinuation jumpStack:
                        List<BasicBlock> targets = jumpStack.Targets.ToList();
                        for (int i = targets.Count - 1; i >= 0; i--)
                            if (targets[i] is SyntheticReturnBlock)
                                targets[i] = TerminalBlock;
                        jumpStack.Targets = targets;
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            foreach (BasicBlock block in Blocks)
            {
                if (block.Successors.Any(b => !(b is SyntheticReturnBlock)))
                    continue;
                if (!(block.Instructions[block.Instructions.Count - 1] is IRReturn ret))
                    Returns.PossibleValues[block] = null;
                else
                    Returns.PossibleValues[block] = ret;
            }
        }

        public override void EmitCode(IREmitter emitter, List<Opcode> target)
        {
            List<Opcode> result = emitter.Emit(Blocks);
            Label = result.FirstOrDefault()?.Label;
            target.AddRange(result);
        }

        public override string ToString()
            => $"AnonymousFunction: {Identifier}";
    }
}
