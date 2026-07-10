using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Compilation.IR;

namespace kOS.Safe.Compilation.Optimization.Passes
{
    public class TernaryOperandConstruction : IOptimizationPass<ICodeComponent>
    {
        public OptimizationLevel OptimizationLevel => OptimizationLevel.None;
        public short SortIndex => -990;

        public void ApplyPass(IEnumerable<ICodeComponent> code)
        {
            foreach (ICodeComponent codeComponent in code)
            {
                HashSet<BasicBlock> visited = new HashSet<BasicBlock>();
                HashSet<IStackTransferObject> eliminatedStackItems = new HashSet<IStackTransferObject>();
                ApplyPass(codeComponent.RootBlock, visited, eliminatedStackItems);
                foreach (BasicBlock block in codeComponent.Blocks)
                    RemoveUnnecessaryStackItems(block, eliminatedStackItems);
            }
        }

        private static void ApplyPass(BasicBlock block, HashSet<BasicBlock> visited, HashSet<IStackTransferObject> eliminatedItems)
        {
            if (!visited.Add(block))
                return;
            if (block.IncomingStackState.Count > 0)
            {
                // Find IRParameters with an associated StackTransferPhi
                // Check if that can be used to create a TernaryOperand
                //  Exactly two possible values
                //  Both values are:
                //      a PushStack instruction from a block with no other instructions
                //      (other than a closing jump (no branch)), or
                //      a StackTransferPhi that itself could be a TernaryOperand
                // Replace the original branch with a jump to the rejoining block
                // Replace the IRParameter with a TernaryOperand (including nesting).

                foreach (IOperandInstructionBase operandInstruction in block.Instructions.DepthFirst())
                {
                    operandInstruction.MutateEachOperand(op =>
                    {
                        if (!(op is IRParameter parameter))
                            return op;

                        if (parameter.IsResolvable)
                            return parameter.StackTransferObject.Value;

                        if (!ParameterCanBeReplaced(parameter.StackTransferObject))
                            return op;

                        return CreateOperand(parameter.StackTransferObject, block, eliminatedItems);
                    });
                }
            }
            Queue<BasicBlock> queue = new Queue<BasicBlock>(block.Successors);
            foreach (BasicBlock successor in queue)
            {
                if (successor.Dominator == null)
                    continue;
                ApplyPass(successor, visited, eliminatedItems);
            }
        }

        private static bool ParameterCanBeReplaced(IStackTransferObject transferObject)
        {
            if (transferObject == null)
                return false;
            if (transferObject.References.Count + transferObject.Controllers.Count > 1)
                return false;
            switch (transferObject)
            {
                case IRPushStack pushStack:
                    return pushStack.Block != null;
                case StackTransferPhi phi:
                    if (phi.PossibleValues.Count != 2)
                        return false;
                    if (phi.PossibleValues.Keys.Any(b =>
                        b.Instructions.Any(i => !(i is IRPushStack || i is IRJump)) ||
                        b.Successors.Count > 1))
                        return false;
                    return phi.PossibleValues.Values.All(ParameterCanBeReplaced);
                default:
#if DEBUG
                    throw new NotImplementedException();
#else
                    return false;
#endif
            }
        }

        private static IInterimOperand CreateOperand(IStackTransferObject stackTransferObject, BasicBlock block, HashSet<IStackTransferObject> eliminatedObjects)
        {
            eliminatedObjects.Add(stackTransferObject);
            switch (stackTransferObject)
            {
                case IRPushStack pushStack:
                    if (!pushStack.Block.Instructions.Remove(pushStack))
                        throw new InvalidOperationException();
                    return pushStack.Value.Clone(block, true);
                case StackTransferPhi phi:
                    IRBranch branch = GetBranch(phi.PossibleValues.Select(kvp => kvp.Key));
                    IInterimOperand condition = branch.Condition;

                    BasicBlock trueBlock = phi.PossibleValues.Keys.FirstOrDefault(b => b.IsDominatedBy(branch.True, branch.Block));
                    IInterimOperand trueValue = CreateOperand(phi.PossibleValues[trueBlock], block, eliminatedObjects);

                    BasicBlock falseBlock = phi.PossibleValues.Keys.FirstOrDefault(b => b.IsDominatedBy(branch.False, branch.Block));
                    IInterimOperand falseValue = CreateOperand(phi.PossibleValues[falseBlock], block, eliminatedObjects);

                    branch.Block.FallthroughJump = new IRJump(branch.Block, block, branch.SourceLine, branch.SourceColumn);
                    branch.Block.Instructions.RemoveAt(branch.Block.Instructions.Count - 1);
                    branch.Block.AddSuccessor(block);
                    branch.Block.RemoveSuccessor(branch.True);
                    branch.Block.RemoveSuccessor(branch.False);

                    branch.Block.CodeComponent.Blocks.Remove(branch.True);
                    branch.Block.CodeComponent.Blocks.Remove(branch.False);

                    BasicBlock firstSuccessor = trueBlock.Successors.First();
                    trueBlock.RemoveSuccessor(firstSuccessor);
                    if (firstSuccessor.Dominator == trueBlock || firstSuccessor.Dominator == null)
                        branch.Block.CodeComponent.Blocks.Remove(firstSuccessor);
                    firstSuccessor = falseBlock.Successors.First();
                    falseBlock.RemoveSuccessor(firstSuccessor);
                    if (firstSuccessor.Dominator == falseBlock || firstSuccessor.Dominator == null)
                        branch.Block.CodeComponent.Blocks.Remove(firstSuccessor);

                    return new TernaryOperand()
                    {
                        Condition = condition,
                        TrueValue = trueValue,
                        FalseValue = falseValue
                    };
                default:
                    throw new InvalidOperationException();
            }
        }
        public static IRBranch GetBranch(IEnumerable<BasicBlock> blocks)
        {
            List<BasicBlock> blockList = blocks.ToList();
            int maxIndex = blockList.Count - 1;
            HashSet<BasicBlock> visited = new HashSet<BasicBlock>();
            while (true)
            {
                for (int i = maxIndex; i >= 0; i--)
                {
                    if (blockList[i] != null && !visited.Add(blockList[i]))
                        return blockList[i].Instructions.LastOrDefault() as IRBranch;
                    blockList[i] = blockList[i]?.Dominator;
                }
            }
        }

        private static void RemoveUnnecessaryStackItems(BasicBlock block, HashSet<IStackTransferObject> eliminatedStackItems)
        {
            block.IncomingStackState.RemoveAll(eliminatedStackItems.Contains);
            foreach (IOperandInstructionBase operandInstruction in block.Instructions.DepthFirst())
                operandInstruction.ForEachOperand(op =>
                {
                    if (op is IRParameter parameter)
                        parameter.RequiredToBeResolvable.RemoveWhere(p => eliminatedStackItems.Contains(p.StackTransferObject));
                });
        }
    }
}
