using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Compilation.Optimization;

namespace kOS.Safe.Compilation.IR
{
    /// <summary>
    /// This class describes a basic block for the optimizing compiler.
    /// A basic block runs in its entirety without branching (function
    /// calls are allowed), and is limited to a single scope.
    /// </summary>
    public partial class BasicBlock
    {
        private static uint nextID = 0;
        private readonly HashSet<BasicBlock> predecessors = new HashSet<BasicBlock>();
        private readonly HashSet<BasicBlock> successors = new HashSet<BasicBlock>();
        private BasicBlock dominator;
        private readonly HashSet<BasicBlock> dominates = new HashSet<BasicBlock>();
        private BasicBlock postDominator;
        private readonly HashSet<BasicBlock> postDominates = new HashSet<BasicBlock>();
        private readonly List<IRParameter> parameters = new List<IRParameter>();
        private readonly string nonSequentialLabel = null;
        private IRScope scope;
        private BlockContinuation continuation;

        public static void ResetNextID()
            => nextID = 0;

        public IRCodePart CodePart { get; }
        public ICodeComponent CodeComponent { get; private set; }
        /// <summary>
        /// Gets or sets a value indicating whether this block is executable (reachable).
        /// </summary>
        /// <value>
        ///   <c>true</c> if this block is executable; otherwise, <c>false</c>.
        /// </value>
        public bool IsExecutable { get; set; }
        /// <summary>
        /// Gets or sets the scope of this block. This describes the
        /// narrowest scope at the time of this block's execution.
        /// </summary>
        public IRScope Scope
        {
            get => scope;
            set
            {
                scope?.RemoveBlock(this);
                scope = value;
                scope.EnrollBlock(this);
            }
        }
        /// <summary>
        /// Gets the start index of the opcodes that form this block.
        /// </summary>
        public int StartIndex { get; }
        /// <summary>
        /// Gets the end index of the opcodes that form this block.
        /// </summary>
        public int EndIndex { get; }
        /// <summary>
        /// Gets the list of instructions that this block executes. Note
        /// that the instructions listed here are only the terminal
        /// instructions - those that set a variable or suffix, those that
        /// branch to another block, or those that pop an item from the
        /// stack.
        /// </summary>
        public List<IRInstruction> Instructions { get; } = new List<IRInstruction>();
        /// <summary>
        /// Gets the successor blocks of this block. These are the blocks
        /// to which flow can branch after completing this block.
        /// </summary>
        /// <remarks>
        /// Note that a block can be one of its own successors, so be
        /// cautious of creating infinite loops when iterating using
        /// successors.
        /// </remarks>
        public IReadOnlyCollection<BasicBlock> Successors => successors;
        /// <summary>
        /// Gets the predecessor blocks of this block. These are the
        /// blocks from which flow can be entering this block.
        /// </summary>
        /// <remarks>
        /// Note that a block can be one of its own predecessors, so be
        /// cautious of creating infinite loops when iterating using
        /// predecessors.
        /// </remarks>
        public IReadOnlyCollection<BasicBlock> Predecessors => predecessors;
        /// <summary>
        /// Gets the collection of phi functions that define variables
        /// that may have one of several different values upon entering
        /// this block.
        /// </summary>
        /// <remarks>
        /// This data is populated during <see cref="SingleStaticAssignment.FinalizeSSA(IRCodePart)"/>.
        /// </remarks>
        public Dictionary<(string Name, IRScope Scope), PhiNode> Phis { get; } =
            new Dictionary<(string Name, IRScope Scope), PhiNode>();
        /// <summary>
        /// Gets the set of incoming SSA variables that this block
        /// receives, including the results of any phi functions.
        /// </summary>
        /// <remarks>
        /// This data is populated during <see cref="SingleStaticAssignment.FinalizeSSA(IRCodePart)"/>.
        /// </remarks>
        public Dictionary<(string Name, IRScope Scope), SSADefinition> IncomingVariableDefinitions { get; internal set; }
        /// <summary>
        /// Gets the state of the incoming stack.
        /// </summary>
        /// <remarks>
        /// This data is populated during <see cref="SingleStaticAssignment.FinalizeSSA(IRCodePart)"/>.
        /// </remarks>
        public List<IStackTransferObject> IncomingStackState { get; } = new List<IStackTransferObject>();
        /// <summary>
        /// Gets the set of variables that are blacklisted against
        /// caching or propagation due to their presence in active
        /// triggers.
        /// </summary>
        /// <remarks>
        /// This data is populated during <see cref="SingleStaticAssignment.FinalizeSSA(IRCodePart)"/>.
        /// </remarks>
        public HashSet<(string, IRScope)> TriggerPropagationBlacklist { get; } = new HashSet<(string, IRScope)>();
        /// <summary>
        /// Gets the set of variables that are blacklisted against
        /// setting definitive values due to their being unset in active
        /// triggers.
        /// </summary>
        /// <remarks>
        /// This data is populated during <see cref="SingleStaticAssignment.FinalizeSSA(IRCodePart)"/>.
        /// </remarks>
        public Dictionary<(string, IRScope), IRUnset> TriggerUnsetBlacklist { get; } = new Dictionary<(string, IRScope), IRUnset>();
        /// <summary>
        /// Gets the instruction label with which to start the block.
        /// The special prefix "@BB#" will be overwritten during linking.
        /// </summary>
        public string Label => nonSequentialLabel ?? $"@BB#{ID}";
        /// <summary>
        /// Gets a unique ID to help identify this basic block during debugging.
        /// </summary>
        public uint ID { get; }
        /// <summary>
        /// Gets the BasicBlock that dominates this block. That is,
        /// the most recent predecessor that is guaranteed to have
        /// executed before this block.
        /// </summary>
        public BasicBlock Dominator
        {
            get => dominator;
            private set
            {
                dominator?.dominates.Remove(this);
                dominator = value;
                dominator?.dominates.Add(this);
            }
        }
        /// <summary>
        /// Gets the collection of BasicBlocks for which this block is
        /// the Dominator.
        /// </summary>
        public IReadOnlyCollection<BasicBlock> Dominates => dominates;
        /// <summary>
        /// Gets the BasicBlock that post-dominates this block. That is,
        /// the earliest successor that is guaranteed to be
        /// executed after this block.
        /// </summary>
        public BasicBlock PostDominator
        {
            get => postDominator;
            private set
            {
                postDominator?.postDominates.Remove(this);
                postDominator = value;
                postDominator?.postDominates.Add(this);
            }
        }
        /// <summary>
        /// Gets the collection of BasicBlocks for which this block is
        /// the Post-Dominator.
        /// </summary>
        public IReadOnlyCollection<BasicBlock> PostDominates => postDominates;
        /// <summary>
        /// Gets or sets the Extended Basic Block of which this block
        /// is a member.
        /// </summary>
        public ExtendedBasicBlock ExtendedBlock { get; set; }
        /// <summary>
        /// Gets or sets the <see cref="IRJump"/> instruction that this
        /// block will terminate with if it does not branch to another.
        /// </summary>
        public BlockContinuation Continuation
        {
            get => continuation;
            set
            {
                if (continuation == value)
                    return;
                List<BasicBlock> removed = new List<BasicBlock>(continuation?.Destinations ?? Enumerable.Empty<BasicBlock>());
                if (continuation != null)
                {
                    continuation.DestinationChanged -= OnDestinationChanged;
                    continuation.AssignedTo = null;
                }
                continuation = value;
                if (continuation != null)
                {
                    continuation.DestinationChanged += OnDestinationChanged;
                    continuation.AssignedTo = this;
                }
                List<BasicBlock> added = new List<BasicBlock>(continuation?.Destinations ?? Enumerable.Empty<BasicBlock>());

                OnDestinationChanged(this, new BlockContinuation.TargetChangedEvent(added.Except(removed), removed.Except(added)));
            }
        }
#if DEBUG
        internal Opcode[] OriginalOpcodes { get; set; }
        internal Opcode[] GeneratedOpcodes => EmitOpCodes().ToArray();
#endif

