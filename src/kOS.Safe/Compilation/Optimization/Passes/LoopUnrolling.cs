using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Compilation.IR;

namespace kOS.Safe.Compilation.Optimization.Passes
{
    public class LoopUnrolling : IOptimizationPass<ICodeComponent>, ILinkedOptimizationPass
    {
        public OptimizationLevel OptimizationLevel => OptimizationLevel.Extreme;

        public short SortIndex => 4000;
        public Optimizer Optimizer { get; set; }

        private const int maxUnrolledSize = 100 * 10;

        public void ApplyPass(IEnumerable<ICodeComponent> codeComponents)
        {
            foreach (ICodeComponent component in codeComponents)
                ApplyPass(component.RootBlock);
        }

        private void ApplyPass(BasicBlock root)
        {
            IEnumerable<BasicBlock> GetEdges(BasicBlock block)
                => block.Successors.Where(BlockOrdering.AllBlocksPredicate);

            List<BasicBlock> reversePostOrder = BasicBlock.GetReversePostOrder(root, GetEdges);
            Stack<BasicBlock> regionExits = new Stack<BasicBlock>();
            regionExits.Push(reversePostOrder[reversePostOrder.Count - 1]);

            List<BlockOrdering.LoopData> loopData = LoopConditionalRelocation.FindLoops(root, regionExits);
            // Loops are found in reverse post-order, so the list is
            // reversed to work from inside out for nested loops.
            loopData.Reverse();

            foreach (BlockOrdering.LoopData loop in loopData)
            {
                if (IsLoopConstantLength(loop, out Dictionary<string, List<Encapsulation.Structure>> indices))
                {
                    UnrollLoop(loop, indices);
                }
            }

            if (!Optimizer.PassesToSkip.Contains(typeof(SCCPWithTypePropagation)))
            {
                Dictionary<SSADefinition, HashSet<IOperandInstructionBase>> localVarUses =
                    SCCPWithTypePropagation.MapUsesAndPropagateTypes(root);
                HashSet<SSADefinition> requiredLocalDefs =
                    SCCPWithTypePropagation.PropagateConstants(localVarUses);
                foreach (BasicBlock block in root.CodeComponent.Blocks)
                    SCCPWithTypePropagation.RemoveRedundantAssignments(block, requiredLocalDefs);
            }
        }

        private static bool IsLoopConstantLength(BlockOrdering.LoopData data, out Dictionary<string, List<Encapsulation.Structure>> indices)
        {
            IInterimOperand condition = (data.branchBlock.Instructions.Last() as IRBranch).Condition;
            indices = null;
            int maxUnrollIterations = maxUnrolledSize / BasicBlock.GetOpcodeCount(data.GetBody());

            if (condition is IRSuffixGet getNext &&
                getNext.Suffix.Equals("next", StringComparison.OrdinalIgnoreCase) &&
                getNext.Object.Type == typeof(Encapsulation.Enumerator) &&
                getNext.Object is InterimResolvedReference resolvedReference &&
                resolvedReference.Reference is SSASetDefinition setDefinition &&
                setDefinition.DefinedAt.Value is IRSuffixGet getIterator &&
                getIterator.Object.Type == typeof(RangeValue))
            {
                // For loop (range()):
                RangeValue range = GetInvariantRangeObject(getIterator.Object);

                if (range == null)
                    return false;

                IRAssign iteratorAssignment = data.body.Instructions.FirstOrDefault() as IRAssign;
                if (!(iteratorAssignment?.Value is IRSuffixGet suffixGet &&
                    suffixGet.Object is InterimResolvedReference resolvedIterator &&
                    resolvedReference.Reference == resolvedIterator.Reference))
                    iteratorAssignment = null;
                string iteratorObj = iteratorAssignment?.Target.Name ?? "";

                indices = new Dictionary<string, List<Encapsulation.Structure>>
                {
                    [iteratorObj] = range.Cast<Encapsulation.Structure>().ToList()
                };

                return indices.Values.All(idx => idx.Count <= maxUnrollIterations && idx.Count > 1);
            }
            else
            {
                // Index-based loops:
                // Variables referenced in the condition must be
                // strictly known.
                List<IInterimVariableReference> variablesReferenced;
                if (condition is IOperandInstructionBase operandInstruction)
                    variablesReferenced = GetVariableReferences(operandInstruction).ToList();
                else if (condition is IInterimVariableReference variableReference)
                    variablesReferenced = new List<IInterimVariableReference>() { variableReference };
                else
                    return false;

                HashSet<SSADefinition> definitions = new HashSet<SSADefinition>(variablesReferenced.Select(r => ((InterimResolvedReference)r).Reference));

                foreach (SSADefinition definition in definitions)
                    if (!DefinitionIsAllowable(definition, data))
                        return false;

                indices = new Dictionary<string, List<Encapsulation.Structure>>();
                PopulateIndices(condition, definitions, indices, data, maxUnrollIterations);

                return indices.Values.All(idx => idx.Count <= maxUnrollIterations && idx.Count > 1);
            }
        }

        private static RangeValue GetInvariantRangeObject(IInterimOperand operand)
        {
            switch (operand)
            {
                case IRCall call:
                    if (!call.IsInvariant || !call.IsInert)
                        return null;
                    return call.Evaluate().Value as RangeValue;
                case InterimResolvedReference reference:
                    if (!(reference.Reference is SSASetDefinition definition))
                        return null;
                    return GetInvariantRangeObject(definition.DefinedAt.Value);
                case InterimUnresolvedReference _:
                case InterimVariableReference _:
                    return null;
                default:
#if DEBUG
                    throw new NotImplementedException("Range object reference type is not implemented in LoopUnrolling.cs");
#else
                    return null;
#endif
            }
        }

        private static IEnumerable<IInterimVariableReference> GetVariableReferences(IOperandInstructionBase operandInstruction)
        {
            IRInstruction instruction = (IRInstruction)operandInstruction;
            foreach (IRInstruction subInstruction in instruction.DepthFirst())
            {
                if (subInstruction is ISingleOperandInstruction singleOperandInstruction &&
                    singleOperandInstruction.Operand is IInterimVariableReference singleResult)
                    yield return singleResult;
                else if (subInstruction is IMultipleOperandInstruction multipleOperandInstruction)
                {
                    foreach (IInterimOperand op in multipleOperandInstruction.Operands)
                    {
                        if (op is IInterimVariableReference multipleResult)
                            yield return multipleResult;
                    }
                }
            }
        }

        private static bool DefinitionIsAllowable(SSADefinition definition, BlockOrdering.LoopData loopData, HashSet<SSADefinition> passed = null)
        {
            if (passed == null)
                passed = new HashSet<SSADefinition>();
            if (!passed.Add(definition))
                return true;
            if (definition.State != SSADefinition.SetState.Set)
                return false;
            switch (definition)
            {
                case SSASetDefinition setDefinition:
                    if (!loopData.BodyContains(setDefinition.DefinedAt.Block))
                        return true;
                    return GetVariableReferences(setDefinition.DefinedAt).All(r =>
                        r is InterimResolvedReference resolvedRef &&
                        DefinitionIsAllowable(resolvedRef.Reference, loopData, passed));
                case SSAPotentialDefinition potentialDefinition:
                    return false;
                case PhiVariable phi:
                    // The external one must be invariant.
                    // Any internal ones (where executable) must be allowable.
                    foreach (KeyValuePair<BasicBlock, SSADefinition> possibleValue in phi.Node.PossibleValues)
                    {
                        if (!possibleValue.Key.IsExecutable)
                            continue;
                        if (loopData.BodyContains(possibleValue.Key))
                        {
                            if (!DefinitionIsAllowable(possibleValue.Value, loopData, passed))
                                return false;
                        }
                        else
                        {
                            if (!possibleValue.Value.IsInvariant)
                                return false;
                        }
                    }
                    return true;
                default:
                    throw new NotImplementedException();
            }
        }

        private static void PopulateIndices(IInterimOperand condition, IEnumerable<SSADefinition> definitions, Dictionary<string, List<Encapsulation.Structure>> indices, BlockOrdering.LoopData loopData, int maxIterations)
        {
            Dictionary<string, IInterimOperand> incrementFuncs = new Dictionary<string, IInterimOperand>();
            Dictionary<SSADefinition, InterimConstantValue> lastValue = new Dictionary<SSADefinition, InterimConstantValue>();
            Dictionary<string, InterimConstantValue> iterators = new Dictionary<string, InterimConstantValue>();
            List<string> iteratorVariables = new List<string>();

            foreach (SSADefinition definition in definitions.Where(def => loopData.body.IncomingVariableDefinitions.Keys.Any(scopleSlot => scopleSlot.Name.Equals(def.Name, StringComparison.OrdinalIgnoreCase))))
            {
                InterimConstantValue input = (InterimConstantValue)GetFirstExternalValue(definition, loopData).Clone(null);
                indices[definition.Name] = new List<Encapsulation.Structure>() { };
                lastValue[definition] = input;
                iterators[definition.Name] = input;
                iteratorVariables.Add(definition.Name);
            }
            foreach (SSADefinition definition in definitions)
                incrementFuncs[definition.Name] = BuildIncrementFunc(definition, lastValue, loopData);

            condition = ReplaceVariableReferences(condition.Clone(null, true), lastValue, loopData);

            int iteration = 0;
            try
            {
                // Things shouldn't be able to throw an exception here,
                // but better safe than sorry.
                while (!Convert.ToBoolean(((IEvaluatableToConstant)condition).Evaluate().Value))
                {
                    if (iteration++ > maxIterations)
                        break;
                    foreach (string name in iteratorVariables)
                        indices[name].Add((Encapsulation.Structure)iterators[name].Value);
                    foreach (string name in iteratorVariables)
                        iterators[name].Value = ((IEvaluatableToConstant)incrementFuncs[name]).Evaluate().Value;
                }
            }
            catch (Exception)
            {
#if DEBUG
                // Flag for debug, fail gently in release.
                throw;
#else
                // Emit a bogus set of indices that will
                // trigger logic to not unroll the loop.
                indices["!error"] = Enumerable.Repeat<Encapsulation.Structure>(Encapsulation.BooleanValue.False, maxIterations + 1).ToList();
                return;
#endif
            }
        }

        private static InterimConstantValue GetFirstExternalValue(SSADefinition definition, BlockOrdering.LoopData loopData)
        {
            IRScope scope = loopData.body.Scope;
            SSADefinition incomingDefinition = null;
            while (scope != null)
            {
                if (loopData.body.IncomingVariableDefinitions.TryGetValue((definition.Name, scope), out incomingDefinition))
                    break;
                scope = scope.ParentScope;
            }
            switch (incomingDefinition)
            {
                case SSASetDefinition setDefinition:
                    return definition.Evaluate();
                case PhiVariable phi:
                    return phi.Node.PossibleValues.FirstOrDefault(kvp => kvp.Key.IsExecutable && !loopData.BodyContains(kvp.Key)).Value.Evaluate();
                default:
                    throw new NotImplementedException();
            }
        }
        private static IInterimOperand BuildIncrementFunc(SSADefinition definition, Dictionary<SSADefinition, InterimConstantValue> values, BlockOrdering.LoopData loopData)
        {
            IInterimOperand result = GetFirstInternalSet(definition, loopData).DefinedAt.Value.Clone(null, true);
            result = ReplaceVariableReferences(result, values, loopData);
            return result;
        }
        private static SSASetDefinition GetFirstInternalSet(SSADefinition ssaDef, BlockOrdering.LoopData loopData)
        {
            switch (ssaDef)
            {
                case SSASetDefinition setDefinition:
                    return setDefinition;
                case PhiVariable phi:
                    if (phi.Node.PossibleValues.Keys.Where(b => b.IsExecutable && loopData.BodyContains(b)).Count() > 1)
                        throw new InvalidOperationException();
                    SSADefinition result = phi.Node.PossibleValues.FirstOrDefault(kvp => kvp.Key.IsExecutable && (loopData.BodyContains(kvp.Key) || loopData.branchBlock == kvp.Key)).Value;
                    switch (result)
                    {
                        case null:
                            throw new InvalidOperationException();
                        case SSASetDefinition setDefinition:
                            return setDefinition;
                        case PhiVariable nestedPhi:
                            return GetFirstInternalSet(nestedPhi, loopData);
                        default:
                            throw new InvalidOperationException();
                    }
                default:
                    throw new InvalidOperationException();
            }
        }
        private static IInterimOperand ReplaceVariableReferences(IInterimOperand operand, Dictionary<SSADefinition, InterimConstantValue> values, BlockOrdering.LoopData loopData)
        {
            IInterimOperand ReplaceVariableReferenceMutation(IInterimOperand op)
                => ReplaceVariableReferences(op, values, loopData);

            switch (operand)
            {
                case IOperandInstructionBase operandInstruction:
                    operandInstruction.MutateEachOperand(ReplaceVariableReferenceMutation);
                    return operand;
                case InterimResolvedReference variableReference:
                    // For an external variable, return its evaluated constant as stored in values.
                    // Phis with an executable internal value are taken as the internal value.
                    // For an internal variable, return a reference to its InterimConstantValue object from values.
                    if (values.TryGetValue(GetFirstInternalSet(variableReference.Reference, loopData), out InterimConstantValue value))
                        return value;
                    else
                        return BuildIncrementFunc(variableReference.Reference, values, loopData);
                case InterimUnresolvedReference _:
                    throw new InvalidOperationException();
                // If values does not contain it and it is internal, it must be an intermediate reference
                // and so can be replaced by the result of BuildIncrementFunc.
                default:
                    return operand;
            }
        }

