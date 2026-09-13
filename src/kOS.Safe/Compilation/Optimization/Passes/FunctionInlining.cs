using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Compilation.IR;

namespace kOS.Safe.Compilation.Optimization.Passes
{
    public class FunctionInlining : IOptimizationPass<IRFunction>, ILinkedOptimizationPass
    {
        public OptimizationLevel OptimizationLevel => OptimizationLevel.Aggressive;
        public short SortIndex => 2000;
        public Optimizer Optimizer { get; set; }

        public void ApplyPass(IEnumerable<IRFunction> functions)
        {
            int maxFunctionLength;
            if (Optimizer.OptimizationLevel >= OptimizationLevel.Aggressive)
                maxFunctionLength = 200;
            else
                maxFunctionLength = 50;

            List<IRFunction> functionsToInline = new List<IRFunction>
                (functions.Where(CanInlineFunction));
            Dictionary<IRFunction, int> functionLengths = new Dictionary<IRFunction, int>();
            
            foreach (IRFunction function in functionsToInline)
                functionLengths[function] = CalculateFunctionLength(function);
            
            functionsToInline.RemoveAll(f => functionLengths[f] > maxFunctionLength);
            functionsToInline.Sort((x, y) => functionLengths[x].CompareTo(functionLengths[y]));

            foreach (IRFunction function in functionsToInline)
            {
                InlineFunction(function);
            }
        }

        public static int CalculateFunctionLength(IInterimFunction function)
        {
            switch (function)
            {
                case null:
                    return 0;
                case IRFunction func:
                    int length = func.Fragments.Sum(fragment => BasicBlock.GetOpcodeCount(fragment.Blocks));
                    return length / func.Fragments.Count;
                case IRAnonymousFunction anonFunc:
                    return BasicBlock.GetOpcodeCount(anonFunc.Blocks);
                default:
                    throw new NotImplementedException();
            }
        }

