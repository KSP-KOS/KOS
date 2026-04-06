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
        private readonly List<IRParameter> parameters = new List<IRParameter>();
        private readonly Stack<IRValue> exitStackState = new Stack<IRValue>();  // Note that this is reversed from the real stack. Just now we don't reverse it four times.
        private readonly string nonSequentialLabel = null;
        private IRScope scope;
        private readonly HashSet<SSAVariable> variablesWritten = new HashSet<SSAVariable>();
        private readonly HashSet<IRVariableBase> variablesRead = new HashSet<IRVariableBase>();

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
        /// Gets the collection of variables that are written in this
        /// block in SSA form. This list only returns the last SSA
        /// instance for a given variable name.
        /// </summary>
        public HashSet<SSAVariable> VariablesWritten => variablesWritten;
        /// <summary>
        /// Gets the collection of variables that are read in this block.
        /// This is not in SSA form and includes global and bound
        /// variables.
        /// </summary>
        public IReadOnlyCollection<IRVariableBase> VariablesRead => variablesRead;
        /// <summary>
        /// Gets the collection of phi functions that define variables
        /// that may have one of several different values upon entering
        /// this block.
        /// </summary>
        /// <remarks>
        /// This data is populated during <see cref="SingleStaticAssignment.FinalizeSSA(IRCodePart)"/>.
        /// </remarks>
        public Dictionary<IRVariable, (SSAVariable phiVar, Dictionary<BasicBlock, SSAVariable> values)> Phis { get; } =
            new Dictionary<IRVariable, (SSAVariable phiVar, Dictionary<BasicBlock, SSAVariable> values)>();
        /// <summary>
        /// Gets the set of incoming SSA variables that this block
        /// receives, including the results of any phi functions.
        /// </summary>
        /// <remarks>
        /// This data is populated during <see cref="SingleStaticAssignment.FinalizeSSA(IRCodePart)"/>.
        /// </remarks>
        public HashSet<SSAVariable> IncomingVariableDefinitions { get; internal set; }
        /// <summary>
        /// Gets the set of variables that are blacklisted against
        /// caching or propagation due to their presence in active
        /// triggers.
        /// </summary>
        /// <remarks>
        /// This data is populated during <see cref="SingleStaticAssignment.FinalizeSSA(IRCodePart)"/>.
        /// </remarks>
        public HashSet<IRVariable> TriggerPropagationBlacklist { get; } = new HashSet<IRVariable>();
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
        public BasicBlock(int startIndex, int endIndex, string nonSequentialLabel = null)
        {
            StartIndex = startIndex;
            EndIndex = endIndex;
            ID = nextID++;
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
        /// <exception cref="System.ArgumentException">Cannot remove <paramref name="successor"/> as it is not a successor.</exception>
        public void RemoveSuccessor(BasicBlock successor)
        {
            if (!successors.Remove(successor))
                throw new System.ArgumentException($"Cannot remove {successor} as it is not a successor.");
            successor.predecessors.Remove(this);
            if (successor.predecessors.Count == 0)
                successor.Dominator = null;
            else
                successor.Dominator.EstablishDominance();
        }

        /// <summary>
        /// Establishes the dominance tree. This must be called on the
        /// root block, or the root of the branch that needs
        /// re-establishment.
        /// </summary>
        public void EstablishDominance()
        {
            // Compute reverse postorder
            var postorder = new List<BasicBlock>();
            var visited = new HashSet<BasicBlock>();
            DepthFirstSearch(this, visited, postorder);
            
            postorder.Reverse();

            // Map block to index
            Dictionary<BasicBlock, int> index = new Dictionary<BasicBlock, int>();
            for (int i = 0; i < postorder.Count; i++)
                index[postorder[i]] = i;

            // Initialize
            postorder.Remove(this);
            bool changed = true;
            while (changed)
            {
                changed = false;

                foreach (BasicBlock block in postorder)
                {
                    // Pick first predecessor with defined dominator
                    BasicBlock newIdom = block.predecessors.Where(p => p != block).FirstOrDefault
                        (p => p == this || p.Dominator != null);

                    if (newIdom == null)
                        continue;

                    foreach (BasicBlock predecessor in block.predecessors)
                    {
                        if (predecessor == newIdom)
                            continue;

                        if (predecessor.Dominator != null)
                            newIdom = Intersect(predecessor, newIdom, index);
                    }

                    if (block.Dominator != newIdom)
                    {
                        block.Dominator = newIdom;
                        changed = true;
                    }
                }
            }
        }
        private static BasicBlock Intersect(BasicBlock b1, BasicBlock b2, Dictionary<BasicBlock, int> index)
        {
            while (b1 != b2)
            {
                while (index[b1] > index[b2])
                    b1 = b1.Dominator;

                while (index[b2] > index[b1])
                    b2 = b2.Dominator;
            }

            return b1;
        }
        private static void DepthFirstSearch(BasicBlock block, HashSet<BasicBlock> visited, List<BasicBlock> postorder)
        {
            if (!visited.Add(block))
                return;

            foreach (BasicBlock successor in block.successors)
                DepthFirstSearch(successor, visited, postorder);

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

        public void StoreLocalVariable(SSAVariable variable)
        {
            SingleStaticAssignment.OverwriteVariable(variablesWritten, variable);
            Scope.StoreLocalVariable(variable.Parent);
        }
        public void StoreGlobalVariable(SSAVariable variable)
        {
            SingleStaticAssignment.OverwriteVariable(variablesWritten, variable);
            Scope.StoreGlobalVariable(variable.Parent);
        }
        public void StoreVariable(SSAVariable variable)
        {
            SingleStaticAssignment.OverwriteVariable(variablesWritten, variable);
            Scope.StoreVariable(variable.Parent);
        }
        public bool TryStoreVariable(SSAVariable variable)
        {
            bool result = Scope.TryStoreVariable(variable.Parent);
            if (result)
                SingleStaticAssignment.OverwriteVariable(variablesWritten, variable);
            return result;
        }
        
        public IRVariableBase PushVariable(string name, Opcode opcode)
        {
            IRVariableBase result = Scope.GetVariableNamed(name);
            if (result == null)
            {
                IRScope globalScope = Scope.GetGlobalScope();
                result = new IRVariable(name, globalScope, opcode);
                Scope.StoreGlobalVariable(result);
            }
            variablesRead.Add(result);
            return result;
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
        public void SetStackState(Stack<IRValue> stack)
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
                foreach (Opcode opcode in instruction.EmitOpcode())
                {
                    if (first)
                    {
                        opcode.Label = Label;
                        first = false;
                    }
                    yield return opcode;
                }
            }
            foreach (IRValue stackValue in exitStackState)
            {
                foreach (Opcode opcode in stackValue.EmitPush())
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
                foreach (Opcode opcode in Instructions.Last().EmitOpcode())
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
}
