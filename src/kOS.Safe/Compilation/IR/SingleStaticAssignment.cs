using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Compilation.Optimization;
using static kOS.Safe.Compilation.IR.IRCodePart;

namespace kOS.Safe.Compilation.IR
{
    /// <summary>
    /// This utility class converts an IRCodePart into single static
    /// assignment form.
    /// </summary>
    public static class SingleStaticAssignment
    {
        public static Dictionary<IRInstruction, HashSet<IInterimVariableReference>> ReachableVariables { get; } =
            new Dictionary<IRInstruction, HashSet<IInterimVariableReference>>();

        /// <summary>
        /// Finalizes a program into single static assignment form.
        /// </summary>
        public static void FinalizeSSA(IRCodePart codePart)
        {
            Dictionary<IRFunction, HashSet<IRFunction>> funcCallTrees = new Dictionary<IRFunction, HashSet<IRFunction>>();
            Dictionary<IRFunction, HashSet<IRFunction>> callers = new Dictionary<IRFunction, HashSet<IRFunction>>();
            Queue<IRFunction> functionQueue = new Queue<IRFunction>(codePart.Functions.OrderBy(f => f.FunctionCalls.Count));

            // Establish the call trees and ensure that all propagated effects are current.
            while (functionQueue.Count > 0)
            {
                IRFunction function = functionQueue.Dequeue();
                HashSet<IRFunction> functionCalls = new HashSet<IRFunction>(function.FunctionCalls);
                
                foreach (BasicBlock root in function.RootBlocks)
                    BuildPhis(root, codePart, function);

                FlattenCallTree(function);

                if (funcCallTrees.TryGetValue(function, out HashSet<IRFunction> cachedCalls))
                {
                    if (cachedCalls.SetEquals(functionCalls))
                        continue;
                    funcCallTrees[function].UnionWith(function.FunctionCalls);
                }
                else
                    funcCallTrees.Add(function, new HashSet<IRFunction>(function.FunctionCalls));

                foreach (IRFunction callee in function.FunctionCalls)
                {
                    if (callers.TryGetValue(callee, out HashSet<IRFunction> callerSet))
                        callerSet.Add(function);
                    else
                        callers.Add(callee, new HashSet<IRFunction>() { function });
                }
                if (callers.ContainsKey(function))
                {
                    foreach (IRFunction caller in callers[function])
                    {
                        functionQueue.Enqueue(caller);
                    }
                }
            }
            // At this point, all functions have reached a stable point.

            // Triggers may create triggers, but don't call them in the same
            // way that functions can call functions. A single pass is sufficient.
            foreach (IRTrigger trigger in codePart.Triggers)
            {
                BuildPhis(trigger.RootBlock, codePart, trigger);
                FlattenCallTree(trigger);
            }

            // Now the main code can be SSA'd.
            if (codePart.MainCode.Count > 0)
                BuildPhis(codePart.MainCode[0], codePart, null);

            // Then apply the SSA definitions to all the variable push operations.
            // Functions are done iteratively to propagate the ExternalReads property
            // up the call chain correctly.
            foreach (IRFunction function in codePart.Functions.OrderBy(f => f.FunctionCalls.Count))
                functionQueue.Enqueue(function);
            while (functionQueue.Count > 0)
            {
                IRFunction function = functionQueue.Dequeue();
                HashSet<string> externalReads = new HashSet<string>(function.ExternalReads);
                foreach (BasicBlock block in function.InitializationCode)
                    ApplyUses(block, codePart, function);
                foreach (IRFunction.IRFunctionFragment fragment in function.Fragments)
                    foreach (BasicBlock block in fragment.FunctionCode)
                        ApplyUses(block, codePart, function);

                if (callers.ContainsKey(function) &&
                    !externalReads.SetEquals(function.ExternalReads))
                {
                    foreach (IRFunction caller in callers[function])
                        functionQueue.Enqueue(caller);
                }
            }
            foreach (IRTrigger trigger in codePart.Triggers)
            {
                foreach (BasicBlock block in trigger.Code)
                    ApplyUses(block, codePart, trigger);
            }
            foreach (BasicBlock block in codePart.MainCode)
                ApplyUses(block, codePart, null);
        }

        private static Dictionary<(string VariableName, IRScope Scope), SSADefinition> AnalyzeBlock(BasicBlock block, IRCodePart codePart, IClosureVariableUser funcOrTrigger)
        {
            Dictionary<(string Name, IRScope Scope), SSADefinition> variables =
                new Dictionary<(string, IRScope), SSADefinition>();

            foreach (KeyValuePair<(string, IRScope), SSADefinition> varIn in block.IncomingVariableDefinitions)
                variables.Add(varIn.Key, varIn.Value);

            List<IRInstruction> instructions = block.Instructions;
            for (int i = 0; i < instructions.Count; i++)
            {
                IRInstruction instruction = instructions[i];

                foreach (IRCall call in instruction.DepthFirst().Where(inst => inst is IRCall).Cast<IRCall>())
                {
                    ProcessCall(call, codePart, variables, block.TriggerPropagationBlacklist, block.TriggerUnsetBlacklist, funcOrTrigger, false);
                    IRFunction function = codePart.GetFunction(call);
                    if (function != null)
                        funcOrTrigger?.FunctionCalls.Add(codePart.GetFunction(call));

                }

                switch (instruction)
                {
                    case IRAssign assignment:
                        if (ProcessAssignment(assignment, codePart, variables, block.TriggerPropagationBlacklist, block.TriggerUnsetBlacklist, false))
                            funcOrTrigger?.ExternalWrites.Add(assignment.Target.Name);
                        break;
                    case IRUnset unset:
                        ProcessUnset(unset, variables, funcOrTrigger?.ExternalUnsets, false);
                        break;
                    case IRUnaryConsumer unaryConsumer:
                        // On encountering a trigger, blacklist any variables that are written in the trigger or body.
                        // Potentially apply any unsets in the trigger body.
                        if (unaryConsumer.Operation is OpcodeAddTrigger)
                        {
                            string pointer = (string)((InterimConstantValue)unaryConsumer.Operand).Value;
                            IRTrigger trigger = codePart.GetTrigger(pointer);
                            ProcessTrigger(trigger, variables, block.TriggerPropagationBlacklist, block.TriggerUnsetBlacklist, false);
                            funcOrTrigger?.TriggersCreated.Add(trigger);
                        }
                        break;
                }
            }

            return variables;
        }

