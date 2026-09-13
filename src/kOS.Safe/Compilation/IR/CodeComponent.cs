using System;
using System.Collections.Generic;
using System.Linq;

namespace kOS.Safe.Compilation.IR
{
    /// <summary>
    /// This class represents a piece of code, made up
    /// of one or more <see cref="BasicBlock"/>s.
    /// </summary>
    /// <seealso cref="IClosureVariableUser" />
    public abstract class CodeComponent : IClosureVariableUser
    {
        public BasicBlock EOFJumpBlock { get; protected internal set; } = null;
        public IRCodePart CodePart { get; }
        /// <summary>
        /// Gets or sets the code for this component, in BasicBlock representation.
        /// </summary>
        public List<BasicBlock> Blocks { get; set; }
        public BasicBlock RootBlock { get; set; }
        public virtual BasicBlock TerminalBlock { get; set; }
        /// <summary>
        /// Gets the scope of the closure this instance uses.
        /// </summary>
        public IRScope ClosureScope { get; }
        public virtual HashSet<string> ExternalReads { get; set; } = new HashSet<string>();
        public virtual HashSet<string> ExternalWrites { get; } = new HashSet<string>();
        public virtual HashSet<(string Name, IRUnset Instruction)> ExternalUnsets { get; } = new HashSet<(string, IRUnset)>();
        public virtual HashSet<IRTrigger> TriggersCreated { get; } = new HashSet<IRTrigger>();
        public virtual HashSet<IInterimFunction> FunctionCalls { get; } = new HashSet<IInterimFunction>();
        public virtual HashSet<IRInstruction> UnresolvedCallSites { get; } = new HashSet<IRInstruction>(IRInstruction.ReferenceEqualityComparer);

        protected CodeComponent(IRCodePart codePart, IRScope parentScope)
        {
            CodePart = codePart;
            ClosureScope = parentScope;
        }

        protected virtual void Lower(List<Opcode> code)
        {
            Blocks = IRBuilder.Lower(code, this, ClosureScope);
            RootBlock = Blocks.FirstOrDefault();
        }

        protected internal virtual IEnumerable<BasicBlock> PreEmitPrep(out JumpContinuation syntheticContinuation, out BasicBlock originalTarget, bool forceEOFBlock = false)
        {
            // If the streamlined sequence for the primary code path
            // ends before the end of the file,
            // this adds a jump to a nop at the end of the file so that
            // code flow does not fall through to other blocks.
            int lastMainlineIndex = Blocks.FindIndex(b => b.Continuation is JumpContinuation jump && jump.Target is SyntheticReturnBlock);
            if ((forceEOFBlock ||
                (lastMainlineIndex >= 0 &&
                lastMainlineIndex < Blocks.Count - 1)) &&
                !(Blocks[lastMainlineIndex].Instructions.LastOrDefault() is IRReturn))
            {
                syntheticContinuation = (JumpContinuation)Blocks[lastMainlineIndex].Continuation;
                originalTarget = syntheticContinuation.Target;
                if (EOFJumpBlock == null)
                {
                    EOFJumpBlock = BasicBlock.InsertBlockBetween(Blocks[lastMainlineIndex], originalTarget);
                    Blocks.Remove(EOFJumpBlock);
                    EOFJumpBlock.Add(new IRNoStackInstruction(EOFJumpBlock,
                    new OpcodeNOP()
                    {
                        SourceLine = syntheticContinuation.SourceLine,
                        SourceColumn = syntheticContinuation.SourceColumn
                    },
                    true));
                }
                else
                {
                    syntheticContinuation.Target = EOFJumpBlock;
                }
                return Blocks.Concat(new[] { EOFJumpBlock });
            }
            else
            {
                syntheticContinuation = null;
                originalTarget = null;
                return Blocks;
            }
        }

        protected internal virtual void PostEmitCleanup(JumpContinuation syntheticContinuation, BasicBlock originalTarget)
        {
            if (syntheticContinuation != null)
                syntheticContinuation.Target = originalTarget;
        }

        public virtual void EmitCode(IREmitter emitter, List<Opcode> target)
        {
            target.Clear();
            IEnumerable<BasicBlock> output = PreEmitPrep(out JumpContinuation syntheticContinuation, out BasicBlock originalTarget);
            target.AddRange(emitter.Emit(output));
            PostEmitCleanup(syntheticContinuation, originalTarget);
        }
        internal static BasicBlock GetRootBlock(CodeComponent component)
            => component.RootBlock;
        internal static IEnumerable<BasicBlock> GetBlocks(CodeComponent component)
            => component.Blocks;
        public override string ToString()
            => $"CodeComponent: Root {RootBlock?.Label}";
    }
}