using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Compilation.Optimization;

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
            Dictionary<IClosureVariableUser, HashSet<IInterimFunction>> funcCallTrees = new Dictionary<IClosureVariableUser, HashSet<IInterimFunction>>();
            Dictionary<IClosureVariableUser, HashSet<IClosureVariableUser>> callers = new Dictionary<IClosureVariableUser, HashSet<IClosureVariableUser>>();
            Queue<IClosureVariableUser> functionQueue = new Queue<IClosureVariableUser>();
            Dictionary<IClosureVariableUser, Dictionary<ScopeSlot, SSADefinition>> externalSets = new Dictionary<IClosureVariableUser, Dictionary<ScopeSlot, SSADefinition>>();

            // Establish the call trees and ensure that all propagated effects are current.
            foreach (CodeElement element in codePart.Elements)
                foreach (IRAnonymousFunction anonFunc in element.AnonymousFunctions)
                    functionQueue.Enqueue(anonFunc);
            foreach (IRFunction function in codePart.Functions.OrderBy(f=>f.FunctionCalls.Count))
                functionQueue.Enqueue(function);
            foreach (IRTrigger trigger in codePart.Triggers)
                functionQueue.Enqueue(trigger);

            while (functionQueue.Count > 0)
            {
                IClosureVariableUser closure = functionQueue.Dequeue();
                HashSet<IInterimFunction> functionCalls = new HashSet<IInterimFunction>(closure.FunctionCalls);

                switch (closure)
                {
                    case IRFunction func:
                        foreach (BasicBlock root in func.RootBlocks)
                            BuildPhis(root, codePart, closure, stackAdoptsTypeHints);
                        break;
                    case CodeComponent component:
                        BuildPhis(component.RootBlock, codePart, component, stackAdoptsTypeHints);
                        break;
                    default:
                        throw new NotImplementedException();
                }

                IRCodePart.FlattenCallTree(closure);

                bool same = true;
                // Require that the call tree reaches a stable point.
                if (funcCallTrees.TryGetValue(closure, out HashSet<IInterimFunction> cachedCalls))
                {
                    bool callsEqual = cachedCalls.SetEquals(functionCalls);
                    same &= callsEqual;
                    if (!callsEqual)
                        funcCallTrees[closure].UnionWith(closure.FunctionCalls);
                }
                else
                {
                    funcCallTrees.Add(closure, new HashSet<IInterimFunction>(closure.FunctionCalls));
                    same = false;
                }
                
                // Also require that the closure's terminal block IncomingVariables is unchanged.
                // We'll use that the propagate ExternalSet SSA definitions at call sites.
                bool setsEqual = externalSets.TryGetValue(closure, out Dictionary<ScopeSlot, SSADefinition> cachedSetDefinitions) &&
                    cachedSetDefinitions.ContentsEqual(closure.TerminalBlock.IncomingVariableDefinitions);
                same &= setsEqual;
                if (!setsEqual)
                    externalSets[closure] = new Dictionary<ScopeSlot, SSADefinition>(closure.TerminalBlock.IncomingVariableDefinitions);

                if (!same)
                {
                    foreach (IInterimFunction callee in closure.FunctionCalls)
                    {
                        if (callers.TryGetValue(callee, out HashSet<IClosureVariableUser> callerSet))
                            callerSet.Add(closure);
                        else
                            callers.Add(callee, new HashSet<IClosureVariableUser>() { closure });
                    }
                    if (callers.ContainsKey(closure))
                    {
                        foreach (IClosureVariableUser caller in callers[closure])
                        {
                            functionQueue.Enqueue(caller);
                        }
                    }
                }
            }
            // At this point, all functions and triggers have reached a stable point.

            // Now the main code can be SSA'd.
            if (codePart.MainCode.RootBlock != null)
            {
                BuildPhis(codePart.MainCode.RootBlock, codePart, codePart.MainCode, stackAdoptsTypeHints);
            }

            // Then apply the SSA definitions to all the variable push operations.
            // Functions and triggers are done iteratively to propagate the ExternalReads property
            // up the call chain correctly.
            foreach (CodeElement element in codePart.Elements)
                foreach (IRAnonymousFunction anonFunc in element.AnonymousFunctions)
                    functionQueue.Enqueue(anonFunc);
            foreach (IRFunction function in codePart.Functions.OrderBy(f => f.FunctionCalls.Count))
                functionQueue.Enqueue(function);
            foreach (IRTrigger trigger in codePart.Triggers)
                functionQueue.Enqueue(trigger);

            while (functionQueue.Count > 0)
            {
                IClosureVariableUser function = functionQueue.Dequeue();
                HashSet<string> externalReads = new HashSet<string>(function.ExternalReads);
                switch (function)
                {
                    case IRFunction func:
                        // This one might need changing, but initialization code
                        // shouldn't include anonoymous functions.
                        foreach (BasicBlock block in func.InitializationCode.AllBlocks)
                            ApplyUses(block, codePart, function);
                        foreach (IRFunctionFragment fragment in func.Fragments)
                            foreach (BasicBlock block in fragment.Blocks)
                                ApplyUses(block, codePart, function);
                        break;
                    case CodeComponent component:
                        foreach (BasicBlock block in component.Blocks)
                            ApplyUses(block, codePart, component);
                        break;
                    default:
                        throw new NotImplementedException();
                }
                
                if (callers.ContainsKey(function) &&
                    !externalReads.SetEquals(function.ExternalReads))
                {
                    foreach (IClosureVariableUser caller in callers[function])
                        functionQueue.Enqueue(caller);
                }
            }
            foreach (BasicBlock block in codePart.MainCode.AllBlocks)
                ApplyUses(block, codePart, null);
        }

        private static IInterimFunction ResolveFunctionReference(IInterimOperand sourceDelegate, IRCodePart codePart, IRScope scope, SSAContext context)
        {
            sourceDelegate = DereferenceOperand(sourceDelegate, scope, context);
            if (sourceDelegate is IRDelegateRelocateLater irDelegate)
                return codePart.GetFunction((string)irDelegate.Value);
            return null;
        }

        private static IInterimOperand DereferenceOperand(IInterimOperand operand, IRScope scope, SSAContext context)
        {
            while (operand is IInterimVariableReference ||
                operand is IRParameter)
            {
                if (operand is IRParameter param)
                {
                    if (param.IsResolvable)
                        operand = param.StackTransferObject.Value;
                    else
                        break;
                }
                else if (operand is IInterimVariableReference reference)
                {
                    if (!(operand is InterimResolvedReference))
                        operand = AttemptResolveReference(reference, scope, context, out bool _);
                    if (operand is InterimResolvedReference resolvedReference &&
                        resolvedReference.Reference is SSASetDefinition setDefinition)
                        operand = setDefinition.DefinedAt.Value;
                    else
                        break;
                }
            }
            return operand;
        }

        private static Dictionary<ScopeSlot, SSADefinition> AnalyzeBlock(BasicBlock block, IRCodePart codePart, IClosureVariableUser funcOrTrigger)
        {
            Dictionary<ScopeSlot, SSADefinition> variables =
                new Dictionary<ScopeSlot, SSADefinition>();

            SSAContext context = new SSAContext(variables, block.TriggerPropagationBlacklist, block.TriggerUnsetBlacklist);

            foreach (KeyValuePair<ScopeSlot, SSADefinition> varIn in block.IncomingVariableDefinitions)
                variables.Add(varIn.Key, varIn.Value);

            foreach (IOperandInstructionBase instruction in block.DepthFirstOperandInstructions())
            {
                if (instruction is IRCall call && call.Function != null)
                {
                    if (call.Direct)
                    {
                        // Eligible direct targets are:
                        //  A built-in function of that name
                        //  A user function of that name
                        //  A variable of that name that contains a delegate
                        if (Optimizer.FunctionManager.Exists(call.Function.Replace("()", "")))
                            call.TargetMethod = new InterimBuiltInFunction(call.Function, call);
                        else
                        {
                            if (call.Function.EndsWith("*"))
                            {
                                IInterimFunction target = codePart.GetFunction(call.Function);
                                call.TargetMethod = new InterimUserFunction(target, call);
                            }
                            else
                            {
                                string target = call.Function;
                                if (!target.StartsWith("$"))
                                    target = "$" + target;
                                IInterimVariableReference variableReference = new InterimVariableReference(target, call);
                                variableReference = AttemptResolveReference(variableReference, block.Scope, context, out bool exceeded);

                                // Check if variableReference.Type is derived from BuiltInReference and then scoop the original.
                                if (typeof(Encapsulation.BuiltinDelegate).IsAssignableFrom(variableReference.Type) &&
                                    variableReference is InterimResolvedReference resolvedReference &&
                                    resolvedReference.Reference is SSASetDefinition setDefinition)
                                {
                                    call.TargetMethod = new InterimBuiltInFunction((string)((InterimConstantValue)((IRCall)setDefinition.DefinedAt.Value).Arguments.First()).Value, call);
                                }
                                else
                                {
                                    IInterimFunction targetFunc = ResolveFunctionReference(variableReference, codePart, block.Scope, context);
                                    call.TargetMethod = new InterimUserFunction(variableReference, targetFunc, call);
                                }
                            }
                        }
                    }
                    else
                    {
                        // Eligible indirect targets are:
                        //  [C#] Delegate - should not be used in practice anymore
                        //  KOSDelegate
                        //  ISuffixResult (cannot be stored to a variable)

                        // Indirect values must already be stored.

                    }

                    if (call.TargetMethod is InterimUserFunction userFunc)
                    {
                        IInterimFunction function = userFunc.Function;
                        ProcessCall(call, function, codePart, context, funcOrTrigger, false);
                        if (function != null)
                            funcOrTrigger?.FunctionCalls.Add(function);
                        // No need to set UnresolvedCallSites here since
                        // call.TargetMethod handles that more thoroughly.
                    }
                }
                else if (instruction is IRSuffixGet suffixGet &&
                    suffixGet.Suffix.Equals("call", StringComparison.OrdinalIgnoreCase))
                {
                    IInterimFunction function = ResolveFunctionReference(suffixGet.Object, codePart, block.Scope, context);
                    ProcessCall(suffixGet, function, codePart, context, funcOrTrigger, false);
                    if (function != null)
                        funcOrTrigger?.FunctionCalls.Add(function);
                    else
                        funcOrTrigger?.UnresolvedCallSites.Add((IRInstruction)instruction);
                }

                switch (instruction)
                {
                    case IRAssign assignment:
                        if (ProcessAssignment(assignment, codePart, context, false))
                            funcOrTrigger?.ExternalWrites.Add(assignment.Target.Name);
                        break;
                    case IRUnset unset:
                        ProcessUnset(unset, context.Variables, funcOrTrigger?.ExternalUnsets, false);
                        break;
                    case IRUnaryConsumer unaryConsumer:
                        // On encountering a trigger, blacklist any variables that are written in the trigger or body.
                        // Potentially apply any unsets in the trigger body.
                        if (unaryConsumer.Operation is OpcodeAddTrigger)
                        {
                            string pointer = (string)((InterimConstantValue)unaryConsumer.Operand).Value;
                            IRTrigger trigger = codePart.GetTrigger(pointer);
                            ProcessTrigger(trigger, context, false);
                            funcOrTrigger?.TriggersCreated.Add(trigger);
                        }
                        break;
                        // TODO: Look at branch instructions that employ eq and neq and propagate that definition.
                }
            }

            return context.Variables;
        }

        private static bool ProcessAssignment(IRAssign assignment, IRCodePart codePart, SSAContext context, bool writeReplaceChain)
        {
            IRScope startingScope = assignment.Block.Scope;
            SSASetDefinition definition = assignment.Target;
            Dictionary<ScopeSlot, IRUnset> writeBlacklist = context.WriteBlacklist;
            Dictionary<ScopeSlot, SSADefinition> variables = context.Variables;

            switch (assignment.Scope)
            {
                case IRAssign.StoreScope.Local:
                    if (writeBlacklist.TryGetValue(new ScopeSlot(definition.Name, startingScope), out IRUnset unset))
                        ReplaceDefinition(variables, new ScopeSlot(definition.Name, startingScope), definition.PotentiallyUnset(unset, writeReplaceChain), writeReplaceChain);
                    else
                        ReplaceDefinition(variables, new ScopeSlot(definition.Name, startingScope), definition, writeReplaceChain);
                    startingScope.Assignments.Add(assignment);
                    assignment.IsInert = true;
                    // IRFunction.IsGlobal initiates as false, so there's no need to |= false.
                    break;
                case IRAssign.StoreScope.Global:
                    if (writeBlacklist.TryGetValue(new ScopeSlot(definition.Name, startingScope), out IRUnset globalUnset))
                        ReplaceDefinition(variables, new ScopeSlot(definition.Name, startingScope.GetGlobalScope()), SSAPotentialDefinition.PotentiallySet(definition, globalUnset, writeReplaceChain), writeReplaceChain);
                    else
                        ReplaceDefinition(variables, new ScopeSlot(definition.Name, startingScope.GetGlobalScope()), definition, writeReplaceChain);
                    startingScope.GetGlobalScope().Assignments.Add(assignment);
                    SetFunctionToGlobal(assignment, codePart, context, writeReplaceChain);
                    assignment.IsInert = false;
                    break;
                default:
                    if (ApplyDefinitionToName(definition, startingScope, variables, writeReplaceChain))
                    {
                        // This isn't necessarily true in the case of nested function definitions.
                        // But true function definitions are definitively set as local or global.
                        // This only applies to a nested lock, which deserves to lose out on optimizations.
                        SetFunctionToGlobal(assignment, codePart, context, writeReplaceChain);
                        assignment.IsInert = false;
                        return true;
                    }
                    else
                        assignment.IsInert = true;
                    break;
            }
            return false;
        }
        private static bool ApplyDefinitionToName(SSASetDefinition definition, IRScope scope, Dictionary<ScopeSlot, SSADefinition> variables, bool writeReplaceChain)
        {
            string name = definition.Name;
            bool potential = false;
            SSADefinition lastPotential = null;
            while (scope != null)
            {
                // If there is no variable slot at this scope for this name, escalate one scope level.
                // Similarly if there is a slot but it's unset.
                if (!variables.TryGetValue(new ScopeSlot(definition.Name, scope), out SSADefinition slotValue) ||
                    slotValue.State == SSADefinition.SetState.Unset)
                {
                    // If this is already the global scope, store the definition and break.
                    if (scope.IsGlobalScope)
                    {
                        if (potential)
                            ReplaceDefinition(variables, new ScopeSlot(name, scope), SSAPotentialDefinition.PotentiallySet(definition, (IRUnset)lastPotential.AssignedAt, writeReplaceChain), writeReplaceChain);
                        else
                            ReplaceDefinition(variables, new ScopeSlot(name, scope), definition, writeReplaceChain);
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
                        ReplaceDefinition(variables, new ScopeSlot(name, scope), slotValue.PotentiallyOverwrite(definition, writeReplaceChain), writeReplaceChain);
                    else
                        ReplaceDefinition(variables, new ScopeSlot(name, scope), definition.PotentiallyUnset((IRUnset)slotValue.AssignedAt, writeReplaceChain), writeReplaceChain);
                    scope.Assignments.Add(definition.DefinedAt);
                    potential = true;
                    lastPotential = slotValue;
                }
                else // The slot must be in a set state
                {
                    // If the higher value was only potentially set, anything further becomes potentially overwritten.
                    if (potential)
                        ReplaceDefinition(variables, new ScopeSlot(name, scope), slotValue.PotentiallyOverwrite(definition, writeReplaceChain), writeReplaceChain);
                    else
                        ReplaceDefinition(variables, new ScopeSlot(name, scope), definition, writeReplaceChain);
                    scope.Assignments.Add(definition.DefinedAt);
                    break;
                }
                scope = scope.ParentScope;
            }
            return scope.IsGlobalScope;
        }
        private static void ReplaceDefinition(Dictionary<ScopeSlot, SSADefinition> variables, ScopeSlot key, SSADefinition replacement, bool writeReplaceChain)
        {
            if (writeReplaceChain &&
                variables.TryGetValue(key, out SSADefinition original))
            {
                original.ReplacedBy.Add(replacement);
                replacement.Replaces.Add(original);
            }
            variables[key] = replacement;
        }

        private static void SetFunctionToGlobal(IRAssign assignment, IRCodePart codePart, SSAContext context, bool writeReplaceChain)
        {
            if (assignment.Value is IRRelocateLater funcOrTrigger)
            {
                IInterimFunction function =
                    // Regular functions or triggers have some extra identifiers
                    codePart.GetFunction(((string)funcOrTrigger.Value).Split('-').First()) ??
                    // Anonymous functions just use their name
                    codePart.GetFunction((string)funcOrTrigger.Value);

                if (function == null)
                    return;

                if (function is IRFunction func)
                    func.IsGlobal = true;

                // A global function could be added to a trigger in external code.
                // Treat it as a trigger body itself.
                ProcessTrigger(function, context, writeReplaceChain);
            }
        }

        private static void ProcessUnset(IRUnset unset, Dictionary<ScopeSlot, SSADefinition> variables, HashSet<(string, IRUnset)> recordTo, bool writeReplaceChain)
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
        private static bool Unset(IRUnset unset, IRScope scope, Dictionary<ScopeSlot, SSADefinition> variables, bool writeReplaceChain)
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
                if (variables.TryGetValue(new ScopeSlot(varName, scope), out SSADefinition slotValue) &&
                    slotValue.State != SSADefinition.SetState.Unset)
                {
                    // If the higher scope slot was potentially unset, this one may remain valid
                    // Mark it as potentially unset as well.
                    if (potential)
                        ReplaceDefinition(variables, new ScopeSlot(varName, scope), slotValue.PotentiallyUnset(unset, writeReplaceChain), writeReplaceChain);
                    else
                        ReplaceDefinition(variables, new ScopeSlot(varName, scope), target, writeReplaceChain);

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
        private static bool PotentiallyUnsetByName(IRUnset unset, string varName, IRScope scope, Dictionary<ScopeSlot, SSADefinition> variables, bool writeReplaceChain)
        {
            bool closureVariableAffected = false;
            if (string.IsNullOrEmpty(varName))
                return closureVariableAffected;
            while (scope != null)
            {
                if (scope.IsGlobalScope)
                    closureVariableAffected = true;
                if (variables.TryGetValue(new ScopeSlot(varName, scope), out SSADefinition slotValue) &&
                    slotValue.State != SSADefinition.SetState.Unset)
                {
                    ReplaceDefinition(variables, new ScopeSlot(varName, scope), slotValue.PotentiallyUnset(unset, writeReplaceChain), writeReplaceChain);
                    // Break when there is a slot that is definitively Set.
                    if (slotValue.State == SSADefinition.SetState.Set)
                        break;
                }
                scope = scope.ParentScope;
            }
            return closureVariableAffected;
        }

        private static void ProcessTrigger(IClosureVariableUser trigger, SSAContext context, bool writeReplaceChain)
        {
            foreach ((string varName, IRUnset unset) in trigger.ExternalUnsets)
            {
                IRScope scope = trigger.ClosureScope;
                while (scope != null)
                {
                    context.WriteBlacklist[new ScopeSlot(varName, scope)] = unset;
                    if (context.Variables.TryGetValue(new ScopeSlot(varName, scope), out SSADefinition ssaDef) &&
                        ssaDef.State != SSADefinition.SetState.Unset)
                    {
                        ReplaceDefinition(context.Variables, new ScopeSlot(varName, scope), ssaDef.PotentiallyUnset(unset, writeReplaceChain), writeReplaceChain);
                    }
                    scope = scope.ParentScope;
                }
            }
            foreach (string varName in trigger.ExternalWrites)
            {
                IRScope scope = trigger.ClosureScope;
                while (scope != null)
                {
                    context.ReadBlacklist.Add(new ScopeSlot(varName, scope));
                    scope = scope.ParentScope;
                }
            }
        }

        private static void ProcessCall(IRInstruction call, IInterimFunction function, IRCodePart codePart, SSAContext context, IClosureVariableUser callingClosure, bool writeReplaceChain)
        {
            if (call is IRCall directCall &&
                (directCall.Direct ||
                (directCall.TargetMethod is IRSuffixGet suffixGet &&
                !suffixGet.Suffix.Equals("call", StringComparison.OrdinalIgnoreCase))) &&
                function == null)
                return;

            if (function != null)
            {
                HandleFunction(call, function, context, callingClosure, writeReplaceChain);
            }
            else
            {
                // Function calls to a UserDelegate could be to any function
                // Global variables don't get SSA'd, so we don't care about
                // functions from other code parts.
                // Clobber/potentially overwrite anything affected by any
                // function in this code part.
                foreach (IRFunction func in codePart.Functions)
                    HandleFunction(call, func, context, callingClosure, writeReplaceChain);
                foreach (IRAnonymousFunction anonFunc in codePart.Elements.SelectMany(e => e.AnonymousFunctions))
                    HandleFunction(call, anonFunc, context, callingClosure, writeReplaceChain);
            }
        }
        static void HandleFunction(IRInstruction callSite, IInterimFunction function, SSAContext context, IClosureVariableUser callingClosure, bool writeReplaceChain)
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

                    if (context.Variables.TryGetValue(new ScopeSlot(varName, scope), out SSADefinition ssaDef) &&
                        ssaDef.State != SSADefinition.SetState.Unset)
                    {
                        ReplaceDefinition(context.Variables, new ScopeSlot(varName, scope), ssaDef.PotentiallyUnset(unset, writeReplaceChain), writeReplaceChain);

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

                    if (context.Variables.TryGetValue(new ScopeSlot(varName, scope), out SSADefinition ssaDef) &&
                        ssaDef.State != SSADefinition.SetState.Unset)
                    {
                        if (function.TerminalBlock.IncomingVariableDefinitions.TryGetValue(new ScopeSlot(varName, function.ClosureScope.ParentScope), out SSADefinition writeDefinition))
                            ReplaceDefinition(context.Variables, new ScopeSlot(varName, scope), writeDefinition, writeReplaceChain);
                        else
                            ReplaceDefinition(context.Variables, new ScopeSlot(varName, scope), ssaDef.PotentiallyOverwrite(SSASetDefinition.FromCallSite(varName, callSite), writeReplaceChain), writeReplaceChain);

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
                ProcessTrigger(trigger, context, writeReplaceChain);
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
        public static void BuildPhis(BasicBlock root, bool stackAdoptsTypeHints, Dictionary<ScopeSlot, SSADefinition> incomingVariables = null)
            => BuildPhis(root, root.CodePart, null, stackAdoptsTypeHints, incomingVariables);
        private static void BuildPhis(BasicBlock root, IRCodePart codePart, IClosureVariableUser funcOrTrigger, bool stackAdoptsTypeHints, Dictionary<ScopeSlot, SSADefinition> incomingVariables = null)
        {
            Dictionary<BasicBlock, Dictionary<ScopeSlot, SSADefinition>> variablesOut =
                new Dictionary<BasicBlock, Dictionary<ScopeSlot, SSADefinition>>();
            Dictionary<BasicBlock, List<IStackTransferObject>> stackOut =
                new Dictionary<BasicBlock, List<IStackTransferObject>>();

            Queue<BasicBlock> worklist = new Queue<BasicBlock>();
            worklist.Enqueue(root);
            while (worklist.Count > 0)
            {
                BasicBlock block = worklist.Dequeue();

                HashSet<ScopeSlot> blacklist = block.TriggerPropagationBlacklist;
                Dictionary<ScopeSlot, IRUnset> writeBlacklist = block.TriggerUnsetBlacklist;
                List<(BasicBlock Block, IRScope Scope, SSADefinition Variable)> varsIn = new List<(BasicBlock, IRScope, SSADefinition)>();

                // Make the blacklist the union of all incoming blacklists
                // Collect all incoming variable definitions
                int stackDepth = -1;
                BasicBlock stackDepthSetBy = null;
                if (block.Predecessors.Count == 0 && incomingVariables != null)
                {
                    foreach (KeyValuePair<ScopeSlot, SSADefinition> variable in incomingVariables)
                        varsIn.Add((null, variable.Key.Scope, variable.Value));
                }
                else
                {
                    foreach (BasicBlock predecessor in block.Predecessors)
                    {
                        // Manage blacklist and incoming variables
                        blacklist.UnionWith(predecessor.TriggerPropagationBlacklist);
                        foreach (KeyValuePair<ScopeSlot, IRUnset> item in writeBlacklist)
                            writeBlacklist[item.Key] = item.Value;
                        if (variablesOut.TryGetValue(predecessor, out Dictionary<ScopeSlot, SSADefinition> predVarsOut))
                        {
                            foreach (KeyValuePair<ScopeSlot, SSADefinition> variable in predVarsOut
                                .Where(v => block.Scope.IsEqualOrEncompassedBy(v.Key.Scope)))
                                varsIn.Add((predecessor, variable.Key.Scope, variable.Value));
                        }

                        // Manage incoming stack
                        SetIncomingStackState(block, predecessor, stackOut, ref stackDepthSetBy, ref stackDepth, worklist, stackAdoptsTypeHints);
                    }
                }

                List<IStackTransferObject> stackResult = PopulateParameters(block, stackOut, worklist);

                // Patch in a bogus definition if not all paths to this
                // block provide a definition for a given external variable.
                if (block.Predecessors.Count > 1)
                {
                    List<(BasicBlock, IRScope, SSADefinition)> globalNullVars = new List<(BasicBlock, IRScope, SSADefinition)>();
                    foreach (IGrouping<SSADefinition, (BasicBlock Block, IRScope Scope, SSADefinition)> globalDef in
                        varsIn.Where(v => v.Scope.IsGlobalScope).GroupBy(v => v.Variable, SSADefinition.ReferenceEqualityComparer))
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
                Dictionary<ScopeSlot, SSADefinition> varsOut = AnalyzeBlock(block, codePart, funcOrTrigger);

                // If this block was previously analyzed, and
                // if the definitions all match, there's no need to queue
                // this block's successors.
                bool same = true;
                if (variablesOut.ContainsKey(block))
                {
                    Dictionary<ScopeSlot, SSADefinition> oldDefinition = variablesOut[block];
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

        private static void SetIncomingStackState(BasicBlock block, BasicBlock predecessor, Dictionary<BasicBlock, List<IStackTransferObject>> stackOut, ref BasicBlock stackDepthSetBy, ref int stackDepth, Queue<BasicBlock> worklist, bool stackAdoptsTypeHints)
        {
            if (stackOut.TryGetValue(predecessor, out List<IStackTransferObject> predStackOut))
            {
                if (block.Dominator?.Continuation is BranchContinuation branch &&
                    branch.Condition is IRNonVarPush testArgBottom &&
                    testArgBottom.Operation is OpcodeTestArgBottom)
                {
                    if (block == branch.True)
                    {
                        // This block adds an optional parameter
                        if (predStackOut.Count == 0)
                        {
                            // The parent block doesn't know yet.
                            AddParameterToPredecessors(IRPushStack.ExternalPush(), block, stackOut, worklist);
                            block.IncomingStackState.RemoveAt(block.IncomingStackState.Count - 1);
                        }
                        else if (predStackOut.Count > 0 && block.IncomingStackState.Count > 0 &&
                            predStackOut[0] == block.IncomingStackState[0])
                        {
                            block.IncomingStackState.RemoveAt(0);
                        }
                        predStackOut = new List<IStackTransferObject>(predStackOut);
                        predStackOut.RemoveAt(0);
                    }
                }
                if (predStackOut.Count < block.IncomingStackState.Count)
                {
                    worklist.Enqueue(predecessor);
                    return;
                }

                // Verify that the stack depth is consistent
                // If the stack depth was not set by a root block and if the incoming stack depth does not match the stack depth, that's a problem.
                // If the list is nonzero in length and it doesn't match the incoming stack depth, that's a problem.
                if (predStackOut.Count < block.IncomingStackState.Count)
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
                        BasicBlock otherPredecessor = pushStack.Block ??
                            block.Predecessors.FirstOrDefault(b =>
                                b != predecessor &&
                                stackOut.ContainsKey(b) &&
                                stackOut[b].Count > i &&
                                stackOut[b][i].Equals(pushStack));
                        if (otherPredecessor != null)
                        {
                            StackTransferPhi newPhi = new StackTransferPhi()
                            {
                                AdoptTypeHints = stackAdoptsTypeHints
                            };
                            // TODO: Fix this holding on to old controllers.
                            newPhi.PossibleValues[predecessor] = predStackOut[i];
                            predStackOut[i].AddController(newPhi);
                            newPhi.PossibleValues[otherPredecessor] = pushStack;
                            pushStack.AddController(newPhi);
                            block.IncomingStackState[i] = newPhi;
                        }
                    }
                }
            }
        }
        public static List<IStackTransferObject> GetOutgoingStack(BasicBlock block)
            => PopulateParameters(block, null, null, false);
        private static void AddParameterToPredecessors(IStackTransferObject parameter, BasicBlock originator, Dictionary<BasicBlock, List<IStackTransferObject>> stackOut, Queue<BasicBlock> worklist)
        {
            HashSet<BasicBlock> addedTo = new HashSet<BasicBlock>();
            Queue<BasicBlock> addTo = new Queue<BasicBlock>();
            addTo.Enqueue(originator);
            while (addTo.Count > 0)
            {
                BasicBlock current = addTo.Dequeue();
                if (addedTo.Add(current))
                {
                    if (current != originator && stackOut.ContainsKey(current))
                    {
                        stackOut[current].Add(parameter);
                        foreach (BasicBlock successor in current.Successors.Where(b => !worklist.Contains(b)))
                            worklist.Enqueue(successor);
                    }
                    current.IncomingStackState.Add(parameter);
                    foreach (BasicBlock predecessor in current.Predecessors)
                        addTo.Enqueue(predecessor);
                }
            }
        }
        private static List<IStackTransferObject> PopulateParameters(BasicBlock block, Dictionary<BasicBlock, List<IStackTransferObject>> stackOut, Queue<BasicBlock> worklist, bool setValues = true)
        {
            List<IStackTransferObject> stack = new List<IStackTransferObject>(block.IncomingStackState);
            if (block is SyntheticReturnBlock)
                return stack;
            foreach (IOperandInstructionBase operandInstruction in block.DepthFirstOperandInstructions())
            {
                operandInstruction.ForEachOperand(op =>
                {
                    if (op is IRParameter parameter)
                    {
                        if (stack.Count == 0)
                        {
                            IRPushStack externalPush = IRPushStack.ExternalPush();
                            AddParameterToPredecessors(externalPush, block, stackOut, worklist);
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
                        if (!call.Closed)
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
                                    if (!call.Direct)
                                    {
                                        stackValue = stack[0];
                                        stack.RemoveAt(0);
                                        IRParameter indirectParameter = new IRParameter(block.IncomingStackState.IndexOf(stackValue), block) { StackTransferObject = stackValue };
                                        call.TargetMethod = indirectParameter;
                                    }
                                    break;
                                }

                                if (!setValues)
                                    continue;

                                IRParameter newParameter = new IRParameter(block.IncomingStackState.IndexOf(stackValue), block) { StackTransferObject = stackValue };
                                call.Arguments.Insert(0, newParameter);
                                newParameter.RequiredToBeResolvable.UnionWith(GetFollowingParameters(call, newParameter));
                            }
                        }
                    }
                });
                if (operandInstruction is IRPushStack pushStack)
                    stack.Insert(0, pushStack);
            }
            return stack;
        }
        private static Dictionary<ScopeSlot, SSADefinition> GeneratePhis(BasicBlock block, List<(BasicBlock Block, IRScope Scope, SSADefinition Variable)> varsIn)
        {
            Dictionary<ScopeSlot, SSADefinition> result = new Dictionary<ScopeSlot, SSADefinition>();
            foreach (IGrouping<ScopeSlot, (BasicBlock Block, IRScope Scope, SSADefinition Variable)> definitionSet in
                    varsIn.GroupBy(v => new ScopeSlot(v.Variable.Name, v.Scope)))
            {
                if (definitionSet.Select(def => (def.Scope, def.Variable)).Distinct(PhiComparer.Instance).Skip(1).Any())
                {
                    // Phi required
                    if (!block.Phis.TryGetValue(definitionSet.Key, out PhiNodeSSA phiVar))
                    {
                        phiVar = new PhiNodeSSA(definitionSet.Key.Name) { RequireExecutable = false };
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
                        PhiNodeSSA phiVar = block.Phis[definitionSet.Key];
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
            Dictionary<ScopeSlot, SSADefinition> liveDefinitions = new Dictionary<ScopeSlot, SSADefinition>();

            if (block.IncomingVariableDefinitions == null)
            {
                block.IncomingVariableDefinitions = new Dictionary<ScopeSlot, SSADefinition>();
                block.Scope = block.CodeComponent.RootBlock.Scope.GetGlobalScope();
                return;
            }

            foreach (KeyValuePair<ScopeSlot, SSADefinition> definition in block.IncomingVariableDefinitions)
                liveDefinitions[definition.Key] = definition.Value;

            // Start with the union of all incoming trigger blacklists.
            HashSet<ScopeSlot> triggerBlacklist = new HashSet<ScopeSlot>();
            Dictionary<ScopeSlot, IRUnset> triggerWriteBlacklist = new Dictionary<ScopeSlot, IRUnset>();
            foreach (BasicBlock predecessor in block.Predecessors)
            {
                triggerBlacklist.UnionWith(predecessor.TriggerPropagationBlacklist);
                foreach (KeyValuePair<ScopeSlot, IRUnset> item in predecessor.TriggerUnsetBlacklist)
                    triggerWriteBlacklist[item.Key] = item.Value;
            }

            // Create a local function for SSA replacement for this specific block.
            SSAContext context = new SSAContext(liveDefinitions, triggerBlacklist, triggerWriteBlacklist);
            IInterimOperand ScopedSSAReplacement(IInterimOperand op)
                => SSAReplacement(op, block.Scope, context, funcOrTrigger);

            foreach (IRInstruction instruction in block.Instructions)
            {
                codePart.ReachableVariables[instruction] = DetermineReaches(
                    instruction, liveDefinitions.Keys.Select(def => def.Name).Distinct(StringComparer.OrdinalIgnoreCase),
                    context);
                // Process call sites and replace variable definitions.
                foreach (IOperandInstructionBase operandInstruction in instruction.DepthFirst())
                {
                    if (operandInstruction is IRCall call)
                    {
                        if (call.TargetMethod is InterimUserFunction userFunc)
                        {
                            if (userFunc.Function != null)
                                codePart.ReachableVariables[call] = DetermineCallReaches(call, userFunc.Function, funcOrTrigger, context);
                            ProcessCall(call, userFunc.Function, codePart, context, null, true);
                        }
                    }
                    else if (operandInstruction is IRSuffixGet suffixGet &&
                        suffixGet.Suffix.Equals("call", StringComparison.OrdinalIgnoreCase))
                    {
                        IInterimFunction function = ResolveFunctionReference(suffixGet.Object, codePart, block.Scope, context);
                        if (function != null)
                        {
                            codePart.ReachableVariables[(IRInstruction)suffixGet.Object] = DetermineCallReaches((IRInstruction)suffixGet.Object, function, funcOrTrigger, context);
                        }
                        ProcessCall(suffixGet, function, codePart, context, null, true);
                    }
                    operandInstruction.MutateEachOperand(ScopedSSAReplacement);
                }

                // Process assignments, unsets, and new triggers.
                switch (instruction)
                {
                    case IRAssign assignment:
                        ProcessAssignment(assignment, codePart, context, true);
                        break;
                    case IRUnset unset:
                        ProcessUnset(unset, liveDefinitions, null, true);
                        break;
                    case IRUnaryConsumer irTrigger:
                        if (irTrigger.Operation is OpcodeAddTrigger)
                        {
                            string pointer = (string)((InterimConstantValue)irTrigger.Operand).Value;
                            IRTrigger trigger = codePart.GetTrigger(pointer);
                            ProcessTrigger(trigger, context, true);
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

        private static IInterimOperand SSAReplacement(IInterimOperand operand, IRScope scope, SSAContext context, IClosureVariableUser funcOrTrigger)
        {
            {
                if (operand is InterimVariableReference variableRef)
                {
                    IInterimVariableReference result = AttemptResolveReference(
                        variableRef, scope, context, out bool exceededClosure);

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
                            if (context.Variables.TryGetValue(new ScopeSlot(name, scope), out SSADefinition value) &&
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

        private static IInterimVariableReference AttemptResolveReference(IInterimVariableReference variableRef, IRScope startingScope, SSAContext context, out bool exceededClosure)
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
                if (context.Variables.TryGetValue(new ScopeSlot(name, scope), out SSADefinition value) &&
                    value.State != SSADefinition.SetState.Unset)
                {
                    if (context.ReadBlacklist.Contains(new ScopeSlot(name, scope)))
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

        private static HashSet<IInterimVariableReference> DetermineCallReaches(IRInstruction call, IInterimFunction function, IClosureVariableUser funcOrTrigger, SSAContext context)
        {
            HashSet<IInterimVariableReference> reachableVariables = new HashSet<IInterimVariableReference>();

            foreach (string name in function.ExternalReads.
                Union(function.ExternalWrites).
                Union(function.ExternalUnsets.Select(unset => unset.Name)))
            {
                IInterimVariableReference result = AttemptResolveReference(
                    new InterimVariableReference(name, call), function.ClosureScope,
                    context, out bool exceededClosure);
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
        private static HashSet<IInterimVariableReference> DetermineReaches(IRInstruction instruction, IEnumerable<string> variablesToTest, SSAContext context)
        {
            HashSet<IInterimVariableReference> reachableVariables = new HashSet<IInterimVariableReference>();

            foreach (string name in variablesToTest)
            {
                IInterimVariableReference result = AttemptResolveReference(
                    new InterimVariableReference(name, instruction), instruction.Block.Scope,
                    context, out _);
                if (!(result is InterimVariableReference))
                    reachableVariables.Add(result);
            }
            return reachableVariables;
        }

        public static bool DefinitionIsProtected(SSADefinition definition)
        {
            // Must not remove assignments that are later unset, if those unsets cannot also be removed.
            // Unsets can only be removed if they may unset anything besides this one.
            if (definition.ReplacedBy.Any(ssaDef => ssaDef.State == SSADefinition.SetState.Unset && ssaDef.Replaces.Count > 1))
                return true;
            // Must not remove assignments whose lifespan is not invariant.
            if (definition.ReplacedBy.Any(ssaDef => ssaDef.State == SSADefinition.SetState.PotentiallyUnset))
                return true;

            if (GetDownstreamAssignments(definition).Any(ssaDef => ssaDef.DefinedAt == null))
                return true;

            // If none of the above apply, it is safe to delete this definition.
            return false;
        }

        private static HashSet<SSASetDefinition> GetDownstreamAssignments(SSADefinition definition)
        {
            Queue<SSADefinition> definitionQueue = new Queue<SSADefinition>(definition.ReplacedBy);
            HashSet<SSASetDefinition> downstreamAssignments = new HashSet<SSASetDefinition>(SSADefinition.ReferenceEqualityComparer);
            HashSet<SSADefinition> visited = new HashSet<SSADefinition>(SSADefinition.ReferenceEqualityComparer);
            while (definitionQueue.Count > 0)
            {
                SSADefinition replacedBy = definitionQueue.Dequeue();
                if (!visited.Add(replacedBy))
                    continue;
                switch (replacedBy)
                {
                    case SSASetDefinition setDef:
                        downstreamAssignments.Add(setDef);
                        break;
                    case SSAPotentialDefinition potentialDef:
                        if (potentialDef.Preceding != definition)
                            throw new InvalidOperationException();
                        definitionQueue.Enqueue(potentialDef.Succeeding);
                        break;
                    case PhiVariable ssaDef:
                        foreach (SSADefinition potentialValue in ssaDef.Node.PossibleValues.Values)
                            if (potentialValue != definition)
                                definitionQueue.Enqueue(potentialValue);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            return downstreamAssignments;
        }

        public static bool RemoveAssignment(IRAssign assignment, bool overrideProtectionCheck = false, bool overrideParameterProtection = false)
        {
            if (!assignment.IsInert)
                return false;

            if (!overrideProtectionCheck && DefinitionIsProtected(assignment.Target))
                return false;

            // Assignments which consume a parameter may only be eliminated if the parameter can also be eliminated
            // I.e. the function is a local function and all call sites have that parameter removed.
            if (IRParameter.IsOrContainsParameter(assignment.Value))
            {
                IRFunctionFragment fragment = assignment.Block.CodeComponent as IRFunctionFragment;
                if ((fragment != null &&
                    !fragment.Function.IsGlobal) ||
                    overrideParameterProtection)
                {
                    if (!overrideParameterProtection)
                    {
                        foreach (IRParameter parameter in assignment.GetOperandsWhere(op => op is IRParameter).Cast<IRParameter>())
                        {
                            if (parameter.IsResolvable)
                                continue;
                            int index = GetParameterIndex(assignment.Block.CodeComponent, parameter);
                            if (parameter.StackTransferObject is StackTransferPhi stackTransferPhi)
                                stackTransferPhi.RemoveReference(parameter);
                            parameter.StackTransferObject = null;
                            foreach (IRCall call in fragment.Function.CallSites)
                            {
                                if (call.Arguments.Count > index)
                                    call.Arguments.RemoveAt(index);
                            }
                        }
                    }

                    // If the assignment is the first in a block and the incoming parameter
                    // has a default value, we should remove that branch/push.
                    // Note that this may be broken if multiple optional parameters are involved
                    // in the assignment that was removed. This should only be possible when the
                    // IR is modified since regular code is 'store var <- parameter'.
                    if (assignment.Block.Instructions.FirstOrDefault() == assignment &&
                        assignment.Block.Dominator.Continuation is BranchContinuation branch &&
                        branch.Condition is IRNonVarPush testArg &&
                        testArg.Operation is OpcodeTestArgBottom)
                    {
                        assignment.Block.Dominator.Continuation = new JumpContinuation(assignment.Block, branch.SourceLine, branch.SourceColumn);
                        branch.True.Continuation = null;
                        branch.True.IsExecutable = false;
                    }
                }
                else
                    return false;
            }

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
            // We've already assured no inadvertent side effects of this in SCCPWithTypePropagation.DefinitionIsProtected() or equivalent.
            foreach (IRUnset unset in definition.ReplacedBy.
                Where(ssaDef => ssaDef.State == SSADefinition.SetState.Unset).
                Select(ssaDef => ssaDef.AssignedAt).Cast<IRUnset>())
            {
                unset.Block.Instructions.Remove(unset);
            }

            // Convert subsequent assignments to be declarative
            HashSet<SSASetDefinition> assignmentsToUpdate = GetDownstreamAssignments(definition);
            foreach (IRAssign nextAssign in assignmentsToUpdate.Select(ssaDef => ssaDef.DefinedAt))
            {
                nextAssign.Scope = IRAssign.StoreScope.Local;
                nextAssign.AssertExists = false;
            }

            foreach (SSADefinition replaced in definition.Replaces)
            {
                replaced.ReplacedBy.Remove(definition);
            }
            foreach (SSADefinition replacedBy in definition.ReplacedBy)
            {
                replacedBy.Replaces.Remove(definition);
                foreach (SSADefinition replaced in definition.Replaces)
                {
                    replaced.ReplacedBy.Add(replacedBy);
                    replacedBy.Replaces.Add(replaced);
                }
            }

            // Remove the definition from IncomingVariables
            foreach (BasicBlock b in assignment.Block.CodeComponent.Blocks)
            {
                HashSet<ScopeSlot> itemsToRemove = new HashSet<ScopeSlot>(
                    b.IncomingVariableDefinitions.Where(kvp => kvp.Value == assignment.Target).Select(kvp => kvp.Key)
                    );
                foreach (ScopeSlot key in itemsToRemove)
                {
                    b.IncomingVariableDefinitions.Remove(key);
                    if (definition.Replaces.Count == 1)
                    {
                        b.IncomingVariableDefinitions[key] = definition.Replaces.First();
                    }
                    else if (definition.Replaces.Count > 1)
                        throw new InvalidOperationException();
                }
            }

            definition.ReplacedBy.Clear();
            definition.Replaces.Clear();

            return true;
        }

        private static int GetParameterIndex(CodeComponent codeComponent, IRParameter parameter)
        {
            int index = 0;
            foreach (BasicBlock block in BasicBlock.GetReversePostOrder(codeComponent.RootBlock, BasicBlock.GetSuccessors))
            {
                bool breaking = false;
                foreach (IOperandInstructionBase instruction in block.Instructions.DepthFirst())
                {
                    instruction.ForEachOperand(op =>
                    {
                        if (op == parameter)
                            breaking = true;
                        if (breaking)
                            return;
                        if (op is IRParameter param &&
                            param.StackTransferObject is IRPushStack pushStack &&
                            pushStack.Value == null)  // Exclude ternary operands / linked items - we only need external.
                            index++;
                    });
                }
                if (breaking)
                    break;
            }
            return index;
        }

        public readonly struct ScopeSlot
        {
            public string Name { get; }
            public IRScope Scope { get; }
            public ScopeSlot(string variableName, IRScope scope)
            {
                Name = variableName;
                Scope = scope;
            }
            public static implicit operator (string VariableName, IRScope Scope)(ScopeSlot scopeSlot)
                => (scopeSlot.Name, scopeSlot.Scope);
            public static explicit operator ScopeSlot((string VariableName, IRScope Scope) value)
                => new ScopeSlot(value.VariableName, value.Scope);
        }

        private readonly struct SSAContext
        {
            public Dictionary<ScopeSlot, SSADefinition> Variables { get; }
            public HashSet<ScopeSlot> ReadBlacklist { get; }
            public Dictionary<ScopeSlot, IRUnset> WriteBlacklist { get; }
            public SSAContext(Dictionary<ScopeSlot, SSADefinition> variables, HashSet<ScopeSlot> readBlacklist, Dictionary<ScopeSlot, IRUnset> writeBlacklist)
            {
                Variables = variables;
                ReadBlacklist = readBlacklist;
                WriteBlacklist = writeBlacklist;
            }
        }
    }
}