        private static bool ProcessAssignment(IRAssign assignment, IRCodePart codePart, Dictionary<(string, IRScope), SSADefinition> variables, HashSet<(string, IRScope)> readBlacklist, Dictionary<(string, IRScope), IRUnset> writeBlacklist, bool writeReplaceChain)
        {
            IRScope startingScope = assignment.Block.Scope;
            SSASetDefinition definition = assignment.Target;
            switch (assignment.Scope)
            {
                case IRAssign.StoreScope.Local:
                    if (writeBlacklist.TryGetValue((definition.Name, startingScope), out IRUnset unset))
                        ReplaceDefinition(variables, (definition.Name, startingScope), definition.PotentiallyUnset(unset, writeReplaceChain), writeReplaceChain);
                    else
                        ReplaceDefinition(variables, (definition.Name, startingScope), definition, writeReplaceChain);
                    startingScope.Assignments.Add(assignment);
                    // IRFunction.IsGlobal initiates as false, so there's no need to |= false.
                    break;
                case IRAssign.StoreScope.Global:
                    if (writeBlacklist.TryGetValue((definition.Name, startingScope), out IRUnset globalUnset))
                        ReplaceDefinition(variables, (definition.Name, startingScope.GetGlobalScope()), SSAPotentialDefinition.PotentiallySet(definition, globalUnset, writeReplaceChain), writeReplaceChain);
                    else
                        ReplaceDefinition(variables, (definition.Name, startingScope.GetGlobalScope()), definition, writeReplaceChain);
                    startingScope.GetGlobalScope().Assignments.Add(assignment);
                    SetFunctionToGlobal(assignment, codePart, variables, readBlacklist, writeBlacklist, writeReplaceChain);
                    break;
                default:
                    if (ApplyDefinitionToName(definition, startingScope, variables, writeReplaceChain))
                    {
                        // This isn't necessarily true in the case of nested function definitions.
                        // But true function definitions are definitively set as local or global.
                        // This only applies to a nested lock, which deserves to lose out on optimizations.
                        SetFunctionToGlobal(assignment, codePart, variables, readBlacklist, writeBlacklist, writeReplaceChain);
                        return true;
                    }
                    break;
            }
            return false;
        }
        private static bool ApplyDefinitionToName(SSASetDefinition definition, IRScope scope, Dictionary<(string VariableName, IRScope Scope), SSADefinition> variables, bool writeReplaceChain)
        {
            string name = definition.Name;
            bool potential = false;
            SSADefinition lastPotential = null;
            while (scope != null)
            {
                // If there is no variable slot at this scope for this name, escalate one scope level.
                // Similarly if there is a slot but it's unset.
                if (!variables.TryGetValue((definition.Name, scope), out SSADefinition slotValue) ||
                    slotValue.State == SSADefinition.SetState.Unset)
                {
                    // If this is already the global scope, store the definition and break.
                    if (scope.IsGlobalScope)
                    {
                        if (potential)
                            ReplaceDefinition(variables, (name, scope), SSAPotentialDefinition.PotentiallySet(definition, (IRUnset)lastPotential.AssignedAt, writeReplaceChain), writeReplaceChain);
                        else
                            ReplaceDefinition(variables, (name, scope), definition, writeReplaceChain);
                        scope.Assignments.Add(definition.DefinedAt);
                        // Since the SSA algorithm for a function won't know
                        // of any slots filled between the function's top and the
                        // global scope, this is where propagation to the higher
                        // scope is appropriate.
                        // Save the loop check, we know there's no higher scope.
                        return true;
                    }
                    scope = scope.ParentScope;
                    continue;
                }
                // If the slot is in a potentially unset state, put it at the new definition's
                // potentially unset state.
                // Also recall that we've done this because further values become only potentially overwritten.
                if (slotValue.State == SSADefinition.SetState.PotentiallyUnset)
                {
                    if (potential)
                        ReplaceDefinition(variables, (name, scope), slotValue.PotentiallyOverwrite(definition, writeReplaceChain), writeReplaceChain);
                    else
                        ReplaceDefinition(variables, (name, scope), definition.PotentiallyUnset((IRUnset)slotValue.AssignedAt, writeReplaceChain), writeReplaceChain);
                    scope.Assignments.Add(definition.DefinedAt);
                    potential = true;
                    lastPotential = slotValue;
                }
                else // The slot must be in a set state
                {
                    // If the higher value was only potentially set, anything further becomes potentially overwritten.
                    if (potential)
                        ReplaceDefinition(variables, (name, scope), slotValue.PotentiallyOverwrite(definition, writeReplaceChain), writeReplaceChain);
                    else
                        ReplaceDefinition(variables, (name, scope), definition, writeReplaceChain);
                    scope.Assignments.Add(definition.DefinedAt);
                    break;
                }
                scope = scope.ParentScope;
            }
            return scope.IsGlobalScope;
        }
        private static void ReplaceDefinition(Dictionary<(string, IRScope), SSADefinition> variables, (string, IRScope) key, SSADefinition replacement, bool writeReplaceChain)
        {
            if (writeReplaceChain &&
                variables.TryGetValue(key, out SSADefinition original))
            {
                original.ReplacedBy.Add(replacement);
                replacement.Replaces.Add(original);
            }
            variables[key] = replacement;
        }

        private static void SetFunctionToGlobal(IRAssign assignment, IRCodePart codePart, Dictionary<(string VariableName, IRScope Scope),
            SSADefinition> variables, HashSet<(string, IRScope)> readBlacklist, Dictionary<(string, IRScope), IRUnset> writeBlacklist, bool writeReplaceChain)
        {
            if (codePart == null ||
                !(assignment.Value is IRRelocateLater funcOrTrigger))
                return;
            IRFunction function = codePart.GetFunction(((string)funcOrTrigger.Value).Split('-').First());
            if (function == null)
                return;
            function.IsGlobal = true;

            // A global function could be added to a trigger in external code.
            // Treat it as a trigger body itself.
            ProcessTrigger(function, variables, readBlacklist, writeBlacklist, writeReplaceChain);
        }

