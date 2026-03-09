using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Compilation.IR;

namespace kOS.Safe.Compilation.Optimization
{
    public class ExtendedBasicBlock
    {
        public List<BasicBlock> Blocks { get; } = new List<BasicBlock>();
        public IEnumerable<ExtendedBasicBlock> Sucessors => sucessors;
        public IEnumerable<ExtendedBasicBlock> Predecessors => predecessors;
        private readonly HashSet<ExtendedBasicBlock> predecessors = new HashSet<ExtendedBasicBlock>();
        private readonly HashSet<ExtendedBasicBlock> sucessors = new HashSet<ExtendedBasicBlock>();
        private ExtendedBasicBlock(BasicBlock startingBlock)
        {
            AddBlock(startingBlock);
            Blocks.Sort(Comparer<BasicBlock>.Create((x, y) => x.ID.CompareTo(y.ID)));
        }
        private void AddBlock(BasicBlock block)
        {
            Blocks.Add(block);
            block.ExtendedBlock = this;
            foreach (BasicBlock successor in block.Successors)
            {
                if (!successor.Predecessors.Skip(1).Any())
                    AddBlock(successor);
                else
                {
                    ExtendedBasicBlock extendedSuccessor = successor.ExtendedBlock ?? new ExtendedBasicBlock(successor);
                    AddSuccessor(extendedSuccessor);
                }
            }
        }

        public static ExtendedBasicBlock CreateExtendedBlockTree(BasicBlock root)
        {
            ExtendedBasicBlock resultBlock = new ExtendedBasicBlock(root);
            return resultBlock;
        }
        public static IEnumerable<ExtendedBasicBlock> DumpTree(ExtendedBasicBlock root)
            => Enumerable.Repeat(root, 1).Concat(root.sucessors.SelectMany(DumpTree));

        public void AddSuccessor(ExtendedBasicBlock successor)
        {
            sucessors.Add(successor);
            successor.AddPredecessor(this);
        }
        protected void AddPredecessor(ExtendedBasicBlock predecessor)
        {
            predecessors.Add(predecessor);
        }

        public override string ToString()
        {
            return $"ExtendedBasicBlock: {string.Join(", ", Blocks.Select(bb => $"#{bb.ID}"))}";
        }
    }
}
