using kOS.Safe.Compilation.IR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace kOS.Safe.Compilation.Optimization.Passes
{
    public class ConstantPropagation : IHolisticOptimizationPass
    {
        public OptimizationLevel OptimizationLevel => OptimizationLevel.Minimal;

        public short SortIndex => 20;

        public void ApplyPass(IRCodePart codePart)
        {
            Dictionary<string, HashSet<IRVariableBase>> funcExternalWrites = new Dictionary<string, HashSet<IRVariableBase>>();
            Dictionary<string, HashSet<IRVariableBase>> funcExternalReads = new Dictionary<string, HashSet<IRVariableBase>>();

            // For each function, find the sets of non-local variables read and those written to.
            foreach (IRCodePart.IRFunction function in codePart.Functions)
            {
                HashSet<IRVariableBase> externalWrites = new HashSet<IRVariableBase>();
                HashSet<IRVariableBase> externalReads = new HashSet<IRVariableBase>();
                foreach (IRCodePart.IRFunction.IRFunctionFragment fragment in function.Fragments)
                {
                    AnalyzeFunction(fragment.FunctionCode, externalWrites, externalReads);
                }
                funcExternalWrites[function.Identifier] = externalWrites;
                funcExternalReads[function.Identifier] = externalReads;
            }
            foreach (IRCodePart.IRTrigger trigger in codePart.Triggers)
            {
                HashSet<IRVariableBase> externalWrites = new HashSet<IRVariableBase>();
                HashSet<IRVariableBase> externalReads = new HashSet<IRVariableBase>();
                AnalyzeFunction(trigger.Code, externalWrites, externalReads);
                funcExternalWrites[trigger.Identifier] = externalWrites;
                funcExternalReads[trigger.Identifier] = externalReads;
            }

            Dictionary<IRScope, HashSet<IRVariableBase>> variablesPropagated = new Dictionary<IRScope, HashSet<IRVariableBase>>();
            Dictionary<IRScope, HashSet<IRVariableBase>> variablesUnpropagated = new Dictionary<IRScope, HashSet<IRVariableBase>>();

            Dictionary<BasicBlock, BlockDictionaries> inOutData = new Dictionary<BasicBlock, BlockDictionaries>();
            Dictionary<IRVariableBase, string> storedFuncs = new Dictionary<IRVariableBase, string>();

            foreach (BasicBlock rootBlock in codePart.RootBlocks)
            {
                Dictionary<BasicBlock, BlockDictionaries> map = MapIncoming(rootBlock,
                    funcExternalWrites,
                    out Dictionary<IRVariableBase, string> storedFunctions);
                foreach (KeyValuePair<BasicBlock, BlockDictionaries> pair in map)
                    inOutData.Add(pair.Key, pair.Value);
                foreach (KeyValuePair<IRVariableBase, string> pair in storedFunctions)
                    storedFuncs.Add(pair.Key, pair.Value);
            }

            foreach (BasicBlock block in codePart.Blocks)
            {
                if (block.Scope.IsGlobalScope)
                    PropagateWithinBlock(block, inOutData,
                        storedFuncs,
                        new HashSet<IRVariableBase>(),
                        variablesUnpropagated,
                        funcExternalReads,
                        funcExternalWrites);
                else
                {
                    if (!variablesPropagated.ContainsKey(block.Scope))
                    {
                        variablesPropagated.Add(block.Scope, new HashSet<IRVariableBase>());
                        variablesUnpropagated.Add(block.Scope, new HashSet<IRVariableBase>());
                    }
                    PropagateWithinBlock(block, inOutData,
                        storedFuncs,
                        variablesPropagated[block.Scope],
                        variablesUnpropagated,
                        funcExternalReads,
                        funcExternalWrites);
                }
            }

            foreach (IRScope scope in variablesPropagated.Keys)
            {
                if (variablesUnpropagated.ContainsKey(scope))
                    variablesPropagated[scope].ExceptWith(variablesUnpropagated[scope]);
                foreach (IRVariableBase variable in variablesPropagated[scope])
                    scope.ClearVariable(variable);
            }
        }

        private static void AnalyzeFunction(List<BasicBlock> functionCode, HashSet<IRVariableBase> writes, HashSet<IRVariableBase> reads)
        {
            if (functionCode.Any())
                reads.UnionWith(functionCode.First().Scope.GetGlobalScope().Variables);
            foreach (BasicBlock block in functionCode)
            {
                foreach (IRInstruction instruction in block.Instructions)
                {
                    if (instruction is IRAssign assignment &&
                        assignment.Target.Scope.IsGlobalScope)
                        writes.Add(assignment.Target);
                }
            }
        }

        private static void RescopeFunctionVars(HashSet<IRVariableBase> variables, IRScope scope)
        {
            HashSet<IRVariableBase> temp = new HashSet<IRVariableBase>();
            foreach(IRVariableBase variable in variables)
            {
                if (scope.IsVariableInScope(variable.Name))
                    temp.Add(scope.GetVariableNamed(variable.Name));
                else
                    temp.Add(variable);
            }
            variables.Clear();
            variables.UnionWith(temp);
        }

        private static Dictionary<BasicBlock, BlockDictionaries> MapIncoming(BasicBlock rootBlock,
            Dictionary<string, HashSet<IRVariableBase>> funcExternalWrites,
            out Dictionary<IRVariableBase, string> storedFunctions)
        {
            storedFunctions = new Dictionary<IRVariableBase, string>();

            HashSet<T> GetValueOrDefault<T>(Dictionary<BasicBlock, HashSet<T>> dict, BasicBlock key, IEqualityComparer<T> comparer)
            {
                if (!dict.ContainsKey(key))
                {
                    if (comparer == null)
                        comparer = EqualityComparer<T>.Default;
                    HashSet<T> result = new HashSet<T>(comparer);
                    dict.Add(key, result);
                    return result;
                }
                return dict[key];
            }

            Dictionary<BasicBlock, HashSet<(IRVariableBase variable, IRAssign value)>> incoming = new Dictionary<BasicBlock, HashSet<(IRVariableBase variable, IRAssign value)>>();
            Dictionary<BasicBlock, HashSet<IRVariableBase>> blacklistIn = new Dictionary<BasicBlock, HashSet<IRVariableBase>>();

            Dictionary<BasicBlock, HashSet<(IRVariableBase variable, IRAssign value)>> outgoing = new Dictionary<BasicBlock, HashSet<(IRVariableBase variable, IRAssign value)>>();
            Dictionary<BasicBlock, HashSet<IRVariableBase>> blacklistOut = new Dictionary<BasicBlock, HashSet<IRVariableBase>>();
            Dictionary<BasicBlock, HashSet<(IRVariableBase variable, IRAssign value)>> defs = new Dictionary<BasicBlock, HashSet<(IRVariableBase variable, IRAssign value)>>();

            HashSet<BasicBlock> basicBlocks = new HashSet<BasicBlock>();
            Queue<BasicBlock> worklist = new Queue<BasicBlock>();
            worklist.Enqueue(rootBlock);

            while (worklist.Count > 0)
            {
                BasicBlock block = worklist.Dequeue();
                basicBlocks.Add(block);

                if (!defs.ContainsKey(block))
                {
                    ParseDefs(block,
                        out Dictionary<IRVariableBase, IRAssign> defsMade,
                        GetValueOrDefault(blacklistOut, block, null),
                        storedFunctions,
                        funcExternalWrites);
                    defs[block] = new HashSet<(IRVariableBase variable, IRAssign value)>(defsMade.Select(kvp => (kvp.Key, kvp.Value)), VariableTupleEqualityComparer.Instance);
                }

                incoming.Remove(block);
                HashSet<(IRVariableBase variable, IRAssign value)> defsIn = GetValueOrDefault(incoming, block, VariableTupleEqualityComparer.Instance);
                blacklistIn.Remove(block);
                HashSet<IRVariableBase> localBlacklist = GetValueOrDefault(blacklistIn, block, null);
                BasicBlock firstPredecessor = block.Predecessors.FirstOrDefault();
                if (firstPredecessor != null)
                {
                    defsIn.UnionWith(GetValueOrDefault(outgoing, firstPredecessor, VariableTupleEqualityComparer.Instance));
                    localBlacklist.UnionWith(GetValueOrDefault(blacklistOut, firstPredecessor, null));
                }

                foreach (BasicBlock predecessor in block.Predecessors.Skip(1))
                {
                    if (!outgoing.ContainsKey(predecessor))
                        continue;
                    defsIn.IntersectWith(GetValueOrDefault(outgoing, predecessor, VariableTupleEqualityComparer.Instance));
                    localBlacklist.UnionWith(GetValueOrDefault(blacklistOut, predecessor, null));
                }

                defsIn.RemoveWhere(vv => !(vv.variable.Scope == block.Scope || block.Scope.IsEncompassedBy(vv.variable.Scope)));
                defsIn.RemoveWhere(vv => localBlacklist.Contains(vv.variable));

                HashSet<(IRVariableBase variable, IRAssign value)> defsOut = new HashSet<(IRVariableBase variable, IRAssign value)>(defsIn, VariableTupleEqualityComparer.Instance);
                HashSet<(IRVariableBase variable, IRAssign value)> definitions = defs[block];

                defsOut.UnionWith(defsIn);
                foreach (var def in definitions)
                {
                    defsOut.RemoveWhere(vv => vv.variable.Equals(def.variable));
                    defsOut.Add(def);
                }
                defsOut.RemoveWhere(vv => localBlacklist.Contains(vv.variable));

                if (outgoing.ContainsKey(block) && defsOut.SetEquals(outgoing[block]))
                    continue;

                outgoing[block] = defsOut;
                blacklistOut[block].UnionWith(localBlacklist);
                foreach (BasicBlock successor in block.Successors)
                    worklist.Enqueue(successor);
            }

            Dictionary<BasicBlock, BlockDictionaries> map = new Dictionary<BasicBlock, BlockDictionaries>();
            foreach (BasicBlock block in basicBlocks)
                map.Add(block, new BlockDictionaries(incoming[block], blacklistIn[block]));

            return map;
        }

        private static void ParseDefs(BasicBlock block,
            out Dictionary<IRVariableBase, IRAssign> defs,
            HashSet<IRVariableBase> blacklist,
            Dictionary<IRVariableBase, string> storedFunctions,
            Dictionary<string, HashSet<IRVariableBase>> funcExternalWrites)
        {
            defs = new Dictionary<IRVariableBase, IRAssign>();
            List<IRInstruction> instructions = block.Instructions;
            for (int i = 0; i < block.Instructions.Count; i++)
            {
                IRInstruction instruction = instructions[i];
                // Step through instructions (depth first) and replace variables
                // with their cached constant whenever they're available.
                foreach (IRInstruction nestedInstruction in instruction.DepthFirst())
                {
                    RemoveVarsWrittenInFunction(nestedInstruction,
                        block.Scope,
                        defs,
                        storedFunctions,
                        funcExternalWrites);
                }

                switch (instruction)
                {
                    // On a local assignment by a constant, cache that constant against the variable.
                    case IRAssign assignment:
                        if (!blacklist.Contains(assignment.Target) &&
                            !assignment.Target.Scope.IsGlobalScope)
                        {
                            defs[assignment.Target] = assignment;
                        }

                        // On encountering a PushRelocateLater, note the scope for its closure variables.
                        if (assignment.Value is IRRelocateLater lockOrFunctionPointer)
                        {
                            string pointer = ((string)lockOrFunctionPointer.Value).Split('-').First();
                            storedFunctions[assignment.Target] = pointer;
                            // Re-scope the stored variables from Global to the current scope.
                            RescopeFunctionVars(funcExternalWrites[pointer], block.Scope);
                        }
                        break;

                    // On encountering a trigger:
                    //      Clear the cache of any variables that are written in that trigger or body.
                    //      Blacklist any variables that are written in the trigger or body.
                    case IRUnaryConsumer trigger:
                        if (trigger.Operation is OpcodeAddTrigger)
                        {
                            string destination = (string)((IRConstant)trigger.Operand).Value;
                            if (funcExternalWrites.ContainsKey(destination))
                            {
                                foreach (IRVariableBase variable in funcExternalWrites[destination])
                                    defs.Remove(variable);

                                // Re-scope the stored variables from Global to the current scope.
                                RescopeFunctionVars(funcExternalWrites[destination], block.Scope);
                                blacklist.UnionWith(funcExternalWrites[destination]);
                            }
                        }
                        break;
                }
            }
        }

        private static void RemoveVarsWrittenInFunction(IRInstruction instr,
            IRScope scope,
            Dictionary<IRVariableBase, IRAssign> defsCreated,
            Dictionary<IRVariableBase, string> storedFunctions,
            Dictionary<string, HashSet<IRVariableBase>> funcExternalWrites)
        {
            if (instr is IRCall call)
            {
                // On encountering a Call, where that identifier is in our dictionary of function closure variables.
                //      Inject the assignment call to any constant variables that are read inside the function.
                //      Clear the cache of any variables that are written in that function.
                IRVariableBase functionCall = scope.GetVariableNamed(call.Function);
                if (functionCall != null && storedFunctions.ContainsKey(functionCall))
                {
                    string funcRef = storedFunctions[functionCall];

                    foreach (IRVariableBase variable in funcExternalWrites[funcRef])
                        defsCreated.Remove(variable);
                }
            }
        }

        private static void PropagateWithinBlock(BasicBlock block,
            Dictionary<BasicBlock, BlockDictionaries> inOutData,
            Dictionary<IRVariableBase, string> storedFunctions,
            HashSet<IRVariableBase> scopeVarsPropagated,
            Dictionary<IRScope, HashSet<IRVariableBase>> variablesUnpropagated,
            Dictionary<string, HashSet<IRVariableBase>> externalVariablesRead,
            Dictionary<string, HashSet<IRVariableBase>> externalVariablesWritten)
        {
            BlockDictionaries localInOut = inOutData[block];

            List<IRInstruction> instructions = block.Instructions;

            inOutData[block].InitializeForPropagation(
                out Dictionary<IRVariableBase, IRValue> constantCache,
                out Dictionary<IRVariableBase, IRAssign> assignmentInstructions,
                out HashSet<IRVariableBase> blacklist);

            for (int i = 0; i < instructions.Count; i++)
            {
                IRInstruction instruction = instructions[i];
                // Step through instructions (depth first) and replace variables
                // with their cached constant whenever they're available.
                foreach (IRInstruction nestedInstruction in instruction.DepthFirst())
                {
                    AttemptPropagation(nestedInstruction,
                        instructions,
                        ref i,
                        block.Scope,
                        constantCache,
                        storedFunctions,
                        externalVariablesRead,
                        externalVariablesWritten,assignmentInstructions,
                        variablesUnpropagated);
                }

                switch (instruction)
                {
                    // On a local assignment by a constant, cache that constant against the variable.
                    case IRAssign assignment:
                        if (!blacklist.Contains(assignment.Target) &&
                            !assignment.Target.Scope.IsGlobalScope)
                        {
                            if (assignment.Value is IRTemp temp)
                                assignment.Value = ConstantFolding.AttemptReduction(temp.Parent);
                            if (assignment.Value is IRConstant constantValue)
                            {
                                scopeVarsPropagated.Add(assignment.Target);
                                instructions.RemoveAt(i);
                                assignmentInstructions[assignment.Target] = assignment;
                                constantCache[assignment.Target] = constantValue;
                                i--;
                            }
                            else
                            {
                                scopeVarsPropagated.Remove(assignment.Target);
                                assignmentInstructions.Remove(assignment.Target);
                                constantCache.Remove(assignment.Target);
                            }
                        }

                        // On encountering a PushDelegateRelocateLater, note which variables are cached.
                        //      Store the intersections of that set and the function variable sets.
                        else if (assignment.Value is IRDelegateRelocateLater functionPointer)
                        {
                            string pointer = ((string)functionPointer.Value).Split('-').First();
                            storedFunctions[assignment.Target] = pointer;
                            // Re-scope the stored variables from Global to the current scope.
                            // Writes were accomplished on the first pass.
                            RescopeFunctionVars(externalVariablesRead[pointer], block.Scope);
                        }

                        // On encountering a Lock call, we will::
                        //      Inject the assignment call to any constant variables that are read inside the function.
                        //      Clear the cache of any variables that are written in that lock.
                        else if (assignment.Value is IRRelocateLater lockPointer)
                        {
                            string pointer = ((string)lockPointer.Value).Split('-').First();
                            storedFunctions[assignment.Target] = pointer;
                            // Re-scope the stored variables from Global to the current scope.
                            // Writes were accomplished on the first pass.
                            RescopeFunctionVars(externalVariablesRead[pointer], block.Scope);
                        }
                        break;

                    // On encountering a trigger:
                    //      Inject the assignment call to any constant variables that are read inside the trigger or body.
                    //      Clear the cache of any variables that are written in that trigger or body.
                    //      Blacklist any variables that are written in the trigger or body.
                    case IRUnaryConsumer trigger:
                        if (trigger.Operation is OpcodeAddTrigger)
                        {
                            string destination = (string)((IRConstant)trigger.Operand).Value;
                            if (externalVariablesRead.ContainsKey(destination))
                            {
                                foreach (IRVariableBase variable in externalVariablesRead[destination].Union(
                                    externalVariablesWritten[destination]))
                                    if (assignmentInstructions.ContainsKey(variable))
                                    {
                                        instructions.Insert(i++, assignmentInstructions[variable]);
                                        assignmentInstructions.Remove(variable);
                                        variablesUnpropagated[variable.Scope].Add(variable);
                                    }

                                foreach (IRVariableBase variable in externalVariablesWritten[destination])
                                    constantCache.Remove(variable);

                                blacklist.UnionWith(externalVariablesWritten[destination]);
                            }
                        }
                        break;
                }
            }
        }

        private static void AttemptPropagation(IRInstruction instruction,
            List<IRInstruction> instructions,
            ref int i,
            IRScope scope,
            Dictionary<IRVariableBase, IRValue> constantCache,
            Dictionary<IRVariableBase, string> storedFunctions,
            Dictionary<string, HashSet<IRVariableBase>> funcExternalReads,
            Dictionary<string, HashSet<IRVariableBase>> funcExternalWrites,
            Dictionary<IRVariableBase, IRAssign> assignmentInstructions,
            Dictionary<IRScope, HashSet<IRVariableBase>> variablesUnpropagated)
        {
            if (instruction is IRCall call)
            {
                // On encountering a Call, where that identifier is in our dictionary of function closure variables.
                //      Inject the assignment call to any constant variables that are read inside the function.
                //      Clear the cache of any variables that are written in that function.
                IRVariableBase functionCall = scope.GetVariableNamed(call.Function);
                if (functionCall != null && storedFunctions.ContainsKey(functionCall))
                {
                    string funcRef = storedFunctions[functionCall];
                    foreach (IRVariableBase variable in funcExternalReads[funcRef].Union(
                            funcExternalWrites[funcRef]))
                        if (assignmentInstructions.ContainsKey(variable))
                        {
                            instructions.Insert(i++, assignmentInstructions[variable]);
                            assignmentInstructions.Remove(variable);
                            variablesUnpropagated[variable.Scope].Add(variable);
                        }

                    foreach (IRVariableBase variable in funcExternalWrites[funcRef])
                        constantCache.Remove(variable);
                }
            }
            if (instruction is ISingleOperandInstruction singleOperandInstruction)
            {
                if (singleOperandInstruction.Operand is IRVariable variable &&
                    constantCache.ContainsKey(variable) &&
                    constantCache[variable] is IRConstant)
                {
                    singleOperandInstruction.Operand = constantCache[variable];
                }
            }
            else if (instruction is IMultipleOperandInstruction multipleOperandInstruction)
            {
                for (int j = multipleOperandInstruction.OperandCount - 1; j >= 0; j--)
                {
                    if (multipleOperandInstruction[j] is IRVariable variable &&
                        constantCache.ContainsKey(variable) &&
                        constantCache[variable] is IRConstant)
                    {
                        multipleOperandInstruction[j] = constantCache[variable];
                    }
                }
            }
        }

        private class BlockDictionaries
        {
            public HashSet<(IRVariableBase IRVariableBase, IRAssign value)> incomingDefinitions;
            public HashSet<IRVariableBase> blacklist;
            public BlockDictionaries(
                HashSet<(IRVariableBase, IRAssign)> incomingDefinitions,
                HashSet<IRVariableBase> blacklist)
            {
                this.incomingDefinitions = incomingDefinitions;
                this.blacklist = blacklist;
            }
            public void InitializeForPropagation(
                out Dictionary<IRVariableBase, IRValue> constantCache,
                out Dictionary<IRVariableBase, IRAssign> assignments,
                out HashSet<IRVariableBase> blacklist)
            {
                constantCache = new Dictionary<IRVariableBase, IRValue>();
                assignments = new Dictionary<IRVariableBase, IRAssign>();
                foreach ((IRVariableBase variable, IRAssign value) in incomingDefinitions)
                {
                    constantCache.Add(variable, value.Value);
                    assignments.Add(variable, value);
                }
                blacklist = new HashSet<IRVariableBase>(this.blacklist);
            }
        }

        private class VariableTupleEqualityComparer : IEqualityComparer<(IRVariableBase variable, IRAssign value)>
        {
            public static VariableTupleEqualityComparer Instance { get; } = new VariableTupleEqualityComparer();
            public bool Equals((IRVariableBase variable, IRAssign value) x, (IRVariableBase variable, IRAssign value) y)
                => x.variable.Equals(y.variable) && (x.value.Value.Equals(y.value.Value));

            public int GetHashCode((IRVariableBase variable, IRAssign value) obj)
                => obj.variable.GetHashCode();
        }
    }
}