        private static void ProcessUnset(IRUnset unset, Dictionary<(string Name, IRScope), SSADefinition> variables, HashSet<(string, IRUnset)> recordTo, bool writeReplaceChain)
        {
            IRScope startingScope = unset.Block.Scope;
            // On encountering an unset operation remove the affected definition from the stored variables.
            if (unset.IsInvariant)
            {
                if (Unset(unset, startingScope, variables, writeReplaceChain))
                    recordTo?.Add((unset.Target.Name, unset));
            }
            else    // If the affected definition cannot be determined, potentially unset all reachable variables.
            {
                foreach (string varName in variables.Keys.Select(d => d.Name).Distinct())
                {
                    if (PotentiallyUnsetByName(unset, varName, startingScope, variables, writeReplaceChain))
                        recordTo?.Add((varName, unset));
                }
            }
        }
        private static bool Unset(IRUnset unset, IRScope scope, Dictionary<(string VariableName, IRScope Scope), SSADefinition> variables, bool writeReplaceChain)
        {
            bool closureVariableAffected = false;
            // Target is an instance of SSASetDefinition that is definitively Unset.
            SSASetDefinition target = unset.Target;
            string varName = target.Name;
            bool potential = false;
            while (scope != null)
            {
                if (scope.IsGlobalScope)
                {
                    closureVariableAffected = true;
                    if (potential)
                        target.Replaces.Add(null);
                }
                if (variables.TryGetValue((varName, scope), out SSADefinition slotValue) &&
                    slotValue.State != SSADefinition.SetState.Unset)
                {
                    // If the higher scope slot was potentially unset, this one may remain valid
                    // Mark it as potentially unset as well.
                    if (potential)
                        ReplaceDefinition(variables, (varName, scope), slotValue.PotentiallyUnset(unset, writeReplaceChain), writeReplaceChain);
                    else
                        ReplaceDefinition(variables, (varName, scope), target, writeReplaceChain);

                    // If this slot was potentially unset, the next higher scope slot
                    // could be the target of this unset.
                    // If this slot was definitively set, higher scopes are unaffected.
                    if (slotValue.State == SSADefinition.SetState.PotentiallyUnset)
                        potential = true;
                    else
                        break;
                }
                scope = scope.ParentScope;
            }
            return closureVariableAffected;
        }
        private static bool PotentiallyUnsetByName(IRUnset unset, string varName, IRScope scope, Dictionary<(string VariableName, IRScope Scope), SSADefinition> variables, bool writeReplaceChain)
        {
            bool closureVariableAffected = false;
            if (string.IsNullOrEmpty(varName))
                return closureVariableAffected;
            while (scope != null)
            {
                if (scope.IsGlobalScope)
                    closureVariableAffected = true;
                if (variables.TryGetValue((varName, scope), out SSADefinition slotValue) &&
                    slotValue.State != SSADefinition.SetState.Unset)
                {
                    ReplaceDefinition(variables, (varName, scope), slotValue.PotentiallyUnset(unset, writeReplaceChain), writeReplaceChain);
                    // Break when there is a slot that is definitively Set.
                    if (slotValue.State == SSADefinition.SetState.Set)
                        break;
                }
                scope = scope.ParentScope;
            }
            return closureVariableAffected;
        }

        private static void ProcessTrigger(IClosureVariableUser trigger, Dictionary<(string, IRScope), SSADefinition> variables, HashSet<(string, IRScope)> readBlacklist, Dictionary<(string, IRScope), IRUnset> writeBlacklist, bool writeReplaceChain)
        {
            foreach ((string varName, IRUnset unset) in trigger.ExternalUnsets)
            {
                IRScope scope = trigger.ClosureScope;
                while (scope != null)
                {
                    writeBlacklist[(varName, scope)] = unset;
                    if (variables.TryGetValue((varName, scope), out SSADefinition ssaDef) &&
                        ssaDef.State != SSADefinition.SetState.Unset)
                    {
                        ReplaceDefinition(variables, (varName, scope), ssaDef.PotentiallyUnset(unset, writeReplaceChain), writeReplaceChain);
                    }
                    scope = scope.ParentScope;
                }
            }
            foreach (string varName in trigger.ExternalWrites)
            {
                IRScope scope = trigger.ClosureScope;
                while (scope != null)
                {
                    readBlacklist.Add((varName, scope));
                    scope = scope.ParentScope;
                }
            }
        }