        private void InlineFunction(IRFunction function)
        {
            foreach (IRCall call in function.CallSites.ToArray())
            {
                if (!CanInlineFunction(function, call, out bool protectScope))
                    continue;
                BasicBlock callingBlock = call.Block;
                List<IRInstruction> instructions = callingBlock.Instructions;
                List<IInterimOperand> necessaryStackState = new List<IInterimOperand>();
                int callIndex = 0;
                bool breaking = false;
                IOperandInstructionBase baseInstruction = null;
                for (; callIndex < instructions.Count; callIndex++)
                {
                    foreach (IOperandInstructionBase operandInstruction in instructions[callIndex].DepthFirst())
                    {
                        if (breaking)
                        {
                            bool innerBreaking = false;
                            operandInstruction.ForEachOperand(op =>
                            {
                                if (op == call)
                                    innerBreaking = true;
                                if (!innerBreaking)
                                    necessaryStackState.Add(op);
                            });
                            break;
                        }
                        if (operandInstruction == call)
                            breaking = true;
                    }

                    if (breaking)
                    {
                        baseInstruction = (IOperandInstructionBase)instructions[callIndex];
                        break;
                    }
                    necessaryStackState.Clear();
                }

                if (!breaking &&
                    callingBlock.Continuation is BranchContinuation branch)
                {
                    // Call site must be in the branch condition
                    foreach (IOperandInstructionBase operandInstruction in branch.DepthFirst())
                    {
                        if (breaking)
                        {
                            bool innerBreaking = false;
                            operandInstruction.ForEachOperand(op =>
                            {
                                if (op == call)
                                    innerBreaking = true;
                                if (!innerBreaking)
                                    necessaryStackState.Add(op);
                            });
                            break;
                        }
                        if (operandInstruction == call)
                            breaking = true;
                    }

                    if (breaking)
                        baseInstruction = branch;
                }

                if (!breaking)
#if DEBUG
                    throw new KeyNotFoundException();
#else
                    continue;
#endif

                BasicBlock successor = callingBlock.Split(callIndex);
                
                IEnumerable<BasicBlock> inlinedFunction = BasicBlock.ClonePattern(function.Fragments.First().Blocks).ToList();
                
                List<IRPushStack> operandPushes = new List<IRPushStack>();
                foreach (IInterimOperand operand in necessaryStackState)
                {
                    IRPushStack newPush = new IRPushStack(callingBlock, operand);
                    callingBlock.Add(newPush);
                    operandPushes.Add(newPush);
                }

                IRParameter resultParameter = new IRParameter(0, successor);
                foreach (IOperandInstructionBase operandInstruction in baseInstruction.DepthFirst())
                {
                    if (operandInstruction.AnyOperand(op => op == call))
                    {
                        operandInstruction.MutateEachOperand(op =>
                        {
                            if (op == call)
                                return resultParameter;
                            return op;
                        });
                        break;
                    }
                }

                StackTransferPhi stackTransferPhi = new StackTransferPhi();
                foreach (BasicBlock block in inlinedFunction)
                {
                    block.Instructions.RemoveAll(i => i is IRNoStackInstruction noStackInstruction && noStackInstruction.Operation is OpcodeArgBottom);
                    if (block.Instructions.LastOrDefault() is IRReturn returnPush)
                    {
                        IRPushStack newPush = new IRPushStack(block, returnPush.Value);
                        block.Instructions[block.Instructions.Count - 1] = newPush;
                        if (returnPush.Depth > 0)
                            block.Instructions.Add(
                                new IRNoStackInstruction(block, new OpcodePopScope(returnPush.Depth)
                                {
                                    SourceLine = returnPush.SourceLine,
                                    SourceColumn = returnPush.SourceColumn
                                }));
                        stackTransferPhi.PossibleValues[block] = newPush;
                    }
                }

                if (stackTransferPhi.PossibleValues.Count > 1)
                {
                    foreach (IStackTransferObject stackTransferObject in stackTransferPhi.PossibleValues.Values)
                        stackTransferObject.AddController(stackTransferPhi);
                    resultParameter.StackTransferObject = stackTransferPhi;
                    successor.IncomingStackState.Insert(0, stackTransferPhi);
                }
                else
                {
                    resultParameter.StackTransferObject = stackTransferPhi.PossibleValues.Values.First();
                    successor.IncomingStackState.Insert(0, stackTransferPhi);
                }

                BasicBlock functionRoot = inlinedFunction.First();
                while (functionRoot.Dominator != null)
                    functionRoot = functionRoot.Dominator;

                for (int i = call.Arguments.Count - 1; i >= 0; i--)
                {
                    IRPushStack paramPush = (IRPushStack)functionRoot.IncomingStackState[i];
                    paramPush.Value = call.Arguments[i];
                    paramPush.Block = callingBlock;
                    callingBlock.Add(paramPush);
                }

                ReduceArgumentParameters(functionRoot, call.Arguments.Count, functionRoot.IncomingStackState.Count, call);

                if (protectScope)
                    functionRoot.Scope.IsProtectedFromRemoval = true;

                if (!Optimizer.PassesToSkip.Contains(typeof(SCCPWithTypePropagation)))
                {
                    Dictionary<SSADefinition, HashSet<IOperandInstructionBase>> localVarUses =
                        SCCPWithTypePropagation.MapUsesAndPropagateTypes(functionRoot);
                    HashSet<SSADefinition> requiredLocalDefs =
                        SCCPWithTypePropagation.PropagateConstants(localVarUses);
                    foreach (BasicBlock block in inlinedFunction)
                        SCCPWithTypePropagation.RemoveRedundantAssignments(block, requiredLocalDefs);
                }
                if (!Optimizer.PassesToSkip.Contains(typeof(ConstantFolding)))
                {
                    foreach (BasicBlock block in inlinedFunction)
                        ConstantFolding.ApplyPass(block, Optimizer.AllowClobberBuiltins);
                    if (baseInstruction is IRPop pop &&
                        pop.IsInvariant)
                        successor.Instructions.Remove((IRInstruction)baseInstruction);
                }

                BasicBlock.Stitch(callingBlock, successor, inlinedFunction);
                
                function.CallSites.Remove(call);
            }
        }