        /// <summary>
        /// Initializes a new instance of a <see cref="BasicBlock"/>.
        /// </summary>
        /// <param name="startIndex">The starting index in the original sequence of <see cref="Opcode"/>s.</param>
        /// <param name="endIndex">The ending index in the original sequence of <see cref="Opcode"/>s.</param>
        /// <param name="nonSequentialLabel">A non sequential label, if present.</param>
        public BasicBlock(ICodeComponent codeComponent, int startIndex, int endIndex, string nonSequentialLabel = null)
        {
            CodeComponent = codeComponent;
            CodePart = codeComponent.CodePart;
            StartIndex = startIndex;
            EndIndex = endIndex;
            if (nonSequentialLabel == SyntheticReturnBlock.syntheticReturnLabel)
                ID = uint.MaxValue;
            else
            {
                unchecked
                {
                    ID = nextID++;
                }
            }
            this.nonSequentialLabel = nonSequentialLabel;
        }

        /// <summary>
        /// Adds the specified instruction to this block's list of instructions.
        /// </summary>
        /// <param name="instruction">The instruction to add.</param>
        public void Add(IRInstruction instruction)
            => Instructions.Add(instruction);

        public int GetOpcodeCount()
        {
            int length = 0;
            foreach (IRInstruction instruction in Instructions)
            {
                if (instruction is IOperandInstructionBase operandInstruction)
                    operandInstruction.ForEachOperand(op =>
                    {
                        if (op is IResultingInstruction resultingInstruction)
                            length += resultingInstruction.OpcodeCount;
                        else
                            length += 1;

                    });
                length += 1;
                if (instruction is IRPushStack)
                    length -= 1;
            }
            length += Continuation?.OpcodeCount() ?? 0;
            return length;
        }
        public static int GetOpcodeCount(IEnumerable<BasicBlock> blocks)
            => blocks.Sum(b => b.GetOpcodeCount());