        private static void ProcessCall(IRCall call, IRCodePart codePart, Dictionary<(string, IRScope), SSADefinition> variables, HashSet<(string, IRScope)> readBlacklist, Dictionary<(string, IRScope), IRUnset> writeBlacklist, IClosureVariableUser callingClosure, bool writeReplaceChain)
        {
            IRFunction function = codePart.GetFunction(call);
            if (function != null)
            {
                HandleFunction(call, function, variables, readBlacklist, writeBlacklist, callingClosure, writeReplaceChain);
            }
            else if (!call.Direct && !(call.IndirectMethod is IRSuffixGetMethod))
            {
                // Function calls to a UserDelegate could be to any function
                // Global variables don't get SSA'd, so we don't care about
                // functions from other code parts.
                // Clobber/potentially overwrite anything affected by any
                // function in this code part.
                foreach (IRFunction func in codePart.Functions)
                {
                    HandleFunction(call, func, variables, readBlacklist, writeBlacklist, callingClosure, writeReplaceChain);
                }
            }
        }
        static void HandleFunction(IRCall call, IRFunction function, Dictionary<(string, IRScope), SSADefinition> variables, HashSet<(string, IRScope)> readBlacklist, Dictionary<(string, IRScope), IRUnset> writeBlacklist, IClosureVariableUser callingClosure, bool writeReplaceChain)
        {
            HashSet<string> externalWrites = callingClosure?.ExternalWrites;
            HashSet<(string, IRUnset)> externalUnsets = callingClosure?.ExternalUnsets;

            // Potential unsets must happen first, so that any sets propagate
            // through any uncertainties.
            // All unsets from functions are treated as potential only since
            // no analysis of control flow is done at this point.
            // This could be improved in the future, but is probably fine for
            // the rarity of unsets.
            foreach ((string varName, IRUnset unset) in function.ExternalUnsets)
            {
                IRScope scope = function.ClosureScope;
                while (scope != null)
                {
                    if (scope.IsGlobalScope)
                        externalUnsets?.Add((varName, unset));

                    if (variables.TryGetValue((varName, scope), out SSADefinition ssaDef) &&
                        ssaDef.State != SSADefinition.SetState.Unset)
                    {
                        ReplaceDefinition(variables, (varName, scope), ssaDef.PotentiallyUnset(unset, writeReplaceChain), writeReplaceChain);

                        // A recursive function could unset the same variable multiple times.
                        // So this goes all the way to the top.
                        if (ssaDef.State == SSADefinition.SetState.Set && !function.IsRecursive)
                            break;
                    }
                }
            }
            // Sets are also treated as potential overwrites since control flow
            // is not guaranteed.
            foreach (string varName in function.ExternalWrites)
            {
                IRScope scope = function.ClosureScope;
                while (scope != null)
                {
                    if (scope.IsGlobalScope)
                        externalWrites?.Add(varName);

                    if (variables.TryGetValue((varName, scope), out SSADefinition ssaDef) &&
                        ssaDef.State != SSADefinition.SetState.Unset)
                    {
                        ReplaceDefinition(variables, (varName, scope), ssaDef.PotentiallyOverwrite(SSASetDefinition.FromCallSite(varName, call), writeReplaceChain), writeReplaceChain);

                        if (ssaDef.State == SSADefinition.SetState.Set)
                            break;
                    }
                    scope = scope.ParentScope;
                }
            }
            // Finally, the trigger-affected variables are propagated.
            // This is handled as normal for a trigger.
            foreach (IRTrigger trigger in function.TriggersCreated)
            {
                ProcessTrigger(trigger, variables, readBlacklist, writeBlacklist, writeReplaceChain);
            }
        }

        private class PhiComparer : IEqualityComparer<(IRScope, SSADefinition)>
        {
            public static PhiComparer Instance = new PhiComparer();
            public bool Equals((IRScope, SSADefinition) x, (IRScope, SSADefinition) y)
                => x.Item1.Equals(y.Item1) && SSAReferenceEqualityComparer.Instance.Equals(x.Item2, y.Item2);

