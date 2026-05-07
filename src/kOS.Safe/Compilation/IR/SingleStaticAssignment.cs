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
        private static readonly Dictionary<IRInstruction, Dictionary<(string Name, IRScope Scope), SSADefinition>> reachableVariables =
            new Dictionary<IRInstruction, Dictionary<(string Name, IRScope Scope), SSADefinition>>();

        public static IReadOnlyDictionary<IRInstruction, Dictionary<(string Name, IRScope Scope), SSADefinition>>
            ReachableVariables => reachableVariables;

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
            foreach (IRFunction function in codePart.Functions)
            {
                foreach (BasicBlock block in function.InitializationCode)
                    ApplyUses(block, codePart, function);
                foreach (IRFunction.IRFunctionFragment fragment in function.Fragments)
                    foreach (BasicBlock block in fragment.FunctionCode)
                        ApplyUses(block, codePart, function);
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
                    ProcessCall(call, codePart, variables, block.TriggerPropagationBlacklist, funcOrTrigger);
                    IRFunction function = codePart.GetFunction(call);
                    if (function != null)
                        funcOrTrigger?.FunctionCalls.Add(codePart.GetFunction(call));

                }

                switch (instruction)
                {
                    case IRAssign assignment:
                        if (ProcessAssignment(assignment, codePart, variables, block.TriggerPropagationBlacklist))
                            funcOrTrigger?.ExternalWrites.Add(assignment.Target.Name);
                        break;
                    case IRUnset unset:
                        ProcessUnset(unset, variables, funcOrTrigger?.ExternalUnsets);
                        break;
                    case IRUnaryConsumer unaryConsumer:
                        // On encountering a trigger, blacklist any variables that are written in the trigger or body.
                        // Potentially apply any unsets in the trigger body.
                        if (unaryConsumer.Operation is OpcodeAddTrigger)
                        {
                            string pointer = (string)((InterimConstantValue)unaryConsumer.Operand).Value;
                            IRTrigger trigger = codePart.GetTrigger(pointer);
                            ProcessTrigger(trigger, variables, block.TriggerPropagationBlacklist);
                            funcOrTrigger?.TriggersCreated.Add(trigger);
                        }
                        break;
                }
            }

            return variables;
        }

        private static bool ProcessAssignment(IRAssign assignment, IRCodePart codePart, Dictionary<(string, IRScope), SSADefinition> variables, HashSet<(string, IRScope)> blacklist)
        {
            IRScope startingScope = assignment.Block.Scope;
            SSASetDefinition definition = assignment.Target;
            switch (assignment.Scope)
            {
                case IRAssign.StoreScope.Local:
                    variables[(definition.Name, startingScope)] = definition;
                    // IRFunction.IsGlobal initiates as false, so there's no need to |= false.
                    break;
                case IRAssign.StoreScope.Global:
                    variables[(definition.Name, startingScope.GetGlobalScope())] = definition;
                    SetFunctionToGlobal(assignment, codePart, variables, blacklist);
                    break;
                default:
                    if (ApplyDefinitionToName(definition, startingScope, variables))
                    {
                        // This isn't necessarily true in the case of nested function definitions.
                        // But true function definitions are definitively set as local or global.
                        // This only applies to a nested lock, which deserves to lose out on optimizations.
                        SetFunctionToGlobal(assignment, codePart, variables, blacklist);
                        return true;
                    }
                    break;
            }
            return false;
        }
        private static bool ApplyDefinitionToName(SSASetDefinition definition, IRScope scope, Dictionary<(string VariableName, IRScope Scope), SSADefinition> variables)
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
                            variables[(name, scope)] = SSAPotentialDefinition.PotentiallySet(definition, (IRUnset)lastPotential.AssignedAt);
                        else
                            variables[(name, scope)] = definition;
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
                        variables[(name, scope)] = slotValue.PotentiallyOverwrite(definition);
                    else
                        variables[(name, scope)] = definition.PotentiallyUnset((IRUnset)slotValue.AssignedAt);
                    potential = true;
                    lastPotential = slotValue;
                }
                else // The slot must be in a set state
                {
                    // If the higher value was only potentially set, anything further becomes potentially overwritten.
                    if (potential)
                        variables[(name, scope)] = slotValue.PotentiallyOverwrite(definition);
                    else
                        variables[(name, scope)] = definition;
                    break;
                }
                scope = scope.ParentScope;
            }
            return false;
        }
        private static void SetFunctionToGlobal(IRAssign assignment, IRCodePart codePart, Dictionary<(string VariableName, IRScope Scope),
            SSADefinition> variables, HashSet<(string, IRScope)> blacklist)
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
            ProcessTrigger(function, variables, blacklist);
        }

        private static void ProcessUnset(IRUnset unset, Dictionary<(string Name, IRScope), SSADefinition> variables, HashSet<(string, IRUnset)> recordTo)
        {
            IRScope startingScope = unset.Block.Scope;
            // On encountering an unset operation remove the affected definition from the stored variables.
            if (unset.IsInvariant)
            {
                if (Unset(unset, startingScope, variables))
                    recordTo?.Add((unset.Target.Name, unset));
            }
            else    // If the affected definition cannot be determined, potentially unset all reachable variables.
            {
                foreach (string varName in variables.Keys.Select(d => d.Name).Distinct())
                {
                    if (PotentiallyUnsetByName(unset, varName, startingScope, variables))
                        recordTo?.Add((varName, unset));
                }
            }
        }
        private static bool Unset(IRUnset unset, IRScope scope, Dictionary<(string VariableName, IRScope Scope), SSADefinition> variables)
        {
            bool closureVariableAffected = false;
            // Target is an instance of SSASetDefinition that is definitively Unset.
            SSASetDefinition target = unset.Target;
            string varName = target.Name;
            bool potential = false;
            while (scope != null)
            {
                if (scope.IsGlobalScope)
                    closureVariableAffected = true;
                if (variables.TryGetValue((varName, scope), out SSADefinition slotValue) &&
                    slotValue.State != SSADefinition.SetState.Unset)
                {
                    // If the higher scope slot was potentially unset, this one may remain valid
                    // Mark it as potentially unset as well.
                    if (potential)
                        variables[(varName, scope)] = slotValue.PotentiallyUnset(unset);
                    else
                        variables[(varName, scope)] = target;

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
        private static bool PotentiallyUnsetByName(IRUnset unset, string varName, IRScope scope, Dictionary<(string VariableName, IRScope Scope), SSADefinition> variables)
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
                    variables[(varName, scope)] = slotValue.PotentiallyUnset(unset);
                    // Break when there is a slot that is definitively Set.
                    if (slotValue.State == SSADefinition.SetState.Set)
                        break;
                }
                scope = scope.ParentScope;
            }
            return closureVariableAffected;
        }

        private static void ProcessTrigger(IClosureVariableUser trigger, Dictionary<(string, IRScope), SSADefinition> variables, HashSet<(string, IRScope)> blacklist)
        {
            foreach ((string varName, IRUnset unset) in trigger.ExternalUnsets)
            {
                IRScope scope = trigger.ClosureScope;
                while (scope != null)
                {
                    if (variables.TryGetValue((varName, scope), out SSADefinition ssaDef) &&
                        ssaDef.State != SSADefinition.SetState.Unset)
                    {
                        variables[(varName, scope)] = ssaDef.PotentiallyUnset(unset);
                    }
                }
            }
            foreach (string varName in trigger.ExternalWrites)
            {
                IRScope scope = trigger.ClosureScope;
                while (scope != null)
                {
                    if (variables.TryGetValue((varName, scope), out SSADefinition ssaDef) &&
                        ssaDef.State != SSADefinition.SetState.Unset)
                    {
                        blacklist.Add((varName, scope));
                    }
                    scope = scope.ParentScope;
                }
            }
        }

        private static void ProcessCall(IRCall call, IRCodePart codePart, Dictionary<(string, IRScope), SSADefinition> variables, HashSet<(string, IRScope)> blacklist, IClosureVariableUser callingClosure)
        {
            IRFunction function = codePart.GetFunction(call);
            if (function != null)
            {
                HandleFunction(call, function, variables, blacklist, callingClosure);
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
                    HandleFunction(call, func, variables, blacklist, callingClosure);
                }
            }
        }
        static void HandleFunction(IRCall call, IRFunction function, Dictionary<(string, IRScope), SSADefinition> variables, HashSet<(string, IRScope)> blacklist, IClosureVariableUser callingClosure)
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
                        variables[(varName, scope)] = ssaDef.PotentiallyUnset(unset);

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
                        variables[(varName, scope)] = ssaDef.PotentiallyOverwrite(SSASetDefinition.FromCallSite(varName, call));

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
                ProcessTrigger(trigger, variables, blacklist);
            }
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
                List<(BasicBlock Block, IRScope Scope, SSADefinition Variable)> varsIn = new List<(BasicBlock, IRScope, SSADefinition)>();

                // Make the blacklist the union of all incoming blacklists
                // Collect all incoming variable definitions
                foreach (BasicBlock predecessor in block.Predecessors)
                {
                    blacklist.UnionWith(predecessor.TriggerPropagationBlacklist);
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
                    if (definitionSet.Select(def => (def.Scope, def.Variable)).Distinct().Skip(1).Any())
                    {
                        // Phi required
                        if (!block.Phis.TryGetValue(definitionSet.Key, out PhiNode phiVar))
                        {
                            phiVar = new PhiNode(definitionSet.Key.Name);
                            block.Phis.Add(definitionSet.Key, phiVar);
                        }

                        foreach ((BasicBlock Block, IRScope _, SSADefinition Variable) in definitionSet)
                            phiVar.PossibleValues[Block] = Variable;

                        variablesIn.Add(definitionSet.Key, phiVar.Result);
                    }
                    else
                    {
                        block.Phis.Remove(definitionSet.Key);
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
                        varsOut.All(kvp => oldDefinition[kvp.Key].Equals(kvp.Value)))
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
            foreach (BasicBlock predecessor in block.Predecessors)
                triggerBlacklist.UnionWith(predecessor.TriggerPropagationBlacklist);

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
                        ProcessCall(call, codePart, liveDefinitions, triggerBlacklist, null);
                        IRFunction function = codePart.GetFunction(call);
                        if (function != null)
                        {
                            Dictionary<(string, IRScope), SSADefinition> reachableVariables =
                                new Dictionary<(string, IRScope), SSADefinition>();
                            foreach ((string, IRScope) slot in liveDefinitions.Keys
                                .Where(k => function.ExternalReads.Contains(k.Name)))
                                reachableVariables.Add(slot, liveDefinitions[slot]);
                            SingleStaticAssignment.reachableVariables[call] = reachableVariables;
                        }
                    }
                    if (inst is IOperandInstructionBase operandInstruction)
                        operandInstruction.MutateEachOperand(ScopedSSAReplacement);
                }

                // Process assignments, unsets, and new triggers.
                switch (instruction)
                {
                    case IRAssign assignment:
                        ProcessAssignment(assignment, null, liveDefinitions, triggerBlacklist);
                        break;
                    case IRUnset unset:
                        ProcessUnset(unset, liveDefinitions, null);
                        break;
                    case IRUnaryConsumer irTrigger:
                        if (irTrigger.Operation is OpcodeAddTrigger)
                        {
                            string pointer = (string)((InterimConstantValue)irTrigger.Operand).Value;
                            IRTrigger trigger = codePart.GetTrigger(pointer);
                            ProcessTrigger(trigger, liveDefinitions, triggerBlacklist);
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
                    string name = variableRef.Name;
                    // Don't apply to the global scope since SSA cannot be
                    // guaranteed for global variables.
                    while (!scope.IsGlobalScope)
                    {
                        if (liveDefinitions.TryGetValue((name, scope), out SSADefinition value) &&
                            value.State != SSADefinition.SetState.Unset)
                        {
                            if (triggerBlacklist.Contains((name, scope)))
                            {
                                return variableRef;
                            }
                            return new InterimVariableReference<SSADefinition>(value, variableRef);
                        }
                        scope = scope.ParentScope;
                    }
                    funcOrTrigger?.ExternalReads.Add(name);
                }
                else if (operand is IRUnaryOp existOp && existOp.Operation is OpcodeExists)
                {
                    if (existOp.Operand.IsInvariant)
                    {
                        string name;
                        if (existOp.Operand is IInterimVariableReference reference)
                            name = reference.Name;
                        else if (existOp is IEvaluatableToConstant constant)
                            name = (string)constant.Evaluate().Value;
                        else
                            return operand;

                        while (scope != null)
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
    }
}
