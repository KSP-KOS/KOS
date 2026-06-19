using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Compilation.IR;

namespace kOS.Safe.Compilation.Optimization.Passes
{
    public class LoopInvariantCodeMotion : IHolisticOptimizationPass
    {
        public OptimizationLevel OptimizationLevel => OptimizationLevel.Balanced;
        public short SortIndex => 2100;

        public void ApplyPass(IRCodePart codePart)
        {
            foreach (IRCodePart.IRFunction function in codePart.Functions)
                foreach (IRCodePart.IRFunction.IRFunctionFragment fragment in function.Fragments)
                    ApplyPass(fragment.FunctionCode[0]);
            foreach (IRCodePart.IRTrigger trigger in codePart.Triggers)
                ApplyPass(trigger.Code[0]);
            ApplyPass(codePart.MainCode[0]);
        }

        private static void ApplyPass(BasicBlock root)
        {
            IEnumerable<BasicBlock> GetEdges(BasicBlock block)
                => block.Successors.Where(BlockOrdering.AllBlocksPredicate);

            List<BasicBlock> reversePostOrder = BasicBlock.GetReversePostOrder(root, GetEdges);
            Stack<BasicBlock> regionExits = new Stack<BasicBlock>();
            regionExits.Push(reversePostOrder[reversePostOrder.Count - 1]);

            List<BlockOrdering.LoopData> loopData = LoopConditionalRelocation.FindLoops(root, regionExits);
            // Loops are found in reverse post-order, so the list is
            // reversed to work from inside out for nested loops.
            loopData.Reverse();

            foreach (BlockOrdering.LoopData loop in loopData)
            {
                RelocateInstructions(loop);
            }
        }

        private static void RelocateInstructions(BlockOrdering.LoopData loop)
        {
            // Things that are not able to be relocated:
            //  - Calls to non-inert functions.
            //  - Suffix or index sets or gets.
            //  - InterimVariableReference operands
            //  - InterimUnresolvedReference operands (maybe?)
            //  - InterimResolvedReference operands if that variable definition
            //      does not come into the loop body, unless it comes from an
            //      assignment that is relocated.
            //  - IRParameter operands that are not resolvable.
            //  - Anything that happens inside a conditional.
            //      (Another pass can deal with restructuring conditionals)
            HashSet<SSADefinition> allowableReferences = new HashSet<SSADefinition>(loop.body.IncomingVariableDefinitions.Values);
            BasicBlock bodyEnd = loop.body;
            while (bodyEnd.PostDominator?.Dominator == bodyEnd && bodyEnd.PostDominator != loop.exit)
                bodyEnd = bodyEnd.PostDominator;
            allowableReferences.RemoveWhere(ssaDef => ssaDef is PhiVariable phi && phi.Node.PossibleValues.ContainsKey(bodyEnd));
            bool IsOperandForbidden_(IInterimOperand operand)
                => IsOperandForbidden(operand, allowableReferences);

            List<IRInstruction> relocatedInstructions =
                loop.body.Dominator?.PostDominator == loop.body ? loop.body.Dominator.Instructions :
                new List<IRInstruction>();
            int insertionIndex = GetInsertionIndex(relocatedInstructions);
            int originalCount = relocatedInstructions.Count;

            BasicBlock block = loop.body;
            while (BlockOrdering.IsInsideRegion(block, loop.exit))
            {
                // Walk each instruction
                for (int i = 0; i < block.Instructions.Count; i++)
                {
                    IRInstruction instruction = block.Instructions[i];
                    // Check that nothing invalidating occurs:
                    bool invalid = false;
                    if (instruction is IRBranch ||
                        instruction is IRNoStackInstruction)
                        continue;
                    foreach (IRInstruction inst in instruction.DepthFirst())
                    {
                        if (inst is IActionInstruction action &&
                            !action.IsInert)
                        {
                            invalid = true;
                            break;
                        }
                        if (inst is IRSuffixGet || inst is IRIndexGet)
                        {
                            invalid = true;
                            break;
                        }
                        if (inst is IOperandInstructionBase operandInstruction &&
                            operandInstruction.AnyOperand(IsOperandForbidden_))
                        {
                            invalid = true;
                            break;
                        }
                    }
                    if (invalid)
                        continue;

                    // If it reaches this point, the instruction can
                    // be moved outside the loop.
                    relocatedInstructions.Insert(insertionIndex, instruction);
                    block.Instructions.RemoveAt(i);
                    i--;
                    if (instruction is IRAssign assignment)
                        allowableReferences.Add(assignment.Target);
                }
                // Walking by post-dominator means that this will skip
                // any conditional branches inside the loop.
                block = block.PostDominator;
            }

            if (loop.body.Dominator?.PostDominator != loop.body &&
                originalCount != relocatedInstructions.Count)
            {
                BasicBlock prefaceBlock = BasicBlock.InsertBlockBetween(loop.body.Dominator, loop.body);
                prefaceBlock.Instructions.AddRange(relocatedInstructions);
            }
        }

        private static bool IsOperandForbidden(IInterimOperand operand, HashSet<SSADefinition> allowableReferences)
        {
            switch (operand)
            {
                case InterimVariableReference _:
                    return true;
                case InterimUnresolvedReference _:
                    return true;
                case InterimResolvedReference resolvedReference:
                    return !allowableReferences.Contains(resolvedReference.Reference);
                case IRParameter parameter:
                    return !parameter.IsResolvable;
                default:
                    return false;
            }
        }

        private static int GetInsertionIndex(List<IRInstruction> instructionList)
        {
            int insertionIndex = instructionList.Count;
            while (insertionIndex > 0 &&
                (instructionList[insertionIndex - 1] is IRBranch ||
                instructionList[insertionIndex - 1] is IRJump ||
                instructionList[insertionIndex - 1] is IRJumpStack))
                insertionIndex--;
            return insertionIndex;
        }
    }
}