            public int GetHashCode((IRScope, SSADefinition) obj)
                => (obj.Item1.GetHashCode(), SSAReferenceEqualityComparer.Instance.GetHashCode(obj.Item2)).GetHashCode();
        }

        private static void BuildPhis(BasicBlock root, IRCodePart codePart, IClosureVariableUser funcOrTrigger)
        {
            Dictionary<BasicBlock, Dictionary<(string Name, IRScope Scope), SSADefinition>> variablesOut =
                new Dictionary<BasicBlock, Dictionary<(string, IRScope), SSADefinition>>();

            Queue<BasicBlock> worklist = new Queue<BasicBlock>();
            worklist.Enqueue(root);
            while (worklist.Count > 0)
            {
                BasicBlock block = worklist.Dequeue();

                HashSet<(string, IRScope)> blacklist = block.TriggerPropagationBlacklist;
                Dictionary<(string, IRScope), IRUnset> writeBlacklist = block.TriggerUnsetBlacklist;
                List<(BasicBlock Block, IRScope Scope, SSADefinition Variable)> varsIn = new List<(BasicBlock, IRScope, SSADefinition)>();

                // Make the blacklist the union of all incoming blacklists
                // Collect all incoming variable definitions
                foreach (BasicBlock predecessor in block.Predecessors)
                {
                    blacklist.UnionWith(predecessor.TriggerPropagationBlacklist);
                    foreach (KeyValuePair<(string, IRScope), IRUnset> item in writeBlacklist)
                        writeBlacklist[item.Key] = item.Value;
                    if (variablesOut.TryGetValue(predecessor, out Dictionary<(string Name, IRScope Scope), SSADefinition> predVarsOut))
                    {
                        foreach (KeyValuePair<(string Name, IRScope Scope), SSADefinition> variable in predVarsOut
                            .Where(v => block.Scope.IsEqualOrEncompassedBy(v.Key.Scope)))
                            varsIn.Add((predecessor, variable.Key.Scope, variable.Value));
                    }
                }

                Dictionary<(string, IRScope), SSADefinition> variablesIn = new Dictionary<(string, IRScope), SSADefinition>();

                // Group variable definitions by their scope slot.
                // If a scope slot has multiple distinct definitions, generate a phi.
                foreach (IGrouping<(string Name, IRScope Scope), (BasicBlock Block, IRScope Scope, SSADefinition Variable)> definitionSet in
                    varsIn.GroupBy(v => (v.Variable.Name, v.Scope)))
                {
                    if (definitionSet.Select(def => (def.Scope, def.Variable)).Distinct(PhiComparer.Instance).Skip(1).Any())
                    {
                        // Phi required
                        if (!block.Phis.TryGetValue(definitionSet.Key, out PhiNode phiVar))
                        {
                            phiVar = new PhiNode(definitionSet.Key.Name);
                            block.Phis.Add(definitionSet.Key, phiVar);
                        }

                        foreach ((BasicBlock incomingBlock, _, SSADefinition definition) in definitionSet)
                        {
                            phiVar.PossibleValues[incomingBlock] = definition;
                            definition.ReplacedBy.Add(phiVar.Result);
                            phiVar.Result.Replaces.Add(definition);
                        }

                        variablesIn.Add(definitionSet.Key, phiVar.Result);
                    }
                    else
                    {
                        if (block.Phis.ContainsKey(definitionSet.Key))
                        {
                            PhiNode phiVar = block.Phis[definitionSet.Key];
                            foreach ((_, _, SSADefinition definition) in definitionSet)
                            {
                                definition.ReplacedBy.Remove(phiVar.Result);
                                phiVar.Result.Replaces.Remove(definition);
                            }
                            block.Phis.Remove(definitionSet.Key);
                        }
                        variablesIn.Add(definitionSet.Key, definitionSet.First().Variable);
                    }
                }

                // Store the resulting incoming variable definitions to the block.
                block.IncomingVariableDefinitions = variablesIn;

                // Analyze the block with that set of incoming variable definitions
                Dictionary<(string Name, IRScope Scope), SSADefinition> varsOut =
                    AnalyzeBlock(block, codePart, funcOrTrigger);

                // If this block was previously analyzed, and
                // if the definitions all match, there's no need to queue
                // this block's successors.
                if (variablesOut.ContainsKey(block))
                {
                    Dictionary<(string, IRScope), SSADefinition> oldDefinition = variablesOut[block];
                    if (oldDefinition.Count == varsOut.Count &&
                        varsOut.All(kvp => oldDefinition.ContainsKey(kvp.Key) && SSAReferenceEqualityComparer.Instance.Equals(oldDefinition[kvp.Key], kvp.Value)))
                        continue;
                }

                // Cache this result for comparison in future passes.
                variablesOut[block] = varsOut;

                // Enqueue all successor blocks.
                foreach (BasicBlock successor in block.Successors)
                    worklist.Enqueue(successor);
            }
        }

