using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Compilation.Optimization;

namespace kOS.Safe.Compilation.IR
{
    public class BasicBlock
    {
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
        public int StartIndex { get; }
        public int EndIndex { get; }
        public List<IRInstruction> Instructions { get; } = new List<IRInstruction>();
        public IReadOnlyCollection<BasicBlock> Successors => successors;
        public IReadOnlyCollection<BasicBlock> Predecessors => predecessors;
        public HashSet<SSAVariable> VariablesWritten => variablesWritten;
        public IReadOnlyCollection<IRVariableBase> VariablesRead => variablesRead;
        public Dictionary<IRVariable, (SSAVariable phiVar, Dictionary<BasicBlock, SSAVariable> values)> Phis { get; } =
            new Dictionary<IRVariable, (SSAVariable phiVar, Dictionary<BasicBlock, SSAVariable> values)>();
        public HashSet<SSAVariable> IncomingVariableDefinitions { get; set; }
        public HashSet<IRVariable> TriggerPropagationBlacklist { get; } = new HashSet<IRVariable>();
        public string Label => nonSequentialLabel ?? $"@BB#{ID}";
        public int ID { get; }
        public BasicBlock Dominator
        {
            get => dominator;
            protected set
            {
                dominator?.dominates.Remove(this);
                dominator = value;
                dominator?.dominates.Add(this);
            }
        }
        public IReadOnlyCollection<BasicBlock> Dominates => dominates;
        public ExtendedBasicBlock ExtendedBlock { get; set; }
        public IRJump FallthroughJump { get; set; } = null;
#if DEBUG
        internal Opcode[] OriginalOpcodes { get; set; }
        internal Opcode[] GeneratedOpcodes => EmitOpCodes().ToArray();
#endif

        public BasicBlock(int startIndex, int endIndex, int id, string nonSequentialLabel = null)
        {
            StartIndex = startIndex;
            EndIndex = endIndex;
            ID = id;
            this.nonSequentialLabel = nonSequentialLabel;
        }

        public void Add(IRInstruction instruction)
            => Instructions.Add(instruction);

        public void AddSuccessor(BasicBlock successor)
        {
            successors.Add(successor);
            successor.AddPredecessor(this);
        }
        protected void AddPredecessor(BasicBlock predecessor)
        {
            predecessors.Add(predecessor);
        }
        public void RemoveSuccessor(BasicBlock successor)
        {
            if (!successors.Remove(successor))
                throw new System.ArgumentException(nameof(successor));
            successor.predecessors.Remove(this);
            if (successor.predecessors.Count == 0)
                successor.Dominator = null;
            else
                successor.Dominator.EstablishDominance();
        }
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
        public IRScope GetScopeForVariableNamed(string name)
            => Scope.GetScopeForVariableNamed(name);

        public void SetStackState(Stack<IRValue> stack)
        {
            while (stack.Count > 0)
                exitStackState.Push(stack.Pop());
        }

        public override string ToString()
        {
            return $"BasicBlock#{ID}: {StartIndex}-{EndIndex}; {Instructions.Count} Instructions";
        }

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