        private void UnrollLoop(BlockOrdering.LoopData loopData, Dictionary<string, List<Encapsulation.Structure>> indices)
        {
            BasicBlock incoming = loopData.body.Dominator;
            int numUnrolls = indices.First().Value.Count;
            List<BasicBlock> originalBody = loopData.GetBody().ToList();
            if (incoming.Instructions.Last() is IRJump jump)
                jump.Target = loopData.exit;
            if (incoming.Instructions.LastOrDefault() is IRBranch)
                incoming.Instructions[incoming.Instructions.Count - 1] = new IRJump(incoming, loopData.exit, -1, -1);
            incoming.AddSuccessor(loopData.exit);
            loopData.body.Dominator.RemoveSuccessor(loopData.body);
            loopData.branchBlock.RemoveSuccessor(loopData.exit);
            loopData.branchBlock.RemoveSuccessor(loopData.body);
            IRBranch branch = loopData.branchBlock.Instructions[loopData.branchBlock.Instructions.Count - 1] as IRBranch;
            loopData.branchBlock.Instructions[loopData.branchBlock.Instructions.Count - 1] = new IRJump(loopData.branchBlock, loopData.exit, branch.SourceLine, branch.SourceColumn);
            
            foreach (BasicBlock block in originalBody)
            {
                block.IsExecutable = false;
                loopData.body.CodeComponent.Blocks.Remove(block);
            }

            BasicBlock nextIncoming;
            for (int i = 0; i < numUnrolls; i++)
            {
                List<BasicBlock> loopBody = BasicBlock.ClonePattern(originalBody, incomingVariables: loopData.body.IncomingVariableDefinitions).ToList();
                
                nextIncoming = loopBody[loopBody.Count - 1];
                while (nextIncoming.PostDominator != null)
                    nextIncoming = nextIncoming.PostDominator;
                BasicBlock first = loopBody[0];
                while (first.Dominator != null)
                    first = first.Dominator;
                ReplaceIncomingVariables(loopBody, loopData.body, indices, i);
                if (!Optimizer.PassesToSkip.Contains(typeof(ConstantFolding)))
                {
                    foreach (BasicBlock block in loopBody)
                        ConstantFolding.ApplyPass(block, Optimizer.AllowClobberBuiltins);
                }
                BasicBlock.Stitch(incoming, loopData.exit, loopBody);

                incoming = nextIncoming;
            }
        }

        private static void ReplaceIncomingVariables(List<BasicBlock> body, BasicBlock root, Dictionary<string, List<Encapsulation.Structure>> indices, int index)
        {
            Dictionary<SSADefinition, Encapsulation.Structure> replacements = new Dictionary<SSADefinition, Encapsulation.Structure>();
            BasicBlock bodyRoot = body[0];
            foreach (string name in indices.Keys)
            {
                IRScope scope = root.Scope;
                while (scope != null)
                {
                    if (root.IncomingVariableDefinitions.TryGetValue((name, scope), out SSADefinition ssaDefinition))
                    {
                        replacements[ssaDefinition] = indices[name][index];
                        break;
                    }
                    scope = scope.ParentScope;
                }
            }
            if (indices.Count == 1)
            {
                IRAssign iteratorAssignment = bodyRoot.Instructions.FirstOrDefault() as IRAssign;
                if (iteratorAssignment?.Value is IRSuffixGet suffixGet &&
                    suffixGet.Object is InterimResolvedReference &&
                    suffixGet.Suffix.Equals("value", StringComparison.OrdinalIgnoreCase) &&
                    iteratorAssignment.Target.Name.Equals(indices.Keys.First()))
                    iteratorAssignment.Value = new InterimConstantValue(replacements.Values.First(), iteratorAssignment);
            }
            foreach (BasicBlock block in body)
            {
                foreach (IRInstruction instruction in block.Instructions.DepthFirst())
                {
                    if (instruction is IOperandInstructionBase operandInstruction)
                    {
                        operandInstruction.MutateEachOperand(op =>
                            op is InterimResolvedReference reference &&
                            replacements.TryGetValue(reference.Reference, out Encapsulation.Structure result) ?
                            new InterimConstantValue(result, reference.SourceLine, reference.SourceColumn) : op);
                    }
                }
            }
        }
    }
}
