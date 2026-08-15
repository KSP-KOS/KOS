using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Compilation.IR;

namespace kOS.Safe.Compilation.Optimization.Passes
{
    public class RedundantAssignmentElimination : IOptimizationPass<ICodeComponent>, ILinkedOptimizationPass
    {
        public OptimizationLevel OptimizationLevel => OptimizationLevel.Balanced;
        public short SortIndex => 4100;
        public Optimizer Optimizer { get; set; }

        public void ApplyPass(IEnumerable<ICodeComponent> code)
        {
            foreach (ICodeComponent codeComponent in code)
            {
                Queue<(IRAssign, IOperandInstructionBase)> singleUseVariables = new Queue<(IRAssign, IOperandInstructionBase)>(GetSingleUseAssignments(codeComponent));
                Dictionary<IOperandInstructionBase, IOperandInstructionBase> replacementDestinations = new Dictionary<IOperandInstructionBase, IOperandInstructionBase>();
                List<(IRAssign, IOperandInstructionBase)> requeue = new List<(IRAssign, IOperandInstructionBase)>();

                bool changed = true;
                while (changed)
                {
                    changed = false;
                    foreach ((IRAssign, IOperandInstructionBase) item in requeue)
                        singleUseVariables.Enqueue(item);
                    requeue.Clear();
                    while (singleUseVariables.Count > 0)
                    {
                        (IRAssign definition, IOperandInstructionBase use) = singleUseVariables.Dequeue();
                        while (use != null &&
                            replacementDestinations.TryGetValue(use, out IOperandInstructionBase newUse))
                            use = newUse;

                        if (!DefinitionCanBeRelocated(definition, use, out bool mustRelocateArgB))
                        {
                            requeue.Add((definition, use));
                            continue;
                        }

                        if (RelocateOperand(definition, use, mustRelocateArgB))
                            replacementDestinations[definition] = use;
                        changed = true;
                    }
                }
            }
        }

        private bool DefinitionCanBeRelocated(IRAssign definition, IOperandInstructionBase use, out bool nextInstructionIsArgB)
        {
            nextInstructionIsArgB = false;

            if (!definition.IsInert)
                return false;

            if (SingleStaticAssignment.DefinitionIsProtected(definition.Target))
                return false;

            // Double-use items should not be relocated.
            bool? isUsedTwice = null;
            foreach (IOperandInstructionBase instruction in use.DepthFirst())
            {
                instruction.ForEachOperand(op =>
                {
                    if (isUsedTwice == true)
                        return;
                    if (op is InterimResolvedReference resolvedReference &&
                    resolvedReference.Reference.Equals(definition.Target))
                    {
                        if (isUsedTwice == null)
                            isUsedTwice = false;
                        else if (isUsedTwice == false)
                            isUsedTwice = true;
                    }
                });
                if (isUsedTwice == true)
                    return false;
            }

            // Primitives can be relocated indefinitely unless they have a non-inert operand,
            // in which case they can be relocated as far as the next non-inert operand or operation.
            // Non-primitives can be relocated only until the next non-inert operation.

            bool isPrimitive = typeof(Encapsulation.PrimitiveStructure).IsAssignableFrom(definition.Value.Type);
            bool noFurtherThanNextNonInert = !isPrimitive || IsNonInert(definition.Value);

            // These non-inert checks can really only be assured through single-predecessor chains from the use site...
            // Unless all possible predecessors between them have no non-inert objects.
            if (noFurtherThanNextNonInert &&
               NonInertObjectBetween(definition, (IRInstruction)use))
                return false;

            // Where a parameter is an operand, the definition can be relocated only where it is used as
            // the first operand (or second if IRBinary.IsCommutative) of an Instruction, and no further than
            // the next parameter reference.
            // Make sure to move the argb instruction to after the relocated use (where applicable).
            // This cannot move to after a return statement, so interrupt the relocation in that instance.

            bool containsParameter = IsOrHasNested(definition.Value, op => op is IRParameter);
            if (containsParameter)
            {
                // Optional parameters cannot be relocated.
                // At least not without a lot of work on how argument tests are done.
                if (IsOrHasNested(definition.Value, op =>
                    op is IRParameter parameter &&
                    parameter.StackTransferObject is StackTransferPhi phi &&
                    phi.PossibleValues.Keys.Count(b => b.IsExecutable) > 1))
                    return false;
                nextInstructionIsArgB = NextInstructionIsArgB(definition);
                if (use == null ||
                    use is IRReturn &&
                    nextInstructionIsArgB)
                    return false;
                if (ParameterReferenceBetween(definition, use))
                    return false;
                if (!UseIsFirstOperand(definition.Target, use))
                    return false;
            }

            // Where a variable reference is an operand, the same definition must be reachable at the relocated site.
            if (!AllVariableReferencesReachable(definition, (IRInstruction)use))
                return false;

            return true;
        }

