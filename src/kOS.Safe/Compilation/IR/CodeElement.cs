using System;
using System.Collections.Generic;
using System.Linq;

namespace kOS.Safe.Compilation.IR
{
    /// <summary>
    /// This class represents a complete element within a program,
    /// for example the main code or a function or trigger body.
    /// It may include subcomponents: e.g. anonymous function bodies.
    /// </summary>
    /// <seealso cref="CodeComponent" />
    public class CodeElement : CodeComponent
    {
        public IEnumerable<BasicBlock> AllRootBlocks => new[] { RootBlock }.Concat(AnonymousFunctions.Select(GetRootBlock));
        public IEnumerable<BasicBlock> AllBlocks => Blocks.Concat(AnonymousFunctions.SelectMany(GetBlocks));
        public List<IRAnonymousFunction> AnonymousFunctions { get; set; } = new List<IRAnonymousFunction>();
        public CodeElement(List<Opcode> code, IRCodePart codePart, IRScope parentScope) : base(codePart, parentScope)
        {
            Lower(code);
        }
        protected CodeElement(IRCodePart codePart, IRScope parentScope) : base(codePart, parentScope)
        {
        }
        protected override void Lower(List<Opcode> code)
        {
            base.Lower(code);

            foreach (KeyValuePair<string, (BasicBlock, IRScope)> entry in anonymousFunctionsToBuild)
            {
                (BasicBlock root, IRScope closure) = entry.Value;
                if (root.Dominator != null)
                    throw new Exceptions.KOSYouShouldNeverSeeThisException("An anonymous function is poorly structured.");
                HashSet<BasicBlock> blocks = new HashSet<BasicBlock>();
                Queue<BasicBlock> queue = new Queue<BasicBlock>();
                queue.Enqueue(root);
                while (queue.Count > 0)
                {
                    BasicBlock block = queue.Dequeue();
                    if (block is SyntheticReturnBlock)
                        continue;
                    Blocks.Remove(block);
                    if (blocks.Add(block))
                        foreach (BasicBlock successor in block.Successors)
                            queue.Enqueue(successor);
                }
                AnonymousFunctions.Add(new IRAnonymousFunction(entry.Key, blocks, CodePart, closure));
            }
            anonymousFunctionsToBuild = null;
        }
        private Dictionary<string, (BasicBlock, IRScope)> anonymousFunctionsToBuild = new Dictionary<string, (BasicBlock, IRScope)>();
        public void EnrollAnonymousFunction(string pointer, BasicBlock rootBlock, IRScope closureScope)
        {
            anonymousFunctionsToBuild.Add(pointer, (rootBlock, closureScope));
        }
        public override void EmitCode(IREmitter emitter, List<Opcode> target)
        {
            target.Clear();
            PreEmitPrep(out JumpContinuation syntheticContinuation, out BasicBlock originalTarget, AnonymousFunctions.Count > 0).LastOrDefault();
            List<BasicBlock> output = new List<BasicBlock>(Blocks);
            Dictionary<IRAnonymousFunction, (JumpContinuation, BasicBlock)> anonymousFuncData = new Dictionary<IRAnonymousFunction, (JumpContinuation, BasicBlock)>();
            foreach (IRAnonymousFunction anonymousFunction in AnonymousFunctions)
            {
                anonymousFunction.EOFJumpBlock = EOFJumpBlock;
                anonymousFunction.PreEmitPrep(out JumpContinuation funcContinuation, out BasicBlock funcTarget, true);
                anonymousFuncData[anonymousFunction] = (funcContinuation, funcTarget);
                output.AddRange(anonymousFunction.Blocks);
            }
            if (syntheticContinuation != null)
                output.Add(EOFJumpBlock);

            target.AddRange(emitter.Emit(output));

            PostEmitCleanup(syntheticContinuation, originalTarget);
            foreach (IRAnonymousFunction anonymousFunction in AnonymousFunctions)
            {
                (JumpContinuation funcContinuation, BasicBlock funcTarget) = anonymousFuncData[anonymousFunction];
                anonymousFunction.PostEmitCleanup(funcContinuation, funcTarget);
            }
        }
        internal static IEnumerable<CodeComponent> GetComponents(CodeElement codeUnit)
            => new CodeComponent[] { codeUnit }.Concat(codeUnit.AnonymousFunctions);
        public override string ToString()
            => $"CodeElement: Root {RootBlock?.Label}";
    }
}