        private static void ApplyUses(BasicBlock block, IRCodePart codePart, IClosureVariableUser funcOrTrigger)
        {
            List<IRInstruction> instructions = block.Instructions;

            // Start with a clone of the block's incoming variable definitions.
            Dictionary<(string Name, IRScope Scope), SSADefinition> liveDefinitions =
                new Dictionary<(string, IRScope), SSADefinition>();
            foreach (KeyValuePair<(string Name, IRScope Scope), SSADefinition> definition in block.IncomingVariableDefinitions)
                liveDefinitions[definition.Key] = definition.Value;

            // Start with the union of all incoming trigger blacklists.
            HashSet<(string, IRScope)> triggerBlacklist = new HashSet<(string, IRScope)>();
            Dictionary<(string, IRScope), IRUnset> triggerWriteBlacklist = new Dictionary<(string, IRScope), IRUnset>();
            foreach (BasicBlock predecessor in block.Predecessors)
            {
                triggerBlacklist.UnionWith(predecessor.TriggerPropagationBlacklist);
                foreach (KeyValuePair<(string, IRScope), IRUnset> item in predecessor.TriggerUnsetBlacklist)
                    triggerWriteBlacklist[item.Key] = item.Value;
            }

            // Create a local function for SSA replacement for this specific block.
            IInterimOperand ScopedSSAReplacement(IInterimOperand op)
                => SSAReplacement(op, block.Scope, liveDefinitions, triggerBlacklist, funcOrTrigger);

            foreach (IRInstruction instruction in block.Instructions)
            {
                // Process call sites and replace variable definitions.
                foreach (IRInstruction inst in instruction.DepthFirst())
                {
                    if (inst is IRCall call)
                    {
                        IRFunction function = codePart.GetFunction(call);
                        if (function != null)
                        {
                            ReachableVariables[call] = DetermineCallReaches(call, function, funcOrTrigger, liveDefinitions, triggerBlacklist);
                        }
                        ProcessCall(call, codePart, liveDefinitions, triggerBlacklist, triggerWriteBlacklist, null, true);
                    }
                    if (inst is IOperandInstructionBase operandInstruction)
                        operandInstruction.MutateEachOperand(ScopedSSAReplacement);
                }

                // Process assignments, unsets, and new triggers.
                switch (instruction)
                {
                    case IRAssign assignment:
                        ProcessAssignment(assignment, null, liveDefinitions, triggerBlacklist, triggerWriteBlacklist, true);
                        break;
                    case IRUnset unset:
                        ProcessUnset(unset, liveDefinitions, null, true);
                        break;
                    case IRUnaryConsumer irTrigger:
                        if (irTrigger.Operation is OpcodeAddTrigger)
                        {
                            string pointer = (string)((InterimConstantValue)irTrigger.Operand).Value;
                            IRTrigger trigger = codePart.GetTrigger(pointer);
                            ProcessTrigger(trigger, liveDefinitions, triggerBlacklist, triggerWriteBlacklist, true);
                        }
                        break;
                }
            }
        }