        private bool NonInertObjectBetween(IRInstruction first, IRInstruction second)
            => OccursBetween(first, second, IsNonInert);
        private static bool OccursBetween(IRInstruction first, IRInstruction second, Predicate<IRInstruction> predicate)
        {
            if (second == null)
                return false;
            int firstIndex = first.Block.Instructions.IndexOf(first) + 1;

            List<IRInstruction> instructions = second.Block.Instructions;
            int secondIndex = second.Block.Instructions.IndexOf(second) - 1;

            if (second.Block == first.Block)
                return OccursBetween(instructions, secondIndex, firstIndex, predicate);

            if (OccursBetween(instructions, secondIndex, 0, predicate) ||
                OccursBetween(first.Block.Instructions, first.Block.Instructions.Count - 1, firstIndex, predicate))
                return true;

            HashSet<BasicBlock> visited = new HashSet<BasicBlock>()
            {
                first.Block,
                second.Block
            };
            Queue<BasicBlock> queue = new Queue<BasicBlock>();
            foreach (BasicBlock predecessor in second.Block.Predecessors)
                if (visited.Add(predecessor))
                    queue.Enqueue(predecessor);
            while (queue.Count > 0)
            {
                BasicBlock block = queue.Dequeue();
                instructions = block.Instructions;
                if (OccursBetween(block.Instructions, block.Instructions.Count - 1, 0, predicate))
                    return true;
                foreach (BasicBlock predecessor in block.Predecessors)
                    if (visited.Add(predecessor))
                        queue.Enqueue(predecessor);
            }

            return false;
        }

        private static bool OccursBetween(List<IRInstruction> instructions, int lastIndex, int firstIndex, Predicate<IRInstruction> predicate)
        {
            for (int i = lastIndex; i >= firstIndex; i--)
            {
                if (predicate(instructions[i]))
                    return true;
            }
            return false;
        }

        private bool IsNonInert(object obj)
        {
            // Print/PrintAt aren't really non-inert,
            // they just shouldn't be optimized away.
            if (obj is IRCall call &&
                !Optimizer.AllowClobberBuiltins &&
                (call.Function.Equals("print()", StringComparison.OrdinalIgnoreCase) ||
                 call.Function.Equals("printat()", StringComparison.OrdinalIgnoreCase)))
                return false;
            if (obj is IRNoStackInstruction noStackInstruction &&
                noStackInstruction.Operation is OpcodeArgBottom)
                return false;
            if (obj is IRPushStack)
                return false;
            if (obj is IActionInstruction action &&
                !action.IsInert)
                return true;
            if (obj is IOperandInstructionBase operandInstruction &&
                operandInstruction.AnyOperand(IsNonInert))
                return true;
            return false;
        }

        private static bool NextInstructionIsArgB(IRInstruction instruction)
        {
            List<IRInstruction> instructions = instruction.Block.Instructions;
            int index = instructions.IndexOf(instruction) + 1;
            if (index >= instructions.Count)
                return false;
            IRInstruction next = instructions[index];
            return next is IRNoStackInstruction noStackInstruction &&
                noStackInstruction.Operation is OpcodeArgBottom;
        }

        private static bool ParameterReferenceBetween(IRAssign first, IOperandInstructionBase second)
        {
            if (OccursBetween(first, (IRInstruction)second, ReferencesParameter))
                return true;

            bool foundTarget = false;
            foreach (IOperandInstructionBase instruction in second.DepthFirst())
            {
                if (instruction.AnyOperand(op =>
                {
                    if (op is InterimResolvedReference resolvedReference &&
                    resolvedReference.Reference.Equals(first.Target))
                        foundTarget = true;

                    if (foundTarget)
                        return false;
                    return op is IRParameter;
                }
                ))
                    return true;
            }

            return false;
        }
        private static bool ReferencesParameter(object obj)
            => obj is IRParameter ||
                obj is IOperandInstructionBase operandInstruction &&
                operandInstruction.AnyOperand(ReferencesParameter);

        private static bool UseIsFirstOperand(SSADefinition definition, IOperandInstructionBase instruction)
        {
            instruction = instruction.DepthFirst().First();
            // Calls require an arg marker beforehand.
            if (instruction is IRCall)
                return false;

            if (instruction is ISingleOperandInstruction singleOperandInstruction)
                return singleOperandInstruction.Operand is InterimResolvedReference resolvedReference &&
                    resolvedReference.Reference.Equals(definition);
            if (instruction is IMultipleOperandInstruction multipleOperandInstruction)
            {
                return (multipleOperandInstruction.Operands.First() is InterimResolvedReference resolvedReference &&
                    resolvedReference.Reference.Equals(definition)) ||
                    multipleOperandInstruction is IRBinaryOp binaryOp &&
                    binaryOp.IsCommutative &&
                    binaryOp.Operands.Skip(1).First() is InterimResolvedReference secondReference &&
                    secondReference.Reference.Equals(definition);
            }
            throw new NotImplementedException();
        }