        private static void ReduceArgumentParameters(BasicBlock rootBlock, int argsProvided, int maxPossibleArgs, IRCall callSite)
        {
            if (argsProvided > maxPossibleArgs)
                throw new Exceptions.KOSCompileException(callSite,
                    new Exceptions.KOSArgumentMismatchException("Too many arguments were passed to " + callSite.Function.Replace("$", "").Replace("*", "")));
            if (argsProvided < rootBlock.IncomingStackState.FindIndex(obj => obj.Controllers.Count > 0))
                throw new Exceptions.KOSCompileException(callSite,
                    new Exceptions.KOSArgumentMismatchException("Too few arguments were passed to " + callSite.Function.Replace("$", "").Replace("*", "")));

            HashSet<IStackTransferObject> incomingParameters = new HashSet<IStackTransferObject>(rootBlock.IncomingStackState);
            int argsRemaining = argsProvided;
            while (maxPossibleArgs >= 0)
            {
                for (int i = maxPossibleArgs - argsRemaining; i > 0; --i)
                    rootBlock.IncomingStackState.RemoveAt(argsRemaining);

                foreach (IRAssign assignment in rootBlock.Instructions.Where(i => i is IRAssign).Cast<IRAssign>())
                {
                    if (assignment.Value is IRParameter parameter &&
                        incomingParameters.Contains(parameter.StackTransferObject) &&
                        maxPossibleArgs >= 0)
                    {
                        maxPossibleArgs--;
                        argsRemaining--;
                        assignment.Target.AssignedType = parameter.Type;
                    }
                }

                if (rootBlock.Continuation is BranchContinuation branch &&
                    branch.Condition is IRNonVarPush testArgBottom &&
                    testArgBottom.Operation is OpcodeTestArgBottom)
                {
                    if (argsRemaining > 0)
                    {
                        branch.Condition = new InterimConstantValue(Encapsulation.BooleanValue.False, testArgBottom);
                        branch.True.IsExecutable = false;
                        argsRemaining--;
                        rootBlock.Continuation = new JumpContinuation(branch.False, branch.SourceLine, branch.SourceColumn);
                    }
                    else
                    {
                        branch.Condition = new InterimConstantValue(Encapsulation.BooleanValue.True, testArgBottom);
                        rootBlock.Continuation = new JumpContinuation(branch.True, branch.SourceLine, branch.SourceColumn);

                        foreach (StackTransferPhi stackPhi in branch.False.IncomingStackState.Where(s => s is StackTransferPhi).Cast<StackTransferPhi>())
                        {
                            if (stackPhi.PossibleValues.Keys.Count == 2 &&
                                stackPhi.PossibleValues.ContainsKey(rootBlock) &&
                                stackPhi.PossibleValues.ContainsKey(branch.True))
                                stackPhi.PossibleValues.Remove(rootBlock);
                        }
                    }
                    maxPossibleArgs--;
                }

                if (rootBlock.PostDominator == null)
                    return;
                if (rootBlock.PostDominator is SyntheticReturnBlock)
                    return;
                rootBlock = rootBlock.PostDominator;
            }
        }

        private static bool CanInlineFunction(IRFunction function)
        {
            if (function.IsRecursive)
                return false;
            if (function.Fragments.Count != 1)
                return false;
            return true;
        }
        public static bool CanInlineFunction(IRFunction function, IRCall callSite, out bool protectScope)
        {
            protectScope = false;

            // Without implementing more comprehensive movement of blocks,
            // ternary operator arguments won't be compatible with inlining.
            // This is absolutely doable, but would require a lot more work.
            if (!callSite.EmitArgMarker)
                return false;

            // Because scopes can bypass intervening scopes,
            // We only need to protect the existing scope push
            // if there are identically-name variables between
            // the calling scope and the function scope.
            IRScope scope = callSite.Block.Scope;
            while (scope != null && scope != function.ClosureScope)
            {
                if (scope.Variables.Any(v =>
                    function.ExternalReads.Contains(v) ||
                    function.ExternalWrites.Contains(v) ||
                    function.ExternalUnsets.Any(unset => unset.Name.Equals(v, StringComparison.OrdinalIgnoreCase))))
                {
                    protectScope = true;
                    break;
                }
                scope = scope.ParentScope;
            }

            return true;
        }
    }
}
