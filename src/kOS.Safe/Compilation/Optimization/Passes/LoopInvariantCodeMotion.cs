using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Compilation.IR;

namespace kOS.Safe.Compilation.Optimization.Passes
{
    public class LoopInvariantCodeMotion : IOptimizationPass<ICodeComponent>
    {
        public OptimizationLevel OptimizationLevel => OptimizationLevel.Balanced;
        public short SortIndex => 2100;

        public void ApplyPass(IEnumerable<ICodeComponent> codeComponents)
        {
            foreach (ICodeComponent component in codeComponents)
                ApplyPass(component.RootBlock);
        }

        private static void ApplyPass(BasicBlock root)
        {
            Stack<BasicBlock> regionExits = new Stack<BasicBlock>();
            regionExits.Push(root.CodeComponent.TerminalBlock);

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

            List<IRInstruction> relocatedInstructions =
                loop.body.Dominator?.PostDominator == loop.body ? loop.body.Dominator.Instructions :
                new List<IRInstruction>();
            int insertionIndex = relocatedInstructions.Count;
            int originalCount = relocatedInstructions.Count;

            BasicBlock block = loop.body;
            while (BlockOrdering.IsInsideRegion(block, loop.exit))
            {
                // Walk each instruction
                for (int i = 0; i < block.Instructions.Count; i++)
                {
                    IRInstruction instruction = block.Instructions[i];
                    // Check that nothing invalidating occurs:
                    if (instruction is IRNoStackInstruction)
                        continue;
                    if (instruction is IActionInstruction action &&
                        !action.IsInert)
                        continue;

                    if (instruction is IOperandInstructionBase operandInstruction)
                    {
                        if (operandInstruction.AnyOperand(op => IsOperandForbidden(op, allowableReferences)))
                        {
                            Queue<(IInterimOperand, IOperandInstructionBase)> operandsToCheck = new Queue<(IInterimOperand, IOperandInstructionBase)>();
                            void EnqueueOperands(IOperandInstructionBase operand)
                            {
                                if (operand is ISingleOperandInstruction singleOperandInstruction)
                                    operandsToCheck.Enqueue((singleOperandInstruction.Operand, operandInstruction));
                                else if (operand is IMultipleOperandInstruction multipleOperandInstruction)
                                    foreach (IInterimOperand op in multipleOperandInstruction.Operands)
                                        operandsToCheck.Enqueue((op, operandInstruction));
                            }
                            EnqueueOperands(operandInstruction);

                            while (operandsToCheck.Count > 0)
                            {
                                (IInterimOperand op, IOperandInstructionBase parent) = operandsToCheck.Dequeue();

                                // Consider sub-expressions in a loop body where the
                                // instruction itself could not be relocated but where a
                                // portion of it is worth storing before the loop.
                                // Assuming the loop body is executed at least twice,
                                // Any expression of >=3 opcodes is worth relocating.
                                // If we assume the loop body is executed three times,
                                // expressions of 2 opcodes become worth storing.
                                // Even at two loops, a 2-opcode expression only loses
                                // a single opcode of efficiency if stored.

                                if (!(op is IResultingInstruction resultingInstruction) ||
                                    resultingInstruction.OpcodeCount < 3)
                                    continue;

                                if (!IsOperandForbidden(op, allowableReferences))
                                {
                                    // This operand can be moved outside the
                                    // loop as a compiler temporary variable
                                    string identifier = CommonExpressionElimination.TemporaryVariableIssuer.GetNewIdentifier();
                                    IRAssign relocatedAssignment =
                                        new IRAssign(block, new OpcodeStoreLocal(identifier)
                                        {
                                            SourceLine = op.SourceLine,
                                            SourceColumn = op.SourceColumn
                                        }, op);
                                    InterimResolvedReference reference = new InterimResolvedReference(relocatedAssignment.Target, op.SourceLine, op.SourceColumn);
                                    parent.MutateEachOperand(o => o == op ? reference : o);
                                    relocatedInstructions.Insert(insertionIndex, relocatedAssignment);
                                }
                                else if (op is IOperandInstructionBase operation)
                                {
                                    // Otherwise check to see if any nested
                                    // operands can be relocated by adding
                                    // them to the queue.
                                    EnqueueOperands(operation);
                                }
                            }
                            continue;
                        }
                    }

                    // If it reaches this point, the whole instruction
                    // can be moved outside the loop.
                    relocatedInstructions.Insert(insertionIndex, instruction);
                    block.Instructions.RemoveAt(i);
                    i--;
                    if (instruction is IRAssign assignment)
                        allowableReferences.Add(assignment.Target);
                }
                // Walking by post-dominator means that this will skip
                // any conditional branches inside the loop.
                if (block == bodyEnd)
                    break;
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
            if (operand is IActionInstruction action &&
                !action.IsInert)
                return true;

            if (operand is IRSuffixGet || operand is IRIndexGet)
                return true;

            switch (operand)
            {
                case InterimVariableReference _:
                    return true;
                case InterimUnresolvedReference _:
                    return true;
                case InterimResolvedReference resolvedReference:
                    if (!allowableReferences.Contains(resolvedReference.Reference))
                        return true;
                    break;
                case IRParameter parameter:
                    if (!parameter.IsResolvable)
                        return true;
                    break;
            }

            if (operand is IOperandInstructionBase operandInstruction &&
                operandInstruction.AnyOperand(op => IsOperandForbidden(op, allowableReferences)))
                return true;

            return false;
        }
    }
}
