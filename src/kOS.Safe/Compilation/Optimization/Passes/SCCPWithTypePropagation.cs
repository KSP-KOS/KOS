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

            Dictionary<SSADefinition, HashSet<IOperandInstructionBase>> ssaUses =
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
        private static Dictionary<SSADefinition, HashSet<IOperandInstructionBase>> MapUsesAndPropagateTypes(IRCodePart codePart)
        {
            Dictionary<SSADefinition, HashSet<IOperandInstructionBase>> variableUses =
                new Dictionary<SSADefinition, HashSet<IOperandInstructionBase>>();
            HashSet<BasicBlock> visitedBlocks = new HashSet<BasicBlock>();
            Dictionary<SSADefinition, (Type, bool)> typeAndInvarianceCache = new Dictionary<SSADefinition, (Type, bool)>();

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
                        foreach (PhiNode phi in block.Phis.Values)
                        {
                            foreach (SSADefinition variable in phi.PossibleValues.Values)
                                GetOrCreate(variableUses, variable).Add(phi);

                            VisitInstruction(phi, blockQueue, typeAndInvarianceCache);
                        }

                        // Process instructions.
                        foreach (IRInstruction instruction in block.Instructions)
                        {
                            foreach (IOperandInstructionBase inst in instruction.DepthFirst().Where(i => i is IOperandInstructionBase).Cast<IOperandInstructionBase>())
                            {
                                inst.ForEachOperand(op =>
                                {
                                    if (op is SSADefinition ssaVariable)
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
                                    IRCodePart.IRFunction function = codePart.GetFunction(call);
                                    if (function != null)
                                    {
                                        Dictionary<(string Name, IRScope Scope), SSADefinition> variables = SingleStaticAssignment.ReachableVariables[call];

                                        foreach (string variableName in function.ExternalReads.Union(
                                            function.ExternalWrites))
                                        {
                                            IRScope scope = block.Scope;
                                            while (scope != null)
                                            {
                                                if (variables.TryGetValue((variableName, scope), out SSADefinition definition) &&
                                                    definition.State != SSADefinition.SetState.Unset)
                                                {
                                                    GetOrCreate(variableUses, definition).Add(call);
                                                    if (definition.State == SSADefinition.SetState.Set)
                                                        break;
                                                }
                                                scope = scope.ParentScope;
                                            }
                                        }
                                    }
                                }
                                // Reduce duplication (and the risk of
                                // inadvertently changing the return value).
                                // The else if the next if statement.
                                if (inst != instruction || !(instruction is IOperandInstructionBase))
                                    VisitInstruction(inst, blockQueue, typeAndInvarianceCache);
                            }


                            if (instruction is IOperandInstructionBase operandInstruction)
                            {
                                operandInstruction.ForEachOperand(op =>
                                {
                                    // TODO: Come back to this...
                                    if (op is InterimVariableReference<SSADefinition> ssaRef &&
                                        ssaRef.Reference is SSASetDefinition ssaVariable)
                                        GetOrCreate(variableUses, ssaVariable).Add(operandInstruction);
                                });
                                if (VisitInstruction(operandInstruction, blockQueue, typeAndInvarianceCache) &&
                                    operandInstruction is IRAssign assignment &&
                                    variableUses.TryGetValue(assignment.Target, out HashSet<IOperandInstructionBase> uses))
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
                        if (VisitInstruction(instruction, blockQueue, typeAndInvarianceCache))
                        {
                            if (instruction is IRAssign assignment)
                                foreach (IOperandInstructionBase use in GetOrCreate(variableUses, assignment.Target))
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
            Dictionary<SSADefinition, (Type, bool)> typeAndInvarianceCache)
        {
            if (instruction is IRAssign assignment)
            {
                SSASetDefinition ssaVariable = assignment.Target;
                if (typeAndInvarianceCache.TryGetValue(ssaVariable, out (Type storedType, bool storedInvariance) cached))
                {
                    typeAndInvarianceCache[ssaVariable] = (ssaVariable.Type, cached.storedInvariance &= ssaVariable.IsInvariant);
                    return cached != typeAndInvarianceCache[ssaVariable];
                }
                else
                {
                    typeAndInvarianceCache[ssaVariable] = (ssaVariable.Type, ssaVariable.IsInvariant);
                    return true;
                }
            }
            else if (instruction is PhiVariable phi)
            {
                if (typeAndInvarianceCache.TryGetValue(phi, out (Type storedType, bool storedInvariance) cached))
                {
                    typeAndInvarianceCache[phi] = (phi.Type, cached.storedInvariance &= phi.IsInvariant);
                    return cached != typeAndInvarianceCache[phi];
                }
                else
                {
                    typeAndInvarianceCache[phi] = (phi.Type, phi.IsInvariant);
                    return true;
                }
            }
            else if (instruction is IRBranch branch)
            {
                if (branch.IsInvariant)
                {
                    InterimConstantValue constantCondition = (branch.Condition as IEvaluatableToConstant).Evaluate();
                    bool result = Convert.ToBoolean(constantCondition.Value);

                    blockQueue.Enqueue(result ? branch.True : branch.False);
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
        private static void PropagateConstants(Dictionary<SSADefinition, HashSet<IOperandInstructionBase>> ssaUses)
        {
            // TODO: Keep track of which uses are replaced and if there are no more uses, remove the assignment.
            // But watch for functions that external read that variable and make sure to retain the assignment before then.
            // TODO: Look at branch instructions that employ eq and propagate that constant.
            List<SSADefinition> constantVariables = ssaUses.Keys.Where(
                v => v.State == SSADefinition.SetState.Set &&
                v.IsInvariant &&
                typeof(PrimitiveStructure).IsAssignableFrom(v.Type))
                .ToList();
            
#if DEBUG   // Sort to make debugging variable iterations easier.
            constantVariables.Sort(Comparer<SSADefinition>.Create((a, b) => string.Compare(a.ToString(), b.ToString())));
#endif
            Queue<SSADefinition> queue = new Queue<SSADefinition>(constantVariables);
            Dictionary<SSADefinition, InterimConstantValue> replacements = new Dictionary<SSADefinition, InterimConstantValue>();

            while (queue.Count > 0)
            {
                SSADefinition variable = queue.Dequeue();
                if (queue.Any(v =>
                {
                    if (variable is PhiVariable phi && !replacements.ContainsKey(phi))
                        return true;
                    if (variable is SSASetDefinition setDef && ssaUses[v].Contains(setDef.DefinedAt))
                        return true;
                    return false;
                }))
                {
                    queue.Enqueue(variable);
                    continue;
                }


                InterimConstantValue replacement;
                if (variable is PhiVariable)
                    replacement = replacements[variable];
                else
                    replacement = variable.Evaluate();

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
                        if (instruction is IRUnaryOp unaryOp &&
                            unaryOp.Operation is OpcodeExists)
                            continue;
                        instruction.MutateEachOperand(op => op.Equals(variable) ? replacement : op);
                    }
                }
            }
        }
    }
}
