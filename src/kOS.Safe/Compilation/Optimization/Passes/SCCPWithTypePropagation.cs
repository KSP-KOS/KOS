using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Compilation.IR;
using kOS.Safe.Encapsulation;

namespace kOS.Safe.Compilation.Optimization.Passes
{
    public class SCCPWithTypePropagation : IHolisticOptimizationPass, ILinkedOptimizationPass
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

        public void ApplyPass(IRCodePart codePart)
        {
            Dictionary<SSADefinition, HashSet<IOperandInstructionBase>> ssaUses =
                MapUsesAndPropagateTypes(codePart);

            if (Optimizer.OptimizationLevel == OptimizationLevel.None)
                return;

            HashSet<SSADefinition> usedDefinitions = PropagateConstants(ssaUses);

            foreach (BasicBlock block in codePart.Blocks)
                RemoveRedundantAssignments(block, usedDefinitions);
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
                new Dictionary<SSADefinition, HashSet<IOperandInstructionBase>>(SSAReferenceEqualityComparer.Instance);
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
                                    if (op is IInterimVariableReference reference &&
                                        !(op is InterimVariableReference))
                                    {
                                        foreach (SSADefinition variable in GetSSADefinitionsFromReferences(reference))
                                        {
                                            // Add this instruction to the list of uses for each operand.
                                            GetOrCreate(variableUses, variable).Add(inst);

                                            // Also add the base instruction,
                                            // which is the more important reference
                                            // since anything else is a temp result.
                                            if (instruction is IOperandInstructionBase opInst)
                                                GetOrCreate(variableUses, variable).Add(opInst);
                                        }
                                    }
                                });
                                // Calls get to be special to address their external read needs.
                                if (inst is IRCall call)
                                {
                                    IRCodePart.IRFunction function = codePart.GetFunction(call);
                                    if (function != null)
                                    {
                                        HashSet<IInterimVariableReference> variables = SingleStaticAssignment.ReachableVariables[call];
                                        foreach (SSADefinition variable in variables.SelectMany(GetSSADefinitionsFromReferences))
                                        {
                                            GetOrCreate(variableUses, variable).Add(call);
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
                                    if (op is InterimResolvedReference ssaRef)
                                        GetOrCreate(variableUses, ssaRef.Reference).Add(operandInstruction);
                                    if (op is InterimUnresolvedReference unresolvedRef)
                                    {
                                        foreach (SSADefinition ssaDef in GetSSADefinitionsFromReferences(unresolvedRef))
                                            GetOrCreate(variableUses, ssaDef).Add(operandInstruction);
                                    }
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

                        // With the block marked executable, all the phis based on it may have changed.
                        // Iterate through them and add those uses to the queue.
                        foreach (IOperandInstructionBase use in variableUses.Where(kvp => kvp.Key is PhiVariable p && p.Node.PossibleValues.ContainsKey(block)).SelectMany(kvp => kvp.Value))
                            instructionQueue.Enqueue(use);
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

        private static IEnumerable<SSADefinition> GetSSADefinitionsFromReferences(IInterimVariableReference reference)
        {
            switch (reference)
            {
                case InterimResolvedReference resolvedReference:
                    return Enumerable.Repeat(resolvedReference.Reference, 1);
                case InterimUnresolvedReference unresolvedReference:
                    return unresolvedReference.References;
                default:
                    throw new NotImplementedException();
            }
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
        private static HashSet<SSADefinition> PropagateConstants(Dictionary<SSADefinition, HashSet<IOperandInstructionBase>> ssaUses)
        {
            List<SSADefinition> constantVariables = ssaUses.Keys.Where(
                v => v.State == SSADefinition.SetState.Set &&
                v.IsInvariant &&
                typeof(PrimitiveStructure).IsAssignableFrom(v.Type))
                .ToList();
            
#if DEBUG   // Sort to make debugging variable iterations easier.
            constantVariables.Sort(Comparer<SSADefinition>.Create((a, b) => string.Compare(a.ToString(), b.ToString())));
            List<SSADefinition> usedVariables = ssaUses.Keys.ToList();
            usedVariables.Sort(Comparer<SSADefinition>.Create((a, b) => string.Compare(a.ToString(), b.ToString())));
#endif
            HashSet<SSADefinition> requiredDefinitions = new HashSet<SSADefinition>(constantVariables.Where(def =>
                ssaUses[def].Any(use =>
                {
                    if (use is IRCall call)
                    {
                        var function = call.Block.CodePart.GetFunction(call);
                        if (function == null)
                            return false;
                        if (function.ExternalReads.Any(var => var.Equals(def.Name, StringComparison.OrdinalIgnoreCase)))
                            return true;
                        if (function.ExternalWrites.Any(var => var.Equals(def.Name, StringComparison.OrdinalIgnoreCase)))
                            return true;
                        if (function.ExternalUnsets.Any(unset => unset.Name.Equals(def.Name, StringComparison.OrdinalIgnoreCase)))
                            return true;
                    }
                    return false;
                })));

            Dictionary<SSADefinition, InterimConstantValue> replacements = new Dictionary<SSADefinition, InterimConstantValue>();

            foreach (SSADefinition ssaDef in constantVariables)
            {
                replacements[ssaDef] = ssaDef.Evaluate();
            }

            foreach (SSADefinition constantDef in constantVariables)
            {
                IInterimOperand Propagate(IInterimOperand operand)
                    => PropagateConstant(operand, constantDef, replacements[constantDef]);
                foreach (IOperandInstructionBase instruction in ssaUses[constantDef])
                {
                    if (instruction is IRUnaryOp unaryOp &&
                        unaryOp.Operation is OpcodeExists)
                        continue;
                    if (instruction is IRUnset)
                        continue;
                    instruction.MutateEachOperand(Propagate);
                }
            }

            requiredDefinitions.UnionWith(ssaUses.Keys.Except(constantVariables));

            return requiredDefinitions;
        }

        private static void RemoveRedundantAssignments(BasicBlock block, HashSet<SSADefinition> usedVariables)
        {
            for (int i = 0; i < block.Instructions.Count; i++)
            {
                if (block.Instructions[i] is IRAssign assignment &&
                    AssignmentMayBeEliminated(assignment, usedVariables))
                {
                    RemoveAssignment(assignment);
                    i--;
                }
            }
        }

        private static bool AssignmentMayBeEliminated(IRAssign assignment, HashSet<SSADefinition> usedVariables)
        {
            // RelocateLater may not be eliminated since that is how functions are stored.
            if (assignment.Value is IRRelocateLater)
                return false;
            // Global assignments cannot be eliminated.
            if (assignment.Block.Scope.GetGlobalScope().Assignments.Contains(assignment))
                return false;
            if (DefinitionIsProtected(assignment.Target, usedVariables))
                return false;
            return true;
        }
        private static bool DefinitionIsProtected(SSADefinition definition, HashSet<SSADefinition> usedVariables)
        {
            // Must not remove definitions that are used.
            if (usedVariables.Contains(definition))
                return true;
            // Must not remove assignments that are later unset, if those unsets cannot also be removed.
            // Unsets can only be removed if they may unset anything besides this one.
            if (definition.ReplacedBy.Any(ssaDef => ssaDef.State == SSADefinition.SetState.Unset && ssaDef.Replaces.Count > 1))
                return true;
            // Must not remove assignments whose lifespan is not invariant.
            if (definition.ReplacedBy.Any(ssaDef => ssaDef.State == SSADefinition.SetState.PotentiallyUnset))
                return true;
            // Must not remove assignments that feed into a phi if there are multiple possible incoming values.
            foreach (PhiVariable phi in definition.ReplacedBy.Where(ssaDef => ssaDef is PhiVariable).Cast<PhiVariable>())
            {
                // Ignore restrictions on phis if this definition's block is not executable.
                if (!phi.Node.PossibleValues.First(kvp => kvp.Value == definition).Key.IsExecutable)
                    continue;
                // If the phi variable is protected, its incoming definitions must be too.
                if (DefinitionIsProtected(phi, usedVariables))
                    return true;
                // If the phi has multiple possible incoming values (from executable blocks),
                // they must all be preserved.
                if (!phi.Node.PossibleValues.Where(kvp => kvp.Key.IsExecutable).All(kvp => kvp.Value.Equals(definition)))
                    return true;
            }
            // If none of the above apply, it is safe to delete this definition.
            return false;
        }

        private static void RemoveAssignment(IRAssign assignment)
        {
            // Remove this assignment instruction
            assignment.Block.Instructions.Remove(assignment);

            // Remove this assignment from all scopes.
            IRScope scope = assignment.Block.Scope;
            while (scope != null)
            {
                scope.Assignments.Remove(assignment);
                scope = scope.ParentScope;
            }

            SSASetDefinition definition = assignment.Target;
            // Remove subsequent unsets
            // We've already assured no inadvertent side effects of this in DefinitionIsProtected()
            foreach (IRUnset unset in definition.ReplacedBy.
                Where(ssaDef => ssaDef.State == SSADefinition.SetState.Unset).
                Select(ssaDef => ssaDef.AssignedAt).Cast<IRUnset>())
            {
                unset.Block.Instructions.Remove(unset);
            }

            // Convert subsequent assignments to be declarative
            foreach (IRAssign nextAssign in definition.ReplacedBy.
                Where(ssaDef => ssaDef.State == SSADefinition.SetState.Set).
                Cast<SSASetDefinition>().Select(ssaDef => ssaDef.DefinedAt))
            {
                nextAssign.Scope = IRAssign.StoreScope.Local;
                nextAssign.AssertExists = false;
            }
        }

        private static IInterimOperand PropagateConstant(IInterimOperand operand, SSADefinition definition, InterimConstantValue constant)
        {
            if (operand is InterimResolvedReference resolvedReference &&
                resolvedReference.Reference.Equals((object)definition))
            {
                return constant;
            }
            return operand;
        }
    }
}
