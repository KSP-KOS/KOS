using System.Collections.Generic;
using System.Linq;

namespace kOS.Safe.Compilation.IR
{
    public class BasicBlock
    {
        public int StartIndex { get; }
        public int EndIndex { get; }
        public List<IRInstruction> Instructions { get; } = new List<IRInstruction>();
        protected internal readonly HashSet<BasicBlock> predecessors = new HashSet<BasicBlock>();
        protected internal readonly HashSet<BasicBlock> sucessors = new HashSet<BasicBlock>();
        public string Label => $"@BB#{ID}";
        public int ID { get; }
        private readonly Stack<IRValue> exitStackState = new Stack<IRValue>();  // Note that this is reversed from the real stack. Just now we don't reverse it four times.
#if DEBUG
        internal Opcode[] OriginalOpcodes { get; set; }
        internal Opcode[] GeneratedOpcodes => EmitOpCodes().ToArray();
#endif

        public BasicBlock(int startIndex, int endIndex, int id)
        {
            StartIndex = startIndex;
            EndIndex = endIndex;
            ID = id;
        }

        public void Add(IRInstruction instruction)
            => Instructions.Add(instruction);

        public void AddSuccessor(BasicBlock successor)
        {
            sucessors.Add(successor);
            successor.AddPredecessor(this);
        }
        protected void AddPredecessor(BasicBlock predecessor)
        {
            predecessors.Add(predecessor);
        }

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
        }
    }
}
