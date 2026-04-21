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
    public static class SingleStaticAssignment
    {
        private static readonly Dictionary<IRCall, IEnumerable<SSAVariable>> postCallSSAVariables =
            new Dictionary<IRCall, IEnumerable<SSAVariable>>();
        private static readonly Dictionary<SSAVariable, SSAVariable> postCallSSAVariableLinks =
            new Dictionary<SSAVariable, SSAVariable>();

        /// <summary>
        /// Finalizes a program into single static assignment form.
        /// </summary>
        public static void FinalizeSSA(IRCodePart codePart)
        {
            foreach (BasicBlock block in codePart.Blocks)
            {
                AnalyzeBlock(block, codePart);
            }

            foreach (BasicBlock root in codePart.RootBlocks)
            {
                BuildPhis(root);
            }

            foreach (BasicBlock block in codePart.Blocks)
            {
                ApplyUses(block, codePart.Triggers);
            }
        }

        private static void AnalyzeBlock(BasicBlock block, IRCodePart codePart)
        {
            HashSet<SSAVariable> variables = new HashSet<SSAVariable>();
            List<IRInstruction> instructions = block.Instructions;
            for (int i = 0; i < instructions.Count; i++)
            {
                IRInstruction instruction = instructions[i];

                foreach (IRCall call in instruction.DepthFirst().Where(inst => inst is IRCall).Cast<IRCall>())
                {
                    string functionIdentifier = block.Scope.GetFunctionNameFromVariable(call.Function);
                    IRCodePart.IRFunction function = codePart.GetFunction(functionIdentifier);
                    if (function != null)
                    {
                        HashSet<SSAVariable> ssaVariables = new HashSet<SSAVariable>();
                        foreach (SSAVariable variable in function.ExternalWrites)
                        {
                            SSAVariable ssaVariable = variable.GetNewSSAVariable();
                            postCallSSAVariableLinks.Add(variable, ssaVariable);
                            OverwriteVariable(variables, ssaVariable);
                            ssaVariables.Add(ssaVariable);
                        }
                        if (postCallSSAVariables.TryGetValue(call, out IEnumerable<SSAVariable> storedVariables))
                            ssaVariables.UnionWith(storedVariables);
                        postCallSSAVariables[call] = ssaVariables;
                    }
                }

                switch (instruction)
                {
                    case IRAssign assignment:
                        OverwriteVariable(variables, (SSAVariable)assignment.Target);
                        // On encountering a PushRelocateLater, note the scope for its closure variables.
                        if (assignment.Value is IRRelocateLater lockOrFunctionPointer)
                        {
                            string pointer = ((string)lockOrFunctionPointer.Value).Split('-').First();
                            // Re-scope the stored variables from Global to the current scope.
                            IRCodePart.IRFunction function = codePart.GetFunction(pointer);
                            if (function != null)
                                IRCodePart.SetDefiningScope(function, block.Scope);
                        }
                        break;

                    case IRUnaryConsumer unaryConsumer:
                        // On encountering a trigger:
                        //      Clear the cache of any variables that are written in that trigger or body.
                        //      Blacklist any variables that are written in the trigger or body.
                        if (unaryConsumer.Operation is OpcodeAddTrigger)
                        {
                            string pointer = (string)((IRConstant)unaryConsumer.Operand).Value;
                            // Re-scope the stored variables from Global to the current scope.
                            IRCodePart.IRTrigger trigger = codePart.GetTrigger(pointer);
                            if (trigger != null)
                                IRCodePart.SetDefiningScope(trigger, block.Scope);
                            block.TriggerPropagationBlacklist.UnionWith(trigger.ExternalWrites);
                        }
                        // On encountering an unset operation:
                        //      Remove the affected definition from the stored variables.
                        // If the affected definition cannot be determined, clear all current variables
                        if (unaryConsumer.Operation is OpcodeUnset)
                        {
                            if (!unaryConsumer.IsInvariant)
                            {
                                variables.Clear();
                            }
                            else
                            {
                                IRConstant unsetConst = unaryConsumer.Operand as IRConstant;
                                if (unsetConst == null && unaryConsumer.Operand is IRTemp temp)
                                    unsetConst = Optimization.Passes.ConstantFolding.AttemptReduction(temp.Parent) as IRConstant;
                                if (unsetConst != null &&
                                    (unsetConst.Value is Encapsulation.StringValue ||
                                    unsetConst.Value is string))
                                {
                                    /*IRVariableBase unsetVar = block.Scope.GetVariableNamed(
                                        (Encapsulation.StringValue)unsetConst.Value);
                                    if (unsetVar != null)
                                        variables.RemoveWhere(v => v.Parent.Equals(unsetVar));*/
                                    variables.RemoveWhere(v =>
                                        v.Name.Equals((Encapsulation.StringValue)unsetConst.Value,
                                        StringComparison.OrdinalIgnoreCase));
                                }
                                else
                                    // TODO: See if this can be optimized through the SCCP pass.
                                    // Probably not worth it since unsets are rather rare,
                                    // and unsets without a string const even more so.
                                    variables.Clear();
                            }
                        }
                        break;
                }
            }

            block.VariablesWritten.Clear();
            block.VariablesWritten.UnionWith(variables);
        }

        private static void BuildPhis(BasicBlock root)
        {
            Dictionary<BasicBlock, HashSet<SSAVariable>> variablesOut = new Dictionary<BasicBlock, HashSet<SSAVariable>>();

            Queue<BasicBlock> worklist = new Queue<BasicBlock>();
            worklist.Enqueue(root);
            while (worklist.Count > 0)
            {
                BasicBlock block = worklist.Dequeue();

                HashSet<IRVariable> blacklist = block.TriggerPropagationBlacklist;
                List<(BasicBlock block, SSAVariable variable)> varsIn = new List<(BasicBlock block, SSAVariable variable)>();

                foreach (BasicBlock predecessor in block.Predecessors)
                {
                    blacklist.UnionWith(predecessor.TriggerPropagationBlacklist);
                    if (variablesOut.TryGetValue(predecessor, out HashSet<SSAVariable> predVarsOut))
                    {
                        foreach (SSAVariable variable in predVarsOut
                            .Where(v => block.Scope.IsEqualOrEncompassedBy(v.Scope)))
                            varsIn.Add((predecessor, variable));
                    }
                }

                HashSet<SSAVariable> variablesIn = new HashSet<SSAVariable>();
                foreach (IGrouping<IRVariable, (BasicBlock block, SSAVariable variable)> definition in
                    varsIn.GroupBy(bv => bv.variable.Parent))
                {
                    if (definition.Select(bv => bv.variable).Distinct().Skip(1).Any())
                    {
                        // Phi required
                        PhiVariable phiVar = block.Phis.FirstOrDefault(v => v.Parent.Equals(definition.Key));
                        if (phiVar == null)
                        {
                            phiVar = definition.Key.GetNewPhiVariable();
                            block.Phis.Add(phiVar);
                        }

                        foreach ((BasicBlock block, SSAVariable variable) def in definition)
                            phiVar.PossibleValues[def.block] = def.variable;

                        variablesIn.Add(phiVar);
                    }
                    else
                    {
                        block.Phis.RemoveWhere(v => v.Parent.Equals(definition.Key));
                        variablesIn.Add(definition.First().variable);
                    }
                }

                block.IncomingVariableDefinitions = variablesIn;

                HashSet<SSAVariable> varsOut = new HashSet<SSAVariable>();
                varsOut.UnionWith(variablesIn);

                foreach (SSAVariable variable in block.VariablesWritten.Where(v => !v.Scope.IsGlobalScope))
                    OverwriteVariable(varsOut, variable);

                if (variablesOut.ContainsKey(block) && variablesOut[block].SetEquals(varsOut))
                    continue;

                variablesOut[block] = varsOut;

                foreach (BasicBlock successor in block.Successors)
                    worklist.Enqueue(successor);
            }
        }

        private static void ApplyUses(BasicBlock block, List<IRCodePart.IRTrigger> triggers)
        {
            List<IRInstruction> instructions = block.Instructions;
            Dictionary<IRVariable, SSAVariable> liveDefinitions = new Dictionary<IRVariable, SSAVariable>();

            HashSet<IRVariable> triggerBlacklist = new HashSet<IRVariable>();
            foreach (BasicBlock predecessor in block.Predecessors)
                triggerBlacklist.UnionWith(predecessor.TriggerPropagationBlacklist);

            foreach (SSAVariable variable in block.IncomingVariableDefinitions)
                liveDefinitions[variable.Parent] = variable;

            for (int i = 0; i < instructions.Count; i++)
            {
                IRInstruction instruction = instructions[i];
                foreach (IRInstruction inst in instruction.DepthFirst())
                {
                    if (inst is IRUnaryConsumer triggerInstruction &&
                        triggerInstruction.Operation is OpcodeAddTrigger)
                    {
                        string pointer = (string)((IRConstant)triggerInstruction.Operand).Value;
                        IRCodePart.IRTrigger trigger = triggers.FirstOrDefault(t => string.Equals(t.Identifier, pointer, StringComparison.OrdinalIgnoreCase));
                        
                        triggerBlacklist.UnionWith(trigger.ExternalWrites);

                        foreach (IRVariable variable in trigger.ExternalWrites)
                            liveDefinitions.Remove(variable);
                    }
                    else if (inst is IRAssign assignment &&
                        assignment.Target is SSAVariable ssaVariable)
                    {
                        if (!triggerBlacklist.Contains(ssaVariable.Parent))
                            liveDefinitions[ssaVariable.Parent] = ssaVariable;
                        else
                            liveDefinitions.Remove(ssaVariable.Parent);
                    }
                    else if (inst is IRCall call)
                    {
                        if (postCallSSAVariables.ContainsKey(call))
                        {
                            foreach (SSAVariable variable in postCallSSAVariables[call])
                            {
                                if (!triggerBlacklist.Contains(variable.Parent))
                                    liveDefinitions[variable.Parent] = variable;
                                else
                                    liveDefinitions.Remove(variable.Parent);
                            }
                        }
                    }
                    if (inst is IOperandInstructionBase operandInstruction)
                    {
                        operandInstruction.MutateEachOperand(op =>
                        {
                            if (op is IRVariable variable &&
                            !triggerBlacklist.Contains(variable) &&
                            liveDefinitions.TryGetValue(variable, out SSAVariable currentValue))
                                return currentValue;
                            return op;
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Overwrites an SSA variable with the latest assignment.
        /// </summary>
        /// <param name="variables">The active variables.</param>
        /// <param name="newVariable">The new variable as assigned.</param>
        public static void OverwriteVariable(HashSet<SSAVariable> variables, SSAVariable newVariable)
        {
            variables.RemoveWhere(v => v.Parent == newVariable.Parent);
            variables.Add(newVariable);
        }
    }
}
