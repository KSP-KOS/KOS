using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Compilation.IR;

namespace kOS.Safe.Compilation.Optimization.Passes
{
    public class LoopConditionalRelocation : IHolisticOptimizationPass
    {
        public OptimizationLevel OptimizationLevel => OptimizationLevel.Balanced;

        public short SortIndex => -10000;

        public void ApplyPass(IRCodePart codePart)
        {
            foreach (IRCodePart.IRFunction function in codePart.Functions)
                foreach (IRCodePart.IRFunction.IRFunctionFragment fragment in function.Fragments)
                    ApplyPass(fragment.FunctionCode[0]);
            foreach (IRCodePart.IRTrigger trigger in codePart.Triggers)
                ApplyPass(trigger.Code[0]);
            ApplyPass(codePart.MainCode[0]);
        }

        private static IEnumerable<BasicBlock> GetEdges(BasicBlock block)
            => block.Successors.Where(BlockOrdering.AllBlocksPredicate);

        private static void ApplyPass(BasicBlock root)
        {
            List<BasicBlock> reversePostOrder = BasicBlock.GetReversePostOrder(root, GetEdges);
            Stack<BasicBlock> regionExits = new Stack<BasicBlock>();
            regionExits.Push(reversePostOrder[reversePostOrder.Count - 1]);

            List<BlockOrdering.LoopData> loopData = FindLoops(root, regionExits);

            foreach (BlockOrdering.LoopData loop in loopData)
            {
                RelocateConditional(loop);
            }
        }

        private static void RelocateConditional(BlockOrdering.LoopData loopData)
        {
            BasicBlock header = loopData.header;
            BasicBlock block = loopData.body;
            while (block.PostDominator?.Dominator == block)
                block = block.PostDominator;

            IRBranch newBranch = (IRBranch)header.Instructions[header.Instructions.Count - 1];
            newBranch = newBranch.Clone(block);
            newBranch.PreferFalse = !newBranch.PreferFalse;

            block.Instructions[block.Instructions.Count - 1] = newBranch;
            block.AddSuccessor(newBranch.True);
            block.AddSuccessor(newBranch.False);
            block.RemoveSuccessor(header);
        }

        public static List<BlockOrdering.LoopData> FindLoops(BasicBlock root, Stack<BasicBlock> regionExits)
        {
            List<BlockOrdering.LoopData> loopData = new List<BlockOrdering.LoopData>();
            FindLoops(root, regionExits, loopData, false);
            return loopData;
        }
        private static void FindLoops(BasicBlock root, Stack<BasicBlock> regionExits, List<BlockOrdering.LoopData> loops, bool fromLoop)
        {
            BasicBlock block = root;
            while (block != null && block != regionExits.Peek())
            {
                // Identify loops first because loop branches are subsets of branches.
                // If root == loopData.body it's because this was just called recursively
                // below. This can be treated as not a loop since it is already identified
                // as a loop body.
                if (BlockOrdering.IdentifyLoop(block, regionExits.Peek(), out BlockOrdering.LoopData loopData))
                {
                    if (!fromLoop || root != loopData.body)
                        loops.Add(loopData);
                    if (root != loopData.body)
                    {
                        regionExits.Push(loopData.exit);
                        FindLoops(loopData.body, regionExits, loops, true);
                        regionExits.Pop();
                    }
                    block = loopData.exit;
                }
                // If branchData.elseBlock == root, that's because this is the branch
                // instruction at the end of a loop body.
                else if (BlockOrdering.IdentifyBranch(block, regionExits.Peek(), out BlockOrdering.BranchData branchData) &&
                    branchData.elseBlock != root && branchData.ifBlock != root)
                {
                    regionExits.Push(branchData.exit ?? regionExits.Peek());
                    FindLoops(branchData.ifBlock, regionExits, loops, false);
                    regionExits.Pop();
                    if (branchData.exit == null && branchData.elseBlock != null)
                    {
                        block = branchData.elseBlock;
                    }
                    else
                    {
                        if (branchData.elseBlock != null)
                            FindLoops(branchData.elseBlock, regionExits, loops, false);
                        block = branchData.exit;
                    }
                }
                else if (block.PostDominator?.Dominator == block)
                {
                    block = block.PostDominator;
                }
                else
                    return;
            }
        }
    }
}
