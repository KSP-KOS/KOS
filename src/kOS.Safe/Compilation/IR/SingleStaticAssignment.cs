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
    public class SingleStaticAssignment : IHolisticOptimizationPass, ILinkedOptimizationPass
    {
        public OptimizationLevel OptimizationLevel => OptimizationLevel.None;
        public short SortIndex => -2000;
        public Optimizer Optimizer { get; set; }

        public void ApplyPass(IRCodePart codePart)
            => FinalizeSSA(codePart, Optimizer.OptimizationLevel > OptimizationLevel.Aggressive);

        /// <summary>
        /// Finalizes a program into single static assignment form.
        /// </summary>
        public static void FinalizeSSA(IRCodePart codePart, bool stackAdoptsTypeHints)
        {
            Dictionary<IRFunction, HashSet<IRFunction>> funcCallTrees = new Dictionary<IRFunction, HashSet<IRFunction>>();
            Dictionary<IRFunction, HashSet<IRFunction>> callers = new Dictionary<IRFunction, HashSet<IRFunction>>();
            Queue<IRFunction> functionQueue = new Queue<IRFunction>(codePart.Functions.OrderBy(f => f.FunctionCalls.Count));
            Dictionary<IRFunction, Dictionary<(string, IRScope), SSADefinition>> externalSets = new Dictionary<IRFunction, Dictionary<(string, IRScope), SSADefinition>>();

            // Establish the call trees and ensure that all propagated effects are current.
            while (functionQueue.Count > 0)
            {
                IRFunction function = functionQueue.Dequeue();
                HashSet<IRFunction> functionCalls = new HashSet<IRFunction>(function.FunctionCalls);
                
                foreach (BasicBlock root in function.RootBlocks)
                    BuildPhis(root, codePart, function, stackAdoptsTypeHints);

                FlattenCallTree(function);

                bool same = true;
                // Require that the call tree reaches a stable point.
                if (funcCallTrees.TryGetValue(function, out HashSet<IRFunction> cachedCalls))
                {
                    bool callsEqual = cachedCalls.SetEquals(functionCalls);
                    same &= callsEqual;
                    if (!callsEqual)
                        funcCallTrees[function].UnionWith(function.FunctionCalls);
                }
                else
                    funcCallTrees.Add(function, new HashSet<IRFunction>(function.FunctionCalls));
                
                // Also require that the function's terminal block IncomingVariables is unchanged.
                // We'll use that the propagate ExternalSet SSA definitions at call sites.
                bool setsEqual = externalSets.TryGetValue(function, out Dictionary<(string, IRScope), SSADefinition> cachedSetDefinitions) &&
                    cachedSetDefinitions.ContentsEqual((function as ICodeComponent).TerminalBlock.IncomingVariableDefinitions);
                same &= setsEqual;
                if (!setsEqual)
                    externalSets[function] = new Dictionary<(string, IRScope), SSADefinition>(function.TerminalBlock.IncomingVariableDefinitions);

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
                BuildPhis(trigger.RootBlock, codePart, trigger, stackAdoptsTypeHints);
                FlattenCallTree(trigger);
            }

            // Now the main code can be SSA'd.
            if (codePart.MainCode.Count > 0)
                BuildPhis(codePart.MainCode[0], codePart, null, stackAdoptsTypeHints);

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
                    foreach (BasicBlock block in fragment.Blocks)
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
                foreach (BasicBlock block in trigger.Blocks)
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
                    {
                        funcOrTrigger?.FunctionCalls.Add(codePart.GetFunction(call));
                        function.CallSites.Add(call);
                    }
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
                        // TODO: Look at branch instructions that employ eq and propagate that definition.
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
                    assignment.IsInert = true;
                    // IRFunction.IsGlobal initiates as false, so there's no need to |= false.
                    break;
                case IRAssign.StoreScope.Global:
                    if (writeBlacklist.TryGetValue((definition.Name, startingScope), out IRUnset globalUnset))
                        ReplaceDefinition(variables, (definition.Name, startingScope.GetGlobalScope()), SSAPotentialDefinition.PotentiallySet(definition, globalUnset, writeReplaceChain), writeReplaceChain);
                    else
                        ReplaceDefinition(variables, (definition.Name, startingScope.GetGlobalScope()), definition, writeReplaceChain);
                    startingScope.GetGlobalScope().Assignments.Add(assignment);
                    SetFunctionToGlobal(assignment, codePart, variables, readBlacklist, writeBlacklist, writeReplaceChain);
                    assignment.IsInert = false;
                    break;
                default:
                    if (ApplyDefinitionToName(definition, startingScope, variables, writeReplaceChain))
                    {
                        // This isn't necessarily true in the case of nested function definitions.
                        // But true function definitions are definitively set as local or global.
                        // This only applies to a nested lock, which deserves to lose out on optimizations.
                        SetFunctionToGlobal(assignment, codePart, variables, readBlacklist, writeBlacklist, writeReplaceChain);
                        assignment.IsInert = false;
                        return true;
                    }
                    else
                        assignment.IsInert = true;
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
                        if (function.TerminalBlock.IncomingVariableDefinitions.TryGetValue((varName, function.ClosureScope.ParentScope), out SSADefinition writeDefinition))
                            ReplaceDefinition(variables, (varName, scope), writeDefinition, writeReplaceChain);
                        else
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
                => x.Item1.Equals(y.Item1) && SSADefinition.ReferenceEqualityComparer.Equals(x.Item2, y.Item2);

            public int GetHashCode((IRScope, SSADefinition) obj)
                => (obj.Item1.GetHashCode(), SSADefinition.ReferenceEqualityComparer.GetHashCode(obj.Item2)).GetHashCode();
        }

        private static readonly Dictionary<(BasicBlock, string), SSASetDefinition> externalDefinitionsCache = new Dictionary<(BasicBlock, string), SSASetDefinition>();
        public static void BuildPhis(BasicBlock root, bool stackAdoptsTypeHints, Dictionary<(string, IRScope), SSADefinition> incomingVariables = null)
            => BuildPhis(root, root.CodePart, null, stackAdoptsTypeHints, incomingVariables);
        private static void BuildPhis(BasicBlock root, IRCodePart codePart, IClosureVariableUser funcOrTrigger, bool stackAdoptsTypeHints, Dictionary<(string, IRScope), SSADefinition> incomingVariables = null)
        {
            Dictionary<BasicBlock, Dictionary<(string Name, IRScope Scope), SSADefinition>> variablesOut =
                new Dictionary<BasicBlock, Dictionary<(string, IRScope), SSADefinition>>();
            Dictionary<BasicBlock, List<IStackTransferObject>> stackOut =
                new Dictionary<BasicBlock, List<IStackTransferObject>>();

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
                int stackDepth = -1;
                BasicBlock stackDepthSetBy = null;
                if (block.Predecessors.Count == 0 && incomingVariables != null)
                {
                    foreach (KeyValuePair<(string Name, IRScope Scope), SSADefinition> variable in incomingVariables)
                        varsIn.Add((null, variable.Key.Scope, variable.Value));
                }
                else
                {
                    foreach (BasicBlock predecessor in block.Predecessors)
                    {
                        // Manage blacklist and incoming variables
                        blacklist.UnionWith(predecessor.TriggerPropagationBlacklist);
                        foreach (KeyValuePair<(string, IRScope), IRUnset> item in writeBlacklist)
                            writeBlacklist[item.Key] = item.Value;
                        if (variablesOut.TryGetValue(predecessor, out Dictionary<(string Name, IRScope Scope), SSADefinition> predVarsOut))
                        {
                            foreach (KeyValuePair<(string Name, IRScope Scope), SSADefinition> variable in predVarsOut
                                .Where(v => block.Scope.IsEqualOrEncompassedBy(v.Key.Scope)))
                                varsIn.Add((predecessor, variable.Key.Scope, variable.Value));
                        }

                        // Manage incoming stack
                        SetIncomingStackState(block, predecessor, stackOut, ref stackDepthSetBy, ref stackDepth, stackAdoptsTypeHints);
                    }
                }

                List<IStackTransferObject> stackResult = PopulateParameters(block, stackOut, worklist);

                // Patch in a bogus definition if not all paths to this
                // block provide a definition for a given external variable.
                if (block.Predecessors.Count > 1)
                {
                    List<(BasicBlock, IRScope, SSADefinition)> globalNullVars = new List<(BasicBlock, IRScope, SSADefinition)>();
                    foreach (IGrouping<SSADefinition, (BasicBlock Block, IRScope Scope, SSADefinition)> globalDef in
                        varsIn.Where(v => v.Scope.IsGlobalScope).GroupBy(v => v.Variable))
                    {
                        IRScope scope = globalDef.First().Scope;
                        foreach (BasicBlock predecessor in block.Predecessors.Except(globalDef.Select(v => v.Block)))
                        {
                            if (!externalDefinitionsCache.TryGetValue((predecessor, globalDef.Key.Name), out SSASetDefinition definition))
                            {
                                definition = new SSASetDefinition(globalDef.Key.Name, (IRAssign)null);
                                externalDefinitionsCache[(predecessor, globalDef.Key.Name)] = definition;
                            }
                            globalNullVars.Add((predecessor, scope, definition));
                        }
                    }
                    varsIn.AddRange(globalNullVars);
                }
                
                // Group variable definitions by their scope slot.
                // If a scope slot has multiple distinct definitions, generate a phi.
                // Store the resulting incoming variable definitions to the block.
                block.IncomingVariableDefinitions = GeneratePhis(block, varsIn);

                // Analyze the block with that set of incoming variable definitions
                Dictionary<(string Name, IRScope Scope), SSADefinition> varsOut =
                    AnalyzeBlock(block, codePart, funcOrTrigger);

                // If this block was previously analyzed, and
                // if the definitions all match, there's no need to queue
                // this block's successors.
                bool same = true;
                if (variablesOut.ContainsKey(block))
                {
                    Dictionary<(string, IRScope), SSADefinition> oldDefinition = variablesOut[block];
                    same &= oldDefinition.ContentsEqual(varsOut, SSADefinition.ReferenceEqualityComparer);
                }
                else
                    same = false;
                if (stackOut.ContainsKey(block))
                {
                    List<IStackTransferObject> stackOut_ = stackOut[block];
                    same &= stackOut_.SequenceEqual(stackResult);
                }
                else
                    same = false;

                if (same)
                    continue;

                // Cache this result for comparison in future passes.
                variablesOut[block] = varsOut;
                stackOut[block] = stackResult;

                // Enqueue all successor blocks.
                foreach (BasicBlock successor in block.Successors.Where(b => !worklist.Contains(b)))
                    worklist.Enqueue(successor);
            }
        }

        private static void SetIncomingStackState(BasicBlock block, BasicBlock predecessor, Dictionary<BasicBlock, List<IStackTransferObject>> stackOut, ref BasicBlock stackDepthSetBy, ref int stackDepth, bool stackAdoptsTypeHints)
        {
            if (stackOut.TryGetValue(predecessor, out List<IStackTransferObject> predStackOut))
            {
                if (predecessor.Predecessors.Count == 1 &&
                    predecessor.Predecessors.First().Instructions.Any() &&
                    predecessor.Predecessors.First().Continuation is BranchContinuation branch &&
                    branch.Condition is IRNonVarPush testArgBottom &&
                    testArgBottom.Operation is OpcodeTestArgBottom &&
                    predecessor == branch.True)
                {
                    predStackOut = new List<IStackTransferObject>(predStackOut);
                    predStackOut.RemoveAt(predStackOut.Count - 1);
                }
                // Verify that the stack depth is consistent
                // If the stack depth was not set by a root block and if the incoming stack depth does not match the stack depth, that's a problem.
                // If the list is nonzero in length and it doesn't match the incoming stack depth, that's a problem.
                if (((stackDepthSetBy?.Predecessors.Count ?? 0) != 0 && stackDepth != predStackOut.Count) ||
                    (block.IncomingStackState.Count != 0 && predStackOut.Count != block.IncomingStackState.Count))
                    throw new Exceptions.KOSCompileException(new KS.Token(), "Stack depth is inconsistent - the CFG is not well-structured.");
                stackDepth = predStackOut.Count;
                if (predecessor.Predecessors.Count > 0)
                    stackDepthSetBy = predecessor;
                // Propagate the stack values.
                for (int i = 0; i < stackDepth; i++)
                {
                    if (i >= block.IncomingStackState.Count)
                        block.IncomingStackState.Add(predStackOut[i]);
                    else if (block.IncomingStackState[i].Equals(predStackOut[i]))
                        continue;
                    else if (block.IncomingStackState[i] is StackTransferPhi phi)
                    {
                        phi.PossibleValues[predecessor] = predStackOut[i];
                        predStackOut[i].AddController(phi);
                    }
                    else if (block.IncomingStackState[i] is IRPushStack pushStack)
                    {
                        StackTransferPhi newPhi = new StackTransferPhi()
                        {
                            AdoptTypeHints = stackAdoptsTypeHints
                        };
                        newPhi.PossibleValues[predecessor] = predStackOut[i];
                        predStackOut[i].AddController(newPhi);
                        BasicBlock otherPredecessor = pushStack.Block ??
                            block.Predecessors.FirstOrDefault(b =>
                                b != predecessor &&
                                stackOut.ContainsKey(b) &&
                                stackOut[b].Count > i &&
                                stackOut[b][i].Equals(pushStack));
                        newPhi.PossibleValues[otherPredecessor] = pushStack;
                        pushStack.AddController(newPhi);
                        block.IncomingStackState[i] = newPhi;
                    }
                }
            }
        }
        public static List<IStackTransferObject> GetOutgoingStack(BasicBlock block)
            => PopulateParameters(block, null, null, false);
        private static List<IStackTransferObject> PopulateParameters(BasicBlock block, Dictionary<BasicBlock, List<IStackTransferObject>> stackOut, Queue<BasicBlock> worklist, bool setValues = true)
        {
            List<IStackTransferObject> stack = new List<IStackTransferObject>(block.IncomingStackState);
            foreach (IOperandInstructionBase operandInstruction in block.DepthFirstOperandInstructions())
            {
                operandInstruction.ForEachOperand(op =>
                {
                    if (op is IRParameter parameter)
                    {
                        if (stack.Count == 0)
                        {
                            IRPushStack externalPush = IRPushStack.ExternalPush();
                            HashSet<BasicBlock> addedTo = new HashSet<BasicBlock>();
                            Queue<BasicBlock> addTo = new Queue<BasicBlock>();
                            addTo.Enqueue(block);
                            while (addTo.Count > 0)
                            {
                                BasicBlock current = addTo.Dequeue();
                                if (addedTo.Add(current))
                                {
                                    if (current != block && stackOut.ContainsKey(current))
                                    {
                                        stackOut[current].Add(externalPush);
                                        foreach (BasicBlock successor in current.Successors.Where(b => !worklist.Contains(b)))
                                            worklist.Enqueue(successor);
                                    }
                                    current.IncomingStackState.Add(externalPush);
                                    foreach (BasicBlock predecessor in current.Predecessors)
                                        addTo.Enqueue(predecessor);
                                }
                            }
                            stack.Add(externalPush);
                        }
                        if (setValues)
                        {
                            parameter.StackTransferObject = stack[0];
                            parameter.RequiredToBeResolvable.UnionWith(GetFollowingParameters(operandInstruction, parameter));
                        }
                        // TODO: Add a sub-pass to swap binary operands to optimize the number of resolvable operands.
                        // Not really required now that TernaryOperands are implemented in IR.
                        stack.RemoveAt(0);
                    }
                    else if (op is IRCall call)
                    {
                        while (stack.Count > 0)
                        {
                            IStackTransferObject stackValue = stack[0];
                            stack.RemoveAt(0);

                            if (IsOrContainsArgMarker(stackValue))
                            {
                                if (stackValue is StackTransferPhi phi &&
                                    phi.PossibleValues.Values.Any(v => !IsOrContainsArgMarker(v)))
                                    throw new Exceptions.KOSCompileException(new KS.LineCol(call.SourceLine, call.SourceColumn),
                                        "Cannot handle a variable number of arguments to a function");
                                foreach (IRPushStackArgMarker argMarker in GetArgMarkerPushes(stackValue))
                                    argMarker.Call = call;
                                break;
                            }

                            if (!setValues)
                                continue;

                            IRParameter newParameter = new IRParameter(block.IncomingStackState.IndexOf(stackValue), block) { StackTransferObject = stackValue };
                            call.Arguments.Insert(0, newParameter);
                            newParameter.RequiredToBeResolvable.UnionWith(GetFollowingParameters(call, newParameter));
                        }
                    }
                });
                if (operandInstruction is IRPushStack pushStack)
                    stack.Insert(0, pushStack);
            }
            return stack;
        }
        private static Dictionary<(string Name, IRScope Scope), SSADefinition> GeneratePhis(BasicBlock block, List<(BasicBlock Block, IRScope Scope, SSADefinition Variable)> varsIn)
        {
            Dictionary<(string, IRScope), SSADefinition> result = new Dictionary<(string, IRScope), SSADefinition>();
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

                    result.Add(definitionSet.Key, phiVar.Result);
                }
                else
                {
                    if (block.Phis.ContainsKey(definitionSet.Key))
                    {
                        PhiNode phiVar = block.Phis[definitionSet.Key];
                        foreach (SSADefinition definition in phiVar.Result.Replaces)
                            definition.ReplacedBy.Remove(phiVar.Result);
                        block.Phis.Remove(definitionSet.Key);
                    }
                    result.Add(definitionSet.Key, definitionSet.First().Variable);
                }
            }
            return result;
        }
        private static IEnumerable<IRParameter> GetFollowingParameters(IOperandInstructionBase operation, IRParameter parameter)
        {
            if (!(operation is IMultipleOperandInstruction))
                return Enumerable.Empty<IRParameter>();
            bool foundParameter = false;
            List<IRParameter> result = new List<IRParameter>();
            operation.ForEachOperand(op =>
            {
                if (op == parameter)
                {
                    foundParameter = true;
                    return;
                }
                if (!foundParameter)
                    return;
                if (op is IRParameter laterParam)
                    result.Add(laterParam);
                else if (op is IOperandInstructionBase nestedOp)
                    AddNestedParameters(result, operation);
            });
            return result;
        }
        private static void AddNestedParameters(List<IRParameter> list, IOperandInstructionBase operation)
        {
            operation.ForEachOperand(op =>
            {
                if (op is IRParameter nestedParameter)
                    list.Add(nestedParameter);
                else if (op is IOperandInstructionBase nestedOp)
                    AddNestedParameters(list, nestedOp);
            });
        }
        private static bool IsOrContainsArgMarker(IStackTransferObject stackObj, HashSet<StackTransferPhi> visited = null)
        {
            if (visited == null)
                visited = new HashSet<StackTransferPhi>();
            switch (stackObj)
            {
                case null:
                    return false;
                case IRPushStackArgMarker _:
                    return true;
                case IRPushStack _:
                    return false;
                case StackTransferPhi phi:
                    if (!visited.Add(phi))
                        return false;
                    return phi.PossibleValues.Values.Any(v => IsOrContainsArgMarker(v, visited));
                default:
                    throw new NotImplementedException();
            }
        }
        private static IEnumerable<IRPushStackArgMarker> GetArgMarkerPushes(IStackTransferObject stackObj, HashSet<StackTransferPhi> visited = null)
        {
            if (visited == null)
                visited = new HashSet<StackTransferPhi>();
            switch (stackObj)
            {
                case null:
                    yield break;
                case IRPushStackArgMarker argMarker:
                    yield return argMarker;
                    yield break;
                case IRPushStack _:
                    yield break;
                case StackTransferPhi phi:
                    if (!visited.Add(phi))
                        yield break;
                    foreach (IRPushStackArgMarker phiArgMarker in phi.PossibleValues.Values.SelectMany(v => GetArgMarkerPushes(v, visited)))
                        yield return phiArgMarker;
                    yield break;
                default:
                    throw new NotImplementedException();
            }
        }

        public static void ApplyUses(BasicBlock block)
            => ApplyUses(block, block.CodePart, null);
        private static void ApplyUses(BasicBlock block, IRCodePart codePart, IClosureVariableUser funcOrTrigger)
        {
            List<IRInstruction> instructions = block.Instructions;

            // Start with a clone of the block's incoming variable definitions.
            Dictionary<(string Name, IRScope Scope), SSADefinition> liveDefinitions =
                new Dictionary<(string, IRScope), SSADefinition>();

            if (block.IncomingVariableDefinitions == null && !block.Predecessors.Any())
                block.IncomingVariableDefinitions = new Dictionary<(string Name, IRScope Scope), SSADefinition>();

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
                foreach (IOperandInstructionBase operandInstruction in instruction.DepthFirst())
                {
                    if (operandInstruction is IRCall call)
                    {
                        IRFunction function = codePart.GetFunction(call);
                        if (function != null)
                        {
                            codePart.ReachableVariables[call] = DetermineCallReaches(call, function, funcOrTrigger, liveDefinitions, triggerBlacklist);
                        }
                        ProcessCall(call, codePart, liveDefinitions, triggerBlacklist, triggerWriteBlacklist, null, true);
                    }
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
            if (block.Continuation is IOperandInstructionBase operandContinuation)
            {
                foreach (IOperandInstructionBase operandInstruction in operandContinuation.DepthFirst())
                    operandInstruction.MutateEachOperand(ScopedSSAReplacement);
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