        protected void OnDestinationChanged(object sender, BlockContinuation.TargetChangedEvent eventData)
        {
            List<BasicBlock> removed = eventData.BlocksRemoved.ToList();
            List<BasicBlock> added = eventData.BlocksAdded.ToList();

            foreach (BasicBlock successor in added.Except(removed))
            {
                successors.Add(successor);
                successor.predecessors.Add(this);
            }
            foreach (BasicBlock oldSuccessor in removed.Except(added))
            {
                successors.Remove(oldSuccessor);
                oldSuccessor.predecessors.Remove(this);
            }
            foreach (ICodeComponent component in new[] { this }.Concat(added.Concat(removed)).Select(b => b.CodeComponent).Distinct())
            {
                component.RootBlock.EstablishDominance();
                component.RootBlock.EstablishPostDominance();
            }
        }

        public static IEnumerable<BasicBlock> GetSuccessors(BasicBlock block)
            => block.Successors;
        private static BasicBlock GetDominator(BasicBlock block)
            => block.Dominator;
        private static void SetDominator(BasicBlock block, BasicBlock dominator)
            => block.Dominator = dominator;

        /// <summary>
        /// Establishes the dominance tree.
        /// </summary>
        public void EstablishDominance()
            => EstablishDominanceCore(CodeComponent.RootBlock, GetPredecessors, GetSuccessors, GetDominator, SetDominator);

        public static IEnumerable<BasicBlock> GetPredecessors(BasicBlock block)
            => block.Predecessors;
        private static BasicBlock GetPostDominator(BasicBlock block)
            => block.PostDominator;
        private static void SetPostDominator(BasicBlock block, BasicBlock postDominator)
            => block.PostDominator = postDominator;

        /// <summary>
        /// Establishes the post-dominance tree.
        public void EstablishPostDominance()
        {
            List<BasicBlock> reversePostOrder = GetReversePostOrder(CodeComponent.RootBlock, GetSuccessors);
            BasicBlock tail = reversePostOrder.Contains(CodeComponent.TerminalBlock) ?
                CodeComponent.TerminalBlock : reversePostOrder.Last();
            EstablishDominanceCore(tail, GetSuccessors, GetPredecessors, GetPostDominator, SetPostDominator);
        }

