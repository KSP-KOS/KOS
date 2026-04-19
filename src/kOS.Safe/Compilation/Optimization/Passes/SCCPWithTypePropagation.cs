using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Compilation.IR;
using kOS.Safe.Encapsulation;

namespace kOS.Safe.Compilation.Optimization.Passes
{
    public class SCCPWithTypePropagation : ILinkedOptimizationPass
    {
        /// <summary>
        /// Gets the optimization level associated with this pass.
        /// </summary>
        /// <remarks>
        /// Actual propagation only occurs for optimization levels
        /// <see cref="OptimizationLevel.Minimal"/> or above.
        /// </remarks>
        public OptimizationLevel OptimizationLevel => OptimizationLevel.None;

        public short SortIndex => short.MinValue;

        public Optimizer Optimizer { private get; set; }

        public void ApplyPass()
        {
            IRCodePart codePart = Optimizer.Code;

            Dictionary<SSAVariable, HashSet<IOperandInstructionBase>> ssaUses =
                MapUsesAndPropagateTypes(codePart, out HashSet<BasicBlock> executableBlocks);

            if (Optimizer.OptimizationLevel > OptimizationLevel.None)
                PropagateConstants(ssaUses, executableBlocks);
        }

        private static Dictionary<SSAVariable, HashSet<IOperandInstructionBase>> MapUsesAndPropagateTypes(IRCodePart codePart, out HashSet<BasicBlock> executableBlocks)
        {
            Dictionary<SSAVariable, HashSet<IOperandInstructionBase>> variableUses =
                new Dictionary<SSAVariable, HashSet<IOperandInstructionBase>>();
            executableBlocks = new HashSet<BasicBlock>();
            HashSet<BasicBlock> visitedBlocks = new HashSet<BasicBlock>();

            foreach (BasicBlock root in codePart.RootBlocks)
            {
                Queue<BasicBlock> blockQueue = new Queue<BasicBlock>();
                blockQueue.Enqueue(root);
                Queue<IOperandInstructionBase> instructionQueue = new Queue<IOperandInstructionBase>();

                while (blockQueue.Count > 0 || instructionQueue.Count > 0)
                {
                    // Mark a block executable
                    // Visit every expression in that block
                    // Queue that block's successors
                    while (instructionQueue.Count > 0)
                    {
                        IOperandInstructionBase instruction = instructionQueue.Dequeue();
                        if (VisitInstruction(instruction, blockQueue, executableBlocks))
                        {
                            if (instruction is IRAssign assignment &&
                                assignment.Target is SSAVariable ssaVariable)
                                foreach (IOperandInstructionBase use in GetOrCreate(variableUses, ssaVariable))
                                    instructionQueue.Enqueue(use);
                            else if (instruction is PhiVariable phi)
                                foreach (IOperandInstructionBase use in GetOrCreate(variableUses, phi))
                                    instructionQueue.Enqueue(use);
                        }
                    }

                    if (blockQueue.Count > 0)
                    {
                        BasicBlock block = blockQueue.Dequeue();
                        if (!visitedBlocks.Add(block))
                            continue;

                        foreach (PhiVariable phi in block.Phis)
                        {
                            foreach (SSAVariable variable in phi.PossibleValues.Values)
                                GetOrCreate(variableUses, variable).Add(phi);

                            VisitInstruction(phi, blockQueue, executableBlocks);
                        }

                        foreach (IRInstruction instruction in block.Instructions)
                        {
                            foreach (IOperandInstructionBase inst in instruction.DepthFirst().Where(i => i is IOperandInstructionBase).Cast<IOperandInstructionBase>())
                            {
                                inst.ForEachOperand(op =>
                                {
                                    if (op is SSAVariable ssaVariable)
                                    {
                                        GetOrCreate(variableUses, ssaVariable).Add(inst);
                                        if (instruction is IOperandInstructionBase opInst)
                                            variableUses[ssaVariable].Add(opInst);
                                    }
                                });
                                if (inst is IRCall call)
                                {
                                    string functionIdentifier = block.Scope.GetFunctionNameFromVariable(call.Function);
                                    if (functionIdentifier != null)
                                    {
                                        IRCodePart.IRFunction function = codePart.Functions.FirstOrDefault(f => string.Equals(f.Identifier, functionIdentifier, StringComparison.OrdinalIgnoreCase));
                                        if (function != null)
                                        {
                                            foreach (IRVariable externalVar in function.ExternalReads.Union(function.ExternalWrites))
                                            {
                                                if (externalVar is SSAVariable ssaVariable)
                                                    GetOrCreate(variableUses, ssaVariable).Add(call);
                                            }
                                        }
                                    }
                                }
                                VisitInstruction(inst, blockQueue, executableBlocks);
                            }

                            if (instruction is IOperandInstructionBase operandInstruction)
                            {
                                operandInstruction.ForEachOperand(op =>
                                {
                                    if (op is SSAVariable ssaVariable)
                                        GetOrCreate(variableUses, ssaVariable).Add(operandInstruction);
                                });
                                VisitInstruction(operandInstruction, blockQueue, executableBlocks);
                            }
                        }

                        if (block.Successors.Count == 1)
                            blockQueue.Enqueue(block.Successors.First());

                        executableBlocks.Add(block);
                    }
                }
            }

            return variableUses;
        }

