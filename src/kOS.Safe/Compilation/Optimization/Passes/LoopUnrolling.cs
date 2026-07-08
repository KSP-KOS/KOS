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
            if (maxUnrollIterations < 1)
                return false;

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


                Dictionary<SSADefinition, IInterimOperand> variableReplacements = new Dictionary<SSADefinition, IInterimOperand>();
                Dictionary<string, InterimConstantValue> iterators = new Dictionary<string, InterimConstantValue>();
                Dictionary<string, IInterimOperand> incrementFuncs = new Dictionary<string, IInterimOperand>();
                condition = condition.Clone(null, true);
                // BuildIterators is a recursive function that will
                // construct the appropriate iteration simulation
                // objects, or return null if an invalid operand is used.
                condition = BuildIterators(condition, iterators, incrementFuncs, variableReplacements, data);
                if (condition == null)
                    return false;

                indices = PopulateIndices(condition, iterators, incrementFuncs, maxUnrollIterations);

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

        private static IInterimOperand BuildIterators(IInterimOperand operand, Dictionary<string, InterimConstantValue> iterators, Dictionary<string, IInterimOperand> incrementFuncs, Dictionary<SSADefinition, IInterimOperand> variableReplacements, BlockOrdering.LoopData data)
        {
            bool IsBuildingIteratorsInvalid(IOperandInstructionBase operandInstruction)
            {
                bool invalid = false;
                IInterimOperand BuildIteratorsRecursive(IInterimOperand op)
                {
                    if (invalid)
                        return op;
                    IInterimOperand result = BuildIterators(op, iterators, incrementFuncs, variableReplacements, data);
                    if (result == null)
                        invalid = true;
                    return result;
                }
                operandInstruction.MutateEachOperand(BuildIteratorsRecursive);
                return invalid;
            }
            switch (operand)
            {
                case IRCall call:
                    if (!call.IsInert)
                        return null;
                    if (IsBuildingIteratorsInvalid(call))
                        return null;
                    return operand;
                case InterimVariableReference _:
                case InterimUnresolvedReference _:
                case IRRelocateLater _:
                    return null;
                case IRParameter _:
                    // TODO: resolve this somehow.
                    return null;
                case InterimResolvedReference resolvedReference:
                    // Return the variable's replacement.
                    if (resolvedReference.Reference.State != SSADefinition.SetState.Set)
                        return null;
                    if (variableReplacements.TryGetValue(resolvedReference.Reference, out IInterimOperand replacement))
                        return replacement;
                    IInterimOperand result;
                    switch (resolvedReference.Reference)
                    {
                        case SSASetDefinition setDefinition:
                            // Internally set variables are replaced with their increment function
                            if (data.BodyContains(setDefinition.DefinedAt.Block))
                            {
                                result = BuildIterators(setDefinition.DefinedAt.Value.Clone(null, true), iterators, incrementFuncs, variableReplacements, data);
                            }
                            // Externally set variables are replaced with their incoming value.
                            else
                            {
                                IEvaluatableToConstant incomingValue = setDefinition.DefinedAt.Value as IEvaluatableToConstant;
                                if (incomingValue?.IsInvariant ?? false)
                                {
                                    result = incomingValue.Evaluate();
                                }
                                else
                                    return null;
                            }
                            variableReplacements[setDefinition] = result;
                            return result;
                        case SSAPotentialDefinition _:
                            return null;
                        case PhiVariable phi:
                            IEnumerable<KeyValuePair<BasicBlock, SSADefinition>> possibleValues = phi.Node.PossibleValues.Where(kvp => kvp.Key.IsExecutable);
                            int numPossibleValues = possibleValues.Select(kvp => kvp.Value).Distinct().Count();
                            // More than 2 or zero possible values is unhandled.
                            if (numPossibleValues > 2 || numPossibleValues == 0)
                                return null;
                            // Handle one possible value as if it were the nested value.
                            if (numPossibleValues == 1)
                            {
                                resolvedReference = new InterimResolvedReference(phi.Node.PossibleValues.First(kvp => kvp.Key.IsExecutable).Value, resolvedReference.SourceLine, resolvedReference.SourceColumn);
                                return BuildIterators(resolvedReference, iterators, incrementFuncs, variableReplacements, data);
                            }
                            // More than 1 possible external value means the loop length is not invariant.
                            int numPossibleExternalValues = possibleValues.Where(kvp => !data.BodyContains(kvp.Key)).Select(kvp => kvp.Value).Distinct().Count();
                            if (numPossibleExternalValues > 1)
                                return null;
                            // One internal and one external value means this variable is an iterator.
                            int numPossibleInternalValues = possibleValues.Where(kvp => data.BodyContains(kvp.Key)).Select(kvp => kvp.Value).Distinct().Count();
                            short line = resolvedReference.SourceLine;
                            short column = resolvedReference.SourceColumn;
                            if (numPossibleInternalValues == 1 && numPossibleExternalValues == 1)
                            {

                                // Set the iterator value to the incoming value, which must resolve to a single, constant value.
                                resolvedReference = new InterimResolvedReference(phi.Node.PossibleValues.First(kvp => kvp.Key.IsExecutable && !data.BodyContains(kvp.Key)).Value, line, column);
                                result = BuildIterators(resolvedReference, null, null, variableReplacements, data);
                                if (!(result is IEvaluatableToConstant constantIn &&
                                    constantIn.IsInvariant))
                                    return null;
                                result = constantIn.Evaluate();
                                iterators.Add(phi.Name, (InterimConstantValue)result);
                                variableReplacements.Add(phi, result);

                                // Set the increment function to the looping value
                                resolvedReference = new InterimResolvedReference(phi.Node.PossibleValues.First(kvp => kvp.Key.IsExecutable && data.BodyContains(kvp.Key)).Value, line, column);
                                IInterimOperand iteratorFunc = BuildIterators(resolvedReference, iterators, incrementFuncs, variableReplacements, data);
                                if (iteratorFunc == null)
                                    return null;
                                incrementFuncs.Add(phi.Name, iteratorFunc);
                                return result;
                            }
                            // Two internal values means the result can come
                            // from one of two branches inside the loop.
                            else if (numPossibleInternalValues == 2)
                            {
                                TernaryOperand ternaryOperand = new TernaryOperand();
                                variableReplacements.Add(phi, ternaryOperand);

                                IRBranch branch = GetBranch(possibleValues.Select(kvp => kvp.Key));
                                IInterimOperand condition = branch.Condition.Clone(null, true);
                                condition = BuildIterators(condition, iterators, incrementFuncs, variableReplacements, data);
                                if (condition == null)
                                    return null;

                                BasicBlock trueBlock = phi.Node.PossibleValues.Keys.FirstOrDefault(b => b.IsDominatedBy(branch.True, branch.Block));
                                if (trueBlock == null)
                                    return null;
                                IInterimOperand trueValue = new InterimResolvedReference(phi.Node.PossibleValues[trueBlock], line, column);
                                trueValue = BuildIterators(trueValue, iterators, incrementFuncs, variableReplacements, data);
                                if (trueValue == null)
                                    return null;

                                BasicBlock falseBlock = phi.Node.PossibleValues.Keys.FirstOrDefault(b => b.IsDominatedBy(branch.False, branch.Block));
                                if (falseBlock == null)
                                    return null;
                                IInterimOperand falseValue = new InterimResolvedReference(phi.Node.PossibleValues[falseBlock], line, column);
                                falseValue = BuildIterators(falseValue, iterators, incrementFuncs, variableReplacements, data);
                                if (falseValue == null)
                                    return null;

                                ternaryOperand.Condition = condition;
                                ternaryOperand.TrueValue = trueValue;
                                ternaryOperand.FalseValue = falseValue;
                                return ternaryOperand;
                            }
                            else
                                return null;
                        default:
#if DEBUG
                            throw new NotImplementedException();
#else
                            return null;
#endif
                    }
                case IResultingInstruction _:
                    if (operand is IOperandInstructionBase operandInstruction)
                    {
                        if (IsBuildingIteratorsInvalid(operandInstruction))
                            return null;
                        return operand;
                    }
                    else
                        return null;
                case InterimConstantValue _:
                    return operand;
                case TernaryOperand ternary:
                    if (IsBuildingIteratorsInvalid(ternary))
                        return null;
                    return ternary;
                default:
#if DEBUG
                    throw new NotImplementedException();
#else
                    return null;
#endif
            }
        }
        private static IRBranch GetBranch(IEnumerable<BasicBlock> blocks)
        {
            List<BasicBlock> blockList = blocks.ToList();
            int maxIndex = blockList.Count - 1;
            HashSet<BasicBlock> visited = new HashSet<BasicBlock>();
            while (true)
            {
                for (int i = maxIndex; i >= 0; i--)
                {
                    if (blockList[i] != null && !visited.Add(blockList[i]))
                        return blockList[i].Instructions.LastOrDefault() as IRBranch;
                    blockList[i] = blockList[i]?.Dominator;
                }
            }
        }

        private static Dictionary<string, List<Encapsulation.Structure>> PopulateIndices(IInterimOperand condition, Dictionary<string, InterimConstantValue> iterators, Dictionary<string, IInterimOperand> incrementFuncs, int maxIterations)
        {
            Dictionary<string, List<Encapsulation.Structure>> indices = new Dictionary<string, List<Encapsulation.Structure>>();
            Dictionary<string, Encapsulation.Structure> nextIndex = new Dictionary<string, Encapsulation.Structure>();
            List<string> iteratorVariables = new List<string>();
            foreach (string name in iterators.Keys)
            {
                indices.Add(name, new List<Encapsulation.Structure>());
                iteratorVariables.Add(name);
                indices[name].Add((Encapsulation.Structure)iterators[name].Value);
                nextIndex[name] = (Encapsulation.Structure)((IEvaluatableToConstant)incrementFuncs[name]).Evaluate().Value;
            }

            int iteration = 1;
            try
            {
                // Things shouldn't be able to throw an exception here,
                // but better safe than sorry.
                // The loop is already structured in a do-while format
                // because of the LoopConditionalRelocation pass
                do
                {
                    if (iteration++ > maxIterations)
                        break;
                    foreach (string name in iteratorVariables)
                    {
                        iterators[name].Value = nextIndex[name];
                        indices[name].Add(nextIndex[name]);
                    }
                    foreach (string name in iteratorVariables)
                        nextIndex[name] = (Encapsulation.Structure)((IEvaluatableToConstant)incrementFuncs[name]).Evaluate().Value;
                } while (!Convert.ToBoolean(((IEvaluatableToConstant)condition).Evaluate().Value));
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

            return indices;
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

        public class TernaryOperand : IInterimOperand, IMultipleOperandInstruction, IEvaluatableToConstant
        {
            public IInterimOperand Condition { get; set; }
            public IInterimOperand TrueValue { get; set; }
            public IInterimOperand FalseValue { get; set; }
            public bool IsInvariant
            {
                get
                {
                    bool? condition = EvaluateCondition();
                    if (condition == null)
                        return false;
                    if (condition == true)
                        return TrueValue.IsInvariant;
                    else
                        return FalseValue.IsInvariant;
                }
            }
            public Type Type
            {
                get
                {
                    bool? condition = EvaluateCondition();
                    if (condition == null)
                        return PhiNode<IInterimOperand>.GetFirstCommonBaseType(TrueValue.Type, FalseValue.Type);
                    if (condition == true)
                        return TrueValue.Type;
                    else
                        return FalseValue.Type;
                }
            }
            public IEnumerable<IInterimOperand> Operands => throw new NotImplementedException();
            public int OperandCount => 3;

            private bool? EvaluateCondition()
            {
                if ((Condition?.IsInvariant ?? false) &&
                    Condition is IEvaluatableToConstant evaluatable)
                    return Convert.ToBoolean(evaluatable.Evaluate().Value);
                return null;
            }
            public bool AllOperands(Func<IInterimOperand, bool> predicate)
                => predicate(Condition) &&
                predicate(TrueValue) &&
                predicate(FalseValue);

            public bool AnyOperand(Func<IInterimOperand, bool> predicate)
                => predicate(Condition) ||
                predicate(TrueValue) ||
                predicate(FalseValue);

            public IEnumerable<Opcode> EmitOpcodes()
            {
                bool? condition = EvaluateCondition();
                if (condition == null)
                {
                    throw new NotImplementedException();
                    yield break;
                }
                IEnumerable<Opcode> result;
                if (condition == true)
                    result = TrueValue.EmitOpcodes();
                else
                    result = FalseValue.EmitOpcodes();
                foreach (Opcode op in result)
                    yield return op;
            }

            public bool Equals(IInterimOperand other)
                => other is TernaryOperand ternary &&
                ternary.Condition.Equals(Condition) &&
                ternary.TrueValue.Equals(TrueValue) &&
                ternary.FalseValue.Equals(FalseValue);

            public void ForEachOperand(Action<IInterimOperand> action)
            {
                action(Condition);
                action(TrueValue);
                action(FalseValue);
            }

            public void MutateEachOperand(Func<IInterimOperand, IInterimOperand> mutateFunc)
            {
                Condition = mutateFunc(Condition);
                TrueValue = mutateFunc(TrueValue);
                FalseValue = mutateFunc(FalseValue);
            }

            public IInterimOperand Clone(BasicBlock block, bool maintainSSAReferences = false)
                => new TernaryOperand()
                {
                    Condition = Condition.Clone(block, maintainSSAReferences),
                    TrueValue = TrueValue.Clone(block, maintainSSAReferences),
                    FalseValue = FalseValue.Clone(block, maintainSSAReferences)
                };

            public InterimConstantValue Evaluate()
            {
                bool condition = EvaluateCondition() ?? throw new InvalidOperationException();
                if (condition)
                {
                    if (!(TrueValue.IsInvariant &&
                        TrueValue is IEvaluatableToConstant trueValue))
                        throw new InvalidOperationException();
                    return trueValue.Evaluate();
                }
                else
                {
                    if (!(FalseValue.IsInvariant &&
                        FalseValue is IEvaluatableToConstant falseValue))
                        throw new NotImplementedException();
                    return falseValue.Evaluate();
                }
            }
        }
    }
}