        private static bool AllVariableReferencesReachable(IRAssign definition, IRInstruction instruction)
        {
            if (instruction == null)
                return true;
            HashSet<SSADefinition> references = new HashSet<SSADefinition>();
            bool invalid = false;
            foreach (IOperandInstructionBase operandInstruction in ((IOperandInstructionBase)definition).DepthFirst())
            {
                operandInstruction.ForEachOperand(op =>
                {
                    if (op is InterimResolvedReference resolvedReference)
                        references.Add(resolvedReference.Reference);
                    else if (op is IInterimVariableReference)
                        invalid = true;
                });
                if (invalid)
                    return false;
            }
            SingleStaticAssignment.ApplyUses(instruction.Block);
            HashSet<IInterimVariableReference> reachableDefinitions = instruction.Block.CodePart.ReachableVariables[instruction];

            foreach (SSADefinition reference in references)
            {
                IInterimVariableReference reachableReference = reachableDefinitions.FirstOrDefault(v => v.Name.Equals(reference.Name, StringComparison.OrdinalIgnoreCase));
                if (reachableReference == null ||
                    !(reachableReference is InterimResolvedReference resolvedReference) ||
                    resolvedReference.Reference.Equals(definition.Target))
                    return false;
            }

            return true;
        }

        private static bool RelocateOperand(IRAssign definition, IOperandInstructionBase use, bool relocateArgB)
        {
            bool replaced = use == null;
            foreach (IOperandInstructionBase instruction in use.DepthFirst())
            {
                instruction.MutateEachOperand(op =>
                {
                    if (op is InterimResolvedReference resolvedReference &&
                    resolvedReference.Reference.Equals(definition.Target))
                    {
                        replaced = true;
                        return definition.Value;
                    }
                    return op;
                });
                if (replaced)
                    break;
            }

            if (replaced)
            {
                if (relocateArgB)
                {
                    IRInstruction argB = definition.Block.Instructions[definition.Block.Instructions.IndexOf(definition) + 1];
                    IRInstruction useInstruction = (IRInstruction)use;
                    List<IRInstruction> destinationInstructions = useInstruction.Block.Instructions;
                    argB.Block.Instructions.Remove(argB);
                    int argBIndex = destinationInstructions.IndexOf(useInstruction) + 1;
                    if (argBIndex >= destinationInstructions.Count)
                        destinationInstructions.Add(argB);
                    else
                        destinationInstructions.Insert(argBIndex, argB);
                    argB.Block = useInstruction.Block;
                }
                SingleStaticAssignment.RemoveAssignment(definition, overrideParameterProtection: true);
            }

            return replaced;
        }

        private static IEnumerable<(IRAssign, IOperandInstructionBase)> GetSingleUseAssignments(ICodeComponent codeComponent)
        {
            List<(IRAssign, IOperandInstructionBase)> singleUseVariables =
                new List<(IRAssign, IOperandInstructionBase)>();

            Dictionary<SSADefinition, HashSet<IOperandInstructionBase>> varUses =
                SCCPWithTypePropagation.MapUsesAndPropagateTypes(codeComponent.RootBlock);

            foreach (SSADefinition definition in varUses.Keys)
            {
                if (!(definition is SSASetDefinition setDefinition) ||
                    !StringUtil.IsValidIdentifier(definition.Name.Substring(1)))
                    continue;
                HashSet<IOperandInstructionBase> uses = varUses[definition];
                if (uses.Count == 0)
                    singleUseVariables.Add((setDefinition.DefinedAt, null));
                if (uses.Count > 2)
                    continue;
                IOperandInstructionBase use = uses.FirstOrDefault(u => u is IActionInstruction && !(u is IRCall));
                if (use == null ||
                    use is PhiNode)
                    continue;
                if (uses.Count == 2)
                {
                    IOperandInstructionBase operand = uses.FirstOrDefault(u => (!(u is IActionInstruction)) || u is IRCall);
                    if (operand == null ||
                        !use.AnyOperand(op => IsOrHasNested(op, operand)))
                        continue;
                }
                singleUseVariables.Add((setDefinition.DefinedAt, use));
            }
            return singleUseVariables;
        }

        private static bool IsOrHasNested(IInterimOperand operand, Predicate<IInterimOperand> predicate)
        {
            if (predicate(operand))
                return true;
            if (operand is IOperandInstructionBase operandInstruction)
                return operandInstruction.AnyOperand(op => IsOrHasNested(op, predicate));
            return false;
        }

        private static bool IsOrHasNested(IInterimOperand operand, IOperandInstructionBase target)
        {
            if (operand == target)
                return true;
            if (operand is IOperandInstructionBase operandInstruction)
                return operandInstruction.AnyOperand(op => IsOrHasNested(op, target));
            return false;
        }
    }
}