        private static void EstablishDominanceCore(BasicBlock root, Func<BasicBlock, IEnumerable<BasicBlock>> getPrecedents, Func<BasicBlock, IEnumerable<BasicBlock>> getSubsequents, Func<BasicBlock, BasicBlock> getDominator, Action<BasicBlock, BasicBlock> setDominator)
        {
            // Compute reverse postorder
            List<BasicBlock> reversePostOrder = GetReversePostOrder(root, getSubsequents);

            // Map block to index
            Dictionary<BasicBlock, int> index = new Dictionary<BasicBlock, int>();
            for (int i = 0; i < reversePostOrder.Count; i++)
            {
                index[reversePostOrder[i]] = i;
                // Initialize dominators to null.
                setDominator(reversePostOrder[i], null);
            }

            // Initialize
            reversePostOrder.Remove(root);
            bool changed = true;
            while (changed)
            {
                changed = false;

                foreach (BasicBlock block in reversePostOrder)
                {
                    // Pick first predecessor with defined dominator
                    BasicBlock newIdom = getPrecedents(block).Where(p => p != block).Where(index.ContainsKey).FirstOrDefault
                        (p => p == root || getDominator(p) != null);

                    if (newIdom == null)
                        continue;

                    foreach (BasicBlock predecessor in getPrecedents(block).Where(index.ContainsKey))
                    {
                        if (predecessor == newIdom)
                            continue;

                        if (getDominator(predecessor) != null || predecessor == root)
                            newIdom = Intersect(predecessor, newIdom, index, getDominator);
                    }

                    if (getDominator(block) != newIdom)
                    {
                        setDominator(block, newIdom);
                        changed = true;
                    }
                }
            }
        }
        private static BasicBlock Intersect(BasicBlock b1, BasicBlock b2, Dictionary<BasicBlock, int> index, Func<BasicBlock, BasicBlock> getDominator)
        {
            while (b1 != b2)
            {
                while (index[b1] > index[b2])
                    b1 = getDominator(b1);

                while (index[b2] > index[b1])
                    b2 = getDominator(b2);
            }

            return b1;
        }
        public static List<BasicBlock> GetReversePostOrder(BasicBlock root, Func<BasicBlock, IEnumerable<BasicBlock>> getEdges)
        {
            List<BasicBlock> result = new List<BasicBlock>();
            HashSet<BasicBlock> visited = new HashSet<BasicBlock>();
            DepthFirstSearch(root, visited, result, getEdges);
            result.Reverse();
            return result;
        }
        private static void DepthFirstSearch(BasicBlock block, HashSet<BasicBlock> visited, List<BasicBlock> postorder, Func<BasicBlock, IEnumerable<BasicBlock>> getEdges)
        {
            if (!visited.Add(block))
                return;

            foreach (BasicBlock successor in getEdges(block))
                DepthFirstSearch(successor, visited, postorder, getEdges);

            postorder.Add(block);
        }

        public bool IsDominatedBy(BasicBlock block, BasicBlock stopAt = null)
        {
            BasicBlock current = this;
            while (current != null)
            {
                if (current == stopAt)
                    return false;
                if (current == block)
                    return true;
                current = current.Dominator;
            }
            return false;
        }

        /// <summary>
        /// Adds a parameter to this block.
        /// </summary>
        public IInterimOperand AddParameter()
        {
            IRParameter parameter = new IRParameter(parameters.Count, this);
            parameters.Add(parameter);
            return parameter;
        }

        /// <summary>
        /// Alias for <see cref="IRScope.GetScopeForVariableNamed(string)"/>
        /// using this block's <see cref="Scope"/>.
        /// </summary>
        /// <param name="name">The name of the variable to find.</param>
        /// <returns>
        /// The IRScope object containing the supplied variable, or the
        /// global scope if the variable is not yet tracked.
        /// </returns>
        public IRScope GetScopeForVariableNamed(string name)
            => Scope.GetScopeForVariableNamed(name);

        public override string ToString()
        {
            return $"BasicBlock#{ID}: {StartIndex}-{EndIndex}; {Instructions.Count} Instructions";
        }

        /// <summary>
        /// Emits the the sequence of Opcodes for the instructions
        /// contained in this block.
        /// </summary>
        public virtual IEnumerable<Opcode> EmitOpCodes()
        {
            bool first = true;
            foreach (Opcode opcode in Instructions.SelectMany(i => i.EmitOpcodes()).Union(Continuation?.EmitOpcodes() ?? Enumerable.Empty<Opcode>()))
            {
                if (first)
                {
                    opcode.Label = Label;
                    first = false;
                }
                yield return opcode;
            }
        }
    }

    public sealed class SyntheticReturnBlock : BasicBlock
    {
        public const string syntheticReturnLabel = "syntheticReturn";
        public SyntheticReturnBlock(IRCodePart codePart) : base(codePart, -1, -1, syntheticReturnLabel)
        {
        }
        public override string ToString()
            => $"BasicBlock:SyntheticReturn";
    }
}
