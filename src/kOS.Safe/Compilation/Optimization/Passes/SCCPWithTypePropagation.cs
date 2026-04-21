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
                MapUsesAndPropagateTypes(codePart);

            if (Optimizer.OptimizationLevel > OptimizationLevel.None)
                PropagateConstants(ssaUses);
        }

        /// <summary>
        /// Applies the sparse conditional side of SCCP, and propagates
        /// type information to Phi variables as blocks become executable.
        /// </summary>
        /// <param name="codePart"></param>
        /// <param name="executableBlocks">
        /// The collection of blocks that are marked as executable.
        /// </param>
        /// <returns>
        /// A dictionary of instructions (or phi variables) that make use
        /// of SSA variables (indirectly or directly).
        /// </returns>
        private static Dictionary<SSAVariable, HashSet<IOperandInstructionBase>> MapUsesAndPropagateTypes(IRCodePart codePart)
        {
            Dictionary<SSAVariable, HashSet<IOperandInstructionBase>> variableUses =
                new Dictionary<SSAVariable, HashSet<IOperandInstructionBase>>();
            HashSet<BasicBlock> visitedBlocks = new HashSet<BasicBlock>();
            Dictionary<SSAVariable, bool> invariance = new Dictionary<SSAVariable, bool>();

            // Apply the algorithm starting from each entry block.
            foreach (BasicBlock root in codePart.RootBlocks)
            {
                // The queue of blocks that are executable
                Queue<BasicBlock> blockQueue = new Queue<BasicBlock>();
                // Queues the entry block
                blockQueue.Enqueue(root);

                // The queue of instructions that need updating
                Queue<IOperandInstructionBase> instructionQueue = new Queue<IOperandInstructionBase>();

                while (blockQueue.Count > 0 || instructionQueue.Count > 0)
                {
                    // Visit every expression and phi in a block
                    // Queue that block's successors
                    while (blockQueue.Count > 0)
                    {
                        BasicBlock block = blockQueue.Dequeue();
                        // Disregard if this block has already been visited
                        // any changes will be captured in the sparse pass below.
                        if (!visitedBlocks.Add(block))
                            continue;

                        // Process Phis first, as if they are instructions.
                        foreach (PhiVariable phi in block.Phis)
                        {
                            foreach (SSAVariable variable in phi.PossibleValues.Values)
                                GetOrCreate(variableUses, variable).Add(phi);

                            VisitInstruction(phi, blockQueue, invariance);
                        }

                        // Process instructions.
                        foreach (IRInstruction instruction in block.Instructions)
                        {
                            foreach (IOperandInstructionBase inst in instruction.DepthFirst().Where(i => i is IOperandInstructionBase).Cast<IOperandInstructionBase>())
                            {
                                inst.ForEachOperand(op =>
                                {
                                    if (op is SSAVariable ssaVariable)
                                    {
                                        // Add this instruction to the list of uses for each operand.
                                        GetOrCreate(variableUses, ssaVariable).Add(inst);

                                        // Also add the base instruction,
                                        // which is the more important reference
                                        // since anything else is a temp result.
                                        if (instruction is IOperandInstructionBase opInst)
                                            variableUses[ssaVariable].Add(opInst);
                                    }
                                });
                                // Calls get to be special to address their external read needs.
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
                                // Reduce duplication (and the risk of
                                // inadvertently changing the return value).
                                // The else if the next if statement.
                                if (inst != instruction || !(instruction is IOperandInstructionBase))
                                    VisitInstruction(inst, blockQueue, invariance);
                            }


                            if (instruction is IOperandInstructionBase operandInstruction)
                            {
                                operandInstruction.ForEachOperand(op =>
                                {
                                    if (op is SSAVariable ssaVariable)
                                        GetOrCreate(variableUses, ssaVariable).Add(operandInstruction);
                                });
                                if (VisitInstruction(operandInstruction, blockQueue, invariance) &&
                                    operandInstruction is IRAssign assignment &&
                                    assignment.Target is SSAVariable ssaTarget &&
                                    variableUses.TryGetValue(ssaTarget, out HashSet<IOperandInstructionBase> uses))
                                    foreach (IOperandInstructionBase use in uses)
                                        instructionQueue.Enqueue(use);
                            }
                        }

                        // Multiple successors are covered in VisitInstruction()
                        // where it covers IRBranch.
                        if (block.Successors.Count == 1)
                            blockQueue.Enqueue(block.Successors.First());

                        block.IsExecutable = true;
                    }

                    // Loop over any instructions (or phis) that need updating.
                    while (instructionQueue.Count > 0)
                    {
                        IOperandInstructionBase instruction = instructionQueue.Dequeue();
                        if (VisitInstruction(instruction, blockQueue, invariance))
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
                }
            }

            return variableUses;
        }

        /// <summary>
        /// Visits an instruction and determines if it has changes
        /// that need to be propagated.
        /// </summary>
        /// <returns><c>true</c> if the instruction needs to propagate changes.</returns>
        private static bool VisitInstruction(IOperandInstructionBase instruction,
            Queue<BasicBlock> blockQueue,
            Dictionary<SSAVariable, bool> invariance)
        {
            if (instruction is IRAssign assignment)
            {
                bool result = assignment.Target.ValueType != assignment.Value.ValueType;
                assignment.Target.ValueType = assignment.Value.ValueType;
                if (assignment.Target is SSAVariable ssaVariable)
                {
                    if (invariance.TryGetValue(ssaVariable, out bool storedInvariance))
                    {
                        invariance[ssaVariable] &= ssaVariable.IsInvariant;
                        result |= storedInvariance != invariance[ssaVariable];
                    }
                    else
                    {
                        invariance[ssaVariable] = ssaVariable.IsInvariant;
                        result = true;
                    }
                }

                return result;
            }
            else if (instruction is PhiVariable phi)
            {
                Type pastType = phi.ValueType;
                phi.RefreshType();

                bool result = pastType != phi.ValueType;

                if (invariance.TryGetValue(phi, out bool storedInvariance))
                {
                    invariance[phi] &= storedInvariance;
                    result |= storedInvariance != invariance[phi];
                }
                else
                {
                    invariance[phi] = phi.IsInvariant;
                    result = true;
                }
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

        /// <summary>
        /// Propagates any constant SSA variables to their uses.
        /// </summary>
        private static void PropagateConstants(Dictionary<SSAVariable, HashSet<IOperandInstructionBase>> ssaUses)
        {
            // TODO: Keep track of which uses are replaced and if there are no more uses, remove the assignment.
            // But watch for functions that external read that variable and make sure to retain the assignment before then.
            List<SSAVariable> constantVariables = ssaUses.Keys.Where(v => v.IsInvariant).ToList();
            

#if DEBUG   // Sort to make debugging variable iterations easier.
            constantVariables.Sort(Comparer<SSAVariable>.Create((a, b) => string.Compare(a.ToString(), b.ToString())));
#endif
            Queue<SSAVariable> queue = new Queue<SSAVariable>(constantVariables);
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