        private static bool VisitInstruction(IOperandInstructionBase instruction, Queue<BasicBlock> blockQueue, HashSet<BasicBlock> executableBlocks)
        {
            if (instruction is IRAssign assignment)
            {
                bool result = assignment.Target.ValueType != assignment.Value.ValueType;
                assignment.Target.ValueType = assignment.Value.ValueType;
                return result;
            }
            else if (instruction is PhiVariable phi)
            {
                List<SSAVariable> possibleValues = new List<SSAVariable>(
                                phi.PossibleValues.Where(kvp => executableBlocks.Contains(kvp.Key)).Select(kvp => kvp.Value));

                Type proposedType = possibleValues[0].ValueType;
                foreach (SSAVariable variable in possibleValues.Skip(1))
                    proposedType = PhiVariable.GetFirstCommonBaseType(proposedType, variable.ValueType);

                bool result = proposedType != phi.ValueType;
                phi.ValueType = proposedType;
                return result;
            }
            else if (instruction is IRBranch branch)
            {
                if (branch.IsInvariant)
                {
                    IRConstant constantCondition = branch.Condition as IRConstant;
                    if (constantCondition == null && branch.Condition is IRTemp temp)
                        constantCondition = ConstantFolding.AttemptReduction(temp.Parent) as IRConstant;

                    if (constantCondition != null && constantCondition.Value is BooleanValue boolean)
                    {
                        blockQueue.Enqueue(boolean ? branch.True : branch.False);
                    }
                    else
                    {
                        blockQueue.Enqueue(branch.True);
                        blockQueue.Enqueue(branch.False);
                    }
                }
                else
                {
                    blockQueue.Enqueue(branch.True);
                    blockQueue.Enqueue(branch.False);
                }
            }

            return false;
        }

        private static TValue GetOrCreate<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey key) where TValue : new()
        {
            if (!dictionary.TryGetValue(key, out TValue value))
                value = dictionary[key] = new TValue();
            return value;
        }

        private static void PropagateConstants(Dictionary<SSAVariable, HashSet<IOperandInstructionBase>> ssaUses, HashSet<BasicBlock> executableBlocks)
        {
            // TODO: Keep track of which uses are replaced and if there are no more uses, remove the assignment.
            // But watch for functions that external read that variable and make sure to retain the assignment before then.
            List<SSAVariable> constantVariables = ssaUses.Keys.Where(v => v.IsInvariant).ToList();
            constantVariables.AddRange(ssaUses.Keys.Where(v => v is PhiVariable p && p.PossibleValues.Keys.Where(b => !executableBlocks.Contains(b)).Skip(1).Any()));
#if DEBUG
            constantVariables.Sort(Comparer<SSAVariable>.Create((a, b) => string.Compare(a.ToString(), b.ToString())));
#endif
            Queue<SSAVariable> queue = new Queue<SSAVariable>(constantVariables.Distinct());
            Dictionary<SSAVariable, IRConstant> replacements = new Dictionary<SSAVariable, IRConstant>();

            while (queue.Count > 0)
            {
                SSAVariable variable = queue.Dequeue();
                if (queue.Any(v =>
                {
                    if (variable is PhiVariable phi && !replacements.ContainsKey(phi))
                        return true;
                    if (ssaUses[v].Contains(variable.AssignedAt))
                        return true;
                    return false;
                }))
                {
                    queue.Enqueue(variable);
                    continue;
                }

                IRConstant replacement;
                if (!(variable is PhiVariable))
                {
                    if (variable.AssignedAt.Value is IRConstant constant)
                        replacement = constant;
                    else
                        replacement = ConstantFolding.AttemptReduction(((IRTemp)variable.AssignedAt.Value).Parent) as IRConstant;
                }
                else
                {
                    replacement = replacements[variable];
                }

                if (replacement != null)
                {
                    foreach (IOperandInstructionBase instruction in ssaUses[variable])
                    {
                        if (instruction is PhiVariable phi)
                        {
                            if (constantVariables.Contains(phi))
                                replacements[phi] = replacement;
                            continue;
                        }
                        // TODO: Make this propagate True
                        if (instruction is IRUnaryOp unaryOp &&
                            unaryOp.Operation is OpcodeExists)
                            continue;
                        instruction.MutateEachOperand(op => op.Equals(variable) ? replacement : op);
                    }
                }
            }
        }

        private static void Propagation(BasicBlock block)
        {
            //ssaVariable.ValueType = assignment.Value.ValueType;

            //if (postCallSSAVariableLinks.TryGetValue(ssaVariable, out SSAVariable postCallLink))
                // MUSTFIX: This does not guarantee that this happens before ssaVariable is accessed!
                // Particularly in the case of recursive or bouncing functions.
                //postCallLink.ValueType = ssaVariable.ValueType;
        }
    }
}