        private static IInterimOperand SSAReplacement(IInterimOperand operand, IRScope scope, Dictionary<(string, IRScope), SSADefinition> liveDefinitions, HashSet<(string, IRScope)> triggerBlacklist, IClosureVariableUser funcOrTrigger)
        {
            {
                if (operand is InterimVariableReference variableRef)
                {
                    IInterimVariableReference result = AttemptResolveReference(
                        variableRef, scope,
                        liveDefinitions, triggerBlacklist, out bool exceededClosure);

                    if (exceededClosure)
                        funcOrTrigger?.ExternalReads.Add(result.Name);
                    return result;
                }
                else if (operand is IRUnaryOp existOp && existOp.Operation is OpcodeExists)
                {
                    if (existOp.Operand.IsInvariant || existOp.Operand is IInterimVariableReference)
                    {
                        string name;
                        if (existOp.Operand is IInterimVariableReference reference)
                            name = reference.Name;
                        else if (existOp is IEvaluatableToConstant constant)
                            name = (string)constant.Evaluate().Value;
                        else
                            return operand;

                        while (!scope.IsGlobalScope)
                        {
                            if (liveDefinitions.TryGetValue((name, scope), out SSADefinition value) &&
                                value.State == SSADefinition.SetState.Set)
                                return new InterimConstantValue(Encapsulation.BooleanValue.True, existOp);
                            scope = scope.ParentScope;
                        }
                        funcOrTrigger?.ExternalReads.Add(name);
                    }
                }
                return operand;
            }
        }

        private static IInterimVariableReference AttemptResolveReference(IInterimVariableReference variableRef, IRScope startingScope, Dictionary<(string, IRScope), SSADefinition> liveDefinitions, HashSet<(string, IRScope)> triggerBlacklist, out bool exceededClosure)
        {
            exceededClosure = false;
            string name = variableRef.Name;
            bool blacklisted = false;
            IRScope scope = startingScope;
            InterimUnresolvedReference unresolvedReference = null;
            // Don't apply to the global scope since SSA cannot be
            // guaranteed for global variables.
            while (!scope.IsGlobalScope)
            {
                if (liveDefinitions.TryGetValue((name, scope), out SSADefinition value) &&
                    value.State != SSADefinition.SetState.Unset)
                {
                    if (triggerBlacklist.Contains((name, scope)))
                    {
                        blacklisted = true;
                        unresolvedReference = null;
                    }

                    if (unresolvedReference != null)
                    {
                        unresolvedReference.AddReference(value);
                        if (value.State == SSADefinition.SetState.Set)
                            return unresolvedReference;
                    }
                    else if (value.State == SSADefinition.SetState.Set)
                    {
                        if (blacklisted)
                            return variableRef;
                        return new InterimResolvedReference(value, variableRef.SourceLine, variableRef.SourceColumn);
                    }
                    else if (!blacklisted)
                        unresolvedReference = new InterimUnresolvedReference(value, variableRef.SourceLine, variableRef.SourceColumn);
                }
                scope = scope.ParentScope;
            }
            exceededClosure = true;
            return variableRef;
        }

        private static HashSet<IInterimVariableReference> DetermineCallReaches(IRCall call, IRFunction function, IClosureVariableUser funcOrTrigger, Dictionary<(string, IRScope), SSADefinition> liveDefinitions, HashSet<(string, IRScope)> triggerBlacklist)
        {
            HashSet<IInterimVariableReference> reachableVariables = new HashSet<IInterimVariableReference>();

            foreach (string name in function.ExternalReads.
                Union(function.ExternalWrites).
                Union(function.ExternalUnsets.Select(unset => unset.Name)))
            {
                IInterimVariableReference result = AttemptResolveReference(
                    new InterimVariableReference(name, call), function.ClosureScope,
                    liveDefinitions, triggerBlacklist, out bool exceededClosure);
                if (exceededClosure && funcOrTrigger != null)
                {
                    if (function.ExternalReads.Contains(name))
                        funcOrTrigger.ExternalReads.Add(result.Name);
                    if (function.ExternalWrites.Contains(name))
                        funcOrTrigger.ExternalWrites.Add(result.Name);
                    foreach ((string, IRUnset) externalUnset in function.ExternalUnsets.
                        Where(scopeSlot => scopeSlot.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                    {
                        funcOrTrigger.ExternalUnsets.Add(externalUnset);
                    }
                }
                if (!(result is InterimVariableReference))
                    reachableVariables.Add(result);
            }
            return reachableVariables;
        }
    }
}
