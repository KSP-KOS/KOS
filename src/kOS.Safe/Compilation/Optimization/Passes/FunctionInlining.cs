using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Compilation.IR;

namespace kOS.Safe.Compilation.Optimization.Passes
{
    public class FunctionInlining : IHolisticOptimizationPass, ILinkedOptimizationPass
    {
        public OptimizationLevel OptimizationLevel => OptimizationLevel.Aggressive;
        public short SortIndex => 2000;
        public Optimizer Optimizer { get; set; }

        public void ApplyPass(IRCodePart codePart)
        {
            int maxFunctionLength;
            if (Optimizer.OptimizationLevel >= OptimizationLevel.Aggressive)
                maxFunctionLength = 200;
            else
                maxFunctionLength = 50;

            List<IRCodePart.IRFunction> functionsToInline = new List<IRCodePart.IRFunction>
                (codePart.Functions.Where(CanInlineFunction));
            Dictionary<IRCodePart.IRFunction, int> functionLengths = new Dictionary<IRCodePart.IRFunction, int>();
            
            foreach (IRCodePart.IRFunction function in functionsToInline)
                functionLengths[function] = CalculateFunctionLength(function);
            
            functionsToInline.RemoveAll(f => functionLengths[f] > maxFunctionLength);
            functionsToInline.Sort((x, y) => functionLengths[x].CompareTo(functionLengths[y]));

            foreach (IRCodePart.IRFunction function in functionsToInline)
            {
                InlineFunction(function);
            }
        }

        public static int CalculateFunctionLength(IRCodePart.IRFunction function)
        {
            int length = 0;
            foreach (IRCodePart.IRFunction.IRFunctionFragment fragment in function.Fragments)
            {
                foreach (IRInstruction instruction in fragment.FunctionCode.SelectMany(b => b.Instructions))
                {
                    if (instruction is IOperandInstructionBase operandInstruction)
                        operandInstruction.ForEachOperand(op =>
                        {
                            if (op is IResultingInstruction resultingInstruction)
                                length += resultingInstruction.OpcodeCount;
                            else
                                length += 1;

                        });
                    length += 1;
                }
            }
            return length / function.Fragments.Count;
        }

        private static void InlineFunction(IRCodePart.IRFunction function)
        {
            foreach (IRCall call in function.CallSites.ToArray())
            {
                if (!CanInlineFunction(function, call))
                    continue;
                BasicBlock callingBlock = call.Block;
                List<IRInstruction> instructions = callingBlock.Instructions;
                List<IInterimOperand> necessaryStackState = new List<IInterimOperand>();
                int callIndex = 0;
                bool breaking = false;
                for (; callIndex < instructions.Count; callIndex++)
                {
                    foreach (IRInstruction instruction in instructions[callIndex].DepthFirst())
                    {
                        if (breaking && instruction is IOperandInstructionBase operandInstruction)
                        {
                            breaking = false;
                            operandInstruction.ForEachOperand(op =>
                            {
                                if (op == call)
                                    breaking = true;
                                if (!breaking)
                                    necessaryStackState.Add(op);
                            });
                            break;
                        }
                        if (instruction == call)
                            breaking = true;
                    }
                    if (breaking)
                        break;
                    necessaryStackState.Clear();
                }

                if (!breaking)
#if DEBUG
                    throw new KeyNotFoundException();
#else
                    continue;
#endif
                BasicBlock successor = callingBlock.Split(callIndex);
                
                IEnumerable<BasicBlock> inlinedFunction = BasicBlock.ClonePattern(function.Fragments.First().FunctionCode).ToList();
                
                List<IRPushStack> operandPushes = new List<IRPushStack>();
                foreach (IInterimOperand operand in necessaryStackState)
                {
                    IRPushStack newPush = new IRPushStack(callingBlock, operand);
                    callingBlock.Add(newPush);
                    operandPushes.Add(newPush);
                }

                IRParameter resultParameter = new IRParameter(0, successor);
                foreach (IRInstruction instruction in successor.Instructions[0].DepthFirst())
                {
                    if (instruction is IOperandInstructionBase operandInstruction &&
                        operandInstruction.AnyOperand(op => op == call))
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

                ReduceArgumentParameters(functionRoot, call.Arguments.Count, functionRoot.IncomingStackState.Count, call);

                int instructionCount = callingBlock.Instructions.Count;
                for (int i = call.Arguments.Count - 1; i >= 0; i--)
                {
                    IRPushStack paramPush = (IRPushStack)functionRoot.IncomingStackState[i];
                    paramPush.Value = call.Arguments[i];
                    paramPush.Block = callingBlock;
                    callingBlock.Add(paramPush);
                }

                BasicBlock.Stitch(callingBlock, successor, inlinedFunction);
                
                function.CallSites.Remove(call);
            }
        }

        private static void ReduceArgumentParameters(BasicBlock rootBlock, int argsProvided, int maxPossibleArgs, IRCall callSite)
        {
            if (argsProvided > maxPossibleArgs)
                throw new Exceptions.KOSCompileException(new KS.LineCol(callSite.SourceLine, callSite.SourceColumn), "Function was called with too many arguments.");

            int argsRemaining = argsProvided;
            while (maxPossibleArgs > 0)
            {
                for (int i = maxPossibleArgs - argsRemaining; i > 0; --i)
                    rootBlock.IncomingStackState.RemoveAt(argsRemaining);

                foreach (IRAssign assignment in rootBlock.Instructions.Where(i => i is IRAssign).Cast<IRAssign>())
                {
                    if (assignment.Value is IRParameter)
                    {
                        maxPossibleArgs--;
                        argsRemaining--;
                    }
                }

                if (rootBlock.Instructions.Last() is IRBranch branch &&
                    branch.Condition is IRNonVarPush testArgBottom &&
                    testArgBottom.Operation is OpcodeTestArgBottom)
                {
                    if (argsRemaining > 0)
                    {
                        branch.Condition = new InterimConstantValue(Encapsulation.BooleanValue.False, testArgBottom);
                        branch.True.IsExecutable = false;
                        argsRemaining--;
                        rootBlock.Instructions[rootBlock.Instructions.Count - 1] =
                            new IRJump(rootBlock, branch.False, branch.SourceLine, branch.SourceColumn);
                        rootBlock.RemoveSuccessor(branch.True);
                    }
                    else
                    {
                        branch.Condition = new InterimConstantValue(Encapsulation.BooleanValue.True, testArgBottom);
                        branch.True.IncomingStackState.RemoveAt(0);
                        rootBlock.Instructions[rootBlock.Instructions.Count - 1] =
                            new IRJump(rootBlock, branch.True, branch.SourceLine, branch.SourceColumn);
                        rootBlock.RemoveSuccessor(branch.False);
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

        private static bool CanInlineFunction(IRCodePart.IRFunction function)
        {
            if (function.IsRecursive)
                return false;
            if (function.Fragments.Count != 1)
                return false;
            return true;
        }
        public static bool CanInlineFunction(IRCodePart.IRFunction function, IRCall callSite)
        {
            // Without implementing more comprehensive movement of blocks,
            // ternary operator arguments won't be compatible with inlining.
            // This is absolutely doable, but would require a lot more work.
            if (!callSite.EmitArgMarker)
                return false;
            return true;
        }
    }
}
