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
    public class BasicBlock
    {
        private static uint nextID = 0;
        private readonly HashSet<BasicBlock> predecessors = new HashSet<BasicBlock>();
        private readonly HashSet<BasicBlock> successors = new HashSet<BasicBlock>();
        private BasicBlock dominator;
        private readonly HashSet<BasicBlock> dominates = new HashSet<BasicBlock>();
        private BasicBlock postDominator;
        private readonly HashSet<BasicBlock> postDominates = new HashSet<BasicBlock>();
        private readonly List<IRParameter> parameters = new List<IRParameter>();
        private readonly Stack<IInterimOperand> exitStackState = new Stack<IInterimOperand>();  // Note that this is reversed from the real stack. Just now we don't reverse it four times.
        private readonly string nonSequentialLabel = null;
        private IRScope scope;

        public static void ResetNextID()
            => nextID = 0;

        public IRCodePart CodePart { get; }
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
        public Dictionary<(string, IRScope), SSADefinition> IncomingVariableDefinitions { get; internal set; }
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
        public IRJump FallthroughJump { get; set; } = null;
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
        public BasicBlock(IRCodePart codePart, int startIndex, int endIndex, string nonSequentialLabel = null)
        {
            CodePart = codePart;
            StartIndex = startIndex;
            EndIndex = endIndex;
            unchecked
            {
                ID = nextID++;
            }
            this.nonSequentialLabel = nonSequentialLabel;
        }

        /// <summary>
        /// Adds the specified instruction to this block's list of instructions.
        /// </summary>
        /// <param name="instruction">The instruction to add.</param>
        public void Add(IRInstruction instruction)
            => Instructions.Add(instruction);

        /// <summary>
        /// Adds a successor block.
        /// </summary>
        /// <param name="successor">The successor block to add.</param>
        public void AddSuccessor(BasicBlock successor)
        {
            successors.Add(successor);
            successor.AddPredecessor(this);
        }

        /// <summary>
        /// Adds a predecessor block.
        /// </summary>
        /// <param name="predecessor">The predecessor block to add.</param>
        protected void AddPredecessor(BasicBlock predecessor)
        {
            predecessors.Add(predecessor);
        }

        /// <summary>
        /// Removes a successor block.
        /// </summary>
        /// <param name="successor">The successor block to remove.</param>
        /// <exception cref="ArgumentException">Cannot remove <paramref name="successor"/> as it is not a successor.</exception>
        public void RemoveSuccessor(BasicBlock successor)
        {
            // Break the appropriate links
            if (!successors.Remove(successor))
                throw new ArgumentException($"Cannot remove {successor} as it is not a successor.");
            successor.predecessors.Remove(this);

            // Recompute the Dominance tree(s)
            EstablishDominance();

            // Recompute the Post-Dominance tree(s)
            successor.EstablishPostDominance();
        }

        public static IEnumerable<BasicBlock> GetSuccessors(BasicBlock block)
            => block.successors;
        private static BasicBlock GetDominator(BasicBlock block)
            => block.Dominator;
        private static void SetDominator(BasicBlock block, BasicBlock dominator)
            => block.Dominator = dominator;

        /// <summary>
        /// Establishes the dominance tree.
        /// </summary>
        public void EstablishDominance()
            => EstablishDominanceCore(this, GetPredecessors, GetSuccessors, GetDominator, SetDominator);

        public static IEnumerable<BasicBlock> GetPredecessors(BasicBlock block)
            => block.predecessors;
        private static BasicBlock GetPostDominator(BasicBlock block)
            => block.PostDominator;
        private static void SetPostDominator(BasicBlock block, BasicBlock postDominator)
            => block.PostDominator = postDominator;

        /// <summary>
        /// Establishes the post-dominance tree.
        public void EstablishPostDominance()
            => EstablishDominanceCore(this, GetSuccessors, GetPredecessors, GetPostDominator, SetPostDominator);

        private static void EstablishDominanceCore(BasicBlock root, Func<BasicBlock, IEnumerable<BasicBlock>> getPrecedents, Func<BasicBlock, IEnumerable<BasicBlock>> getSubsequents, Func<BasicBlock, BasicBlock> getDominator, Action<BasicBlock, BasicBlock> setDominator)
        {
            while (getDominator(root) != null)
                root = getDominator(root);

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

                        if (getDominator(predecessor) != null)
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

        /// <summary>
        /// Adds a parameter to this block.
        /// </summary>
        /// <param name="parameter">The parameter object to add.</param>
        public void AddParameter(IRParameter parameter)
        {
            parameters.Add(parameter);
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

        /// <summary>
        /// Sets the state of the stack upon exiting this block.
        /// </summary>
        /// <param name="stack">The stack state to set.</param>
        /// <remarks>
        /// Use extreme caution when manipulating the stack state.
        /// </remarks>
        public void SetStackState(Stack<IInterimOperand> stack)
        {
            while (stack.Count > 0)
                exitStackState.Push(stack.Pop());
        }

        public override string ToString()
        {
            return $"BasicBlock#{ID}: {StartIndex}-{EndIndex}; {Instructions.Count} Instructions";
        }

        /// <summary>
        /// Emits the the sequence of Opcodes for the instructions
        /// contained in this block.
        /// </summary>
        public IEnumerable<Opcode> EmitOpCodes()
        {
            bool addedFallthrough = FallthroughJump != null && Instructions.LastOrDefault() == FallthroughJump;
            if (addedFallthrough)
                Instructions.Add(FallthroughJump);
            bool first = true;
            foreach (IRInstruction instruction in Instructions.Take(Instructions.Count - 1))
            {
                foreach (Opcode opcode in instruction.EmitOpcodes())
                {
                    if (first)
                    {
                        opcode.Label = Label;
                        first = false;
                    }
                    yield return opcode;
                }
            }
            foreach (IInterimOperand stackValue in exitStackState)
            {
                foreach (Opcode opcode in stackValue.EmitOpcodes())
                {
                    if (first)
                    {
                        opcode.Label = Label;
                        first = false;
                    }
                    yield return opcode;
                }
            }
            if (Instructions.Any())
            {
                foreach (Opcode opcode in Instructions.Last().EmitOpcodes())
                {
                    if (first)
                    {
                        opcode.Label = Label;
                        first = false;
                    }
                    yield return opcode;
                }
            }
            if (addedFallthrough)
                Instructions.Remove(FallthroughJump);
        }
    }

    public sealed class SyntheticReturnBlock : BasicBlock
    {
        public SyntheticReturnBlock(IRCodePart codePart) : base(codePart, -1, -1, "syntheticReturn")
        {
        }
    }
}
