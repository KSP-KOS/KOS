using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Compilation.IR;
using kOS.Safe.Exceptions;

namespace kOS.Safe.Compilation.Optimization.Passes
{
    public class ConstantFolding : IOptimizationPass<BasicBlock>
    {
        public OptimizationLevel OptimizationLevel => OptimizationLevel.Minimal;
        public short SortIndex => 30;

        public void ApplyPass(List<BasicBlock> blocks)
        {
            Queue<BasicBlock> worklist = new Queue<BasicBlock>(blocks);
            IEnumerator<BasicBlock> enumerator = blocks.GetEnumerator();
            while (enumerator.MoveNext())
            {
                ApplyPass(enumerator.Current);
            }
        }
        private static void ApplyPass(BasicBlock block)
        {
            for (int i = 0; i < block.Instructions.Count; i++)
            {
                IRInstruction instruction = block.Instructions[i];
                switch (instruction)
                {
                    case IRPop pop:
                        if (AttemptReductionToConstant(pop.Value) is IRConstant)
                        {
                            block.Instructions.RemoveAt(i);
                            i--;
                        }
                        continue;
                    case IRAssign assign:
                        assign.Value = AttemptReductionToConstant(assign.Value);
                        break;
                    case IRSuffixSet suffixSet:
                        suffixSet.Value = AttemptReductionToConstant(suffixSet.Value);
                        suffixSet.Object = AttemptReductionToConstant(suffixSet.Object);
                        break;
                    case IRIndexSet indexSet:
                        indexSet.Value = AttemptReductionToConstant(indexSet.Value);
                        indexSet.Object = AttemptReductionToConstant(indexSet.Object);
                        indexSet.Index = AttemptReductionToConstant(indexSet.Index);
                        break;
                    case IRBranch branch:
                        if (branch.True == branch.False)
                            block.Instructions[i] = new IRJump(branch.True, branch.SourceLine, branch.SourceColumn);
                        else if (AttemptReductionToConstant(branch.Condition) is IRConstant branchConstant)
                        {
                            BasicBlock permanentBlock, deprecatedBlock;
                            (permanentBlock, deprecatedBlock) = Convert.ToBoolean(branchConstant.Value) ? (branch.True, branch.False) : (branch.False, branch.True);
                            block.Instructions[i] = new IRJump(permanentBlock, new OpcodeBranchJump() { SourceLine = branch.SourceLine, SourceColumn = branch.SourceColumn });
                            block.RemoveSuccessor(deprecatedBlock);
                            // TODO: Resolve any phi values in the deprecated block and consider re-running folding on that block.
                        }
                        break;
                }
            }
        }
        private static IRValue AttemptReductionToConstant(IRValue input)
        {
            if (input is IRConstant constant)
                return constant;
            if (!(input is IRTemp temp))
                return input;
            return AttemptReduction(temp.Parent);
        }
        private static IRValue AttemptReduction(IRInstruction instruction)
        {
            switch (instruction)
            {
                case IRUnaryOp unaryOp:
                    return ReduceUnary(unaryOp);
                case IRBinaryOp binaryOp:
                    return ReduceBinary(binaryOp);
                case IRSuffixGet suffixGet:
                    return ReduceSuffixGet(suffixGet);
                case IRIndexGet indexGet:
                    return ReduceIndexGet(indexGet);
                case IRCall call:
                    return ReduceCall(call);
                case IResultingInstruction resulting:
                    return resulting.Result;
            }
            throw new ArgumentException($"{instruction.GetType()} is not supported.");
        }
        private static IRValue ReduceUnary(IRUnaryOp instruction)
        {
            if (instruction.Operand is IRTemp temp)
                instruction.Operand = AttemptReduction(temp.Parent);

            if (instruction.Operand is IRConstant constant)
            {
                object input = constant.Value;
                IRValue result;
                try
                {
                    switch (instruction.Operation)
                    {
                        case OpcodeMathNegate _:
                            result = new IRConstant(OpcodeMathNegate.StaticOperation(input), instruction);
                            break;
                        case OpcodeLogicNot _:
                            result = new IRConstant(OpcodeLogicNot.StaticOperation(input), instruction);
                            break;
                        case OpcodeLogicToBool _:
                            result = new IRConstant(OpcodeLogicToBool.StaticOperation(input), instruction);
                            break;
                        default:
                            result = instruction.Result;
                            break;
                    }
                    instruction.Result = result;
                }
                catch (KOSUnaryOperandTypeException unaryTypeException)
                {
                    throw new KOSCompileException(instruction, unaryTypeException);
                }
            }
            return instruction.Result;
        }
        private static IRValue ReduceBinary(IRBinaryOp instruction)
        {
            if (instruction.Left is IRTemp tempL)
            {
                instruction.Left = AttemptReduction(tempL.Parent);
            }
            if (instruction.Right is IRTemp tempR)
            {
                instruction.Right = AttemptReduction(tempR.Parent);
            }
            // Put constants to the right, if there are any
            if (instruction.IsCommutative && instruction.Left is IRConstant && !(instruction.Right is IRConstant))
            {
                instruction.SwapOperands();
            }
            // If this is false, neither are constants after the last step, unless this isn't commutative, in which case this cleverness doesn't matter.
            if (instruction.Right is IRConstant constantR)
            {
                // If this is true, both are constants
                if (instruction.Left is IRConstant constantL)
                {
                    object left = constantL.Value;
                    object right = constantR.Value;
                    try
                    {
                        IRConstant result = new IRConstant(instruction.Operation.ExecuteCalculation(left, right), instruction);
                        instruction.Result = result;
                    }
                    catch (KOSBinaryOperandTypeException binaryTypeException)
                    {
                        throw new KOSCompileException(instruction, binaryTypeException);
                    }
                    return instruction.Result;
                }
                else if (instruction.IsCommutative)
                {
                    // The right is constant and the left is not...
                    // But what if left.Parent is commutative with this operation and has a constant?
                    if (instruction.Left is IRTemp temp &&
                        temp.Parent is IRBinaryOp leftOp &&
                        leftOp.Operation.GetType() == instruction.Operation.GetType() &&
                        leftOp.Right is IRConstant constantL1)
                    {
                        object right = constantR.Value;
                        object left = constantL1.Value;
                        try
                        {
                            IRConstant result = new IRConstant(instruction.Operation.ExecuteCalculation(left, right), instruction);
                            instruction.Result = result;
                            leftOp.Right = result;
                        }
                        catch (KOSBinaryOperandTypeException binaryTypeException)
                        {
                            throw new KOSCompileException(instruction, binaryTypeException);
                        }
                        return leftOp.Result;
                    }
                }
                // Shortcuts for math operations where both sides don't need to be constant
                switch (instruction.Operation)
                {
                    case OpcodeMathMultiply _:
                        // X * 0 = 0
                        if (Encapsulation.ScalarIntValue.Zero.Equals(constantR.Value))
                            return constantR;
                        if (ReduceDivMult(instruction, constantR, out IRValue newResult))
                            return newResult;
                        break;
                    case OpcodeMathDivide _:
                        if (Encapsulation.ScalarIntValue.Zero.Equals(constantR.Value))
                            throw new KOSCompileException(instruction, new DivideByZeroException());
                        if (ReduceDivMult(instruction, constantR, out newResult))
                            return newResult;
                        break;
                    case OpcodeMathAdd _:
                    case OpcodeMathSubtract _:
                        // X +- 0 = X
                        if (Encapsulation.ScalarIntValue.Zero.Equals(constantR.Value))
                            return instruction.Left;
                        break;
                    case OpcodeMathPower _:
                        // X^0 = 1
                        if (Encapsulation.ScalarIntValue.Zero.Equals(constantR.Value))
                            return new IRConstant(Encapsulation.ScalarIntValue.One, instruction);
                        // X^1 = X
                        if (Encapsulation.ScalarIntValue.One.Equals(constantR.Value))
                            return instruction.Left;
                        break;
                }
            }
            else
            {
                switch (instruction.Operation)
                {
                    case OpcodeMathDivide _:
                        // 0 / X = 0
                        // Technically not true when X = 0
                        // But that would otherwise throw a "Tried to push infinite on to the stack" error
                        // So this is an acceptable assumption that improves performance and eliminates an error.
                        // TODO: Add an "EXIT" (EOP) command to the language because this will break the
                        // PRINT(1/0) shortcut to cause a program to terminate.
                        if (instruction.Left is IRConstant constantL &&
                            Encapsulation.ScalarIntValue.Zero.Equals(constantL.Value))
                            return constantL;
                        // X / X = 1
                        // Technically not true when X = 0
                        // But that would otherwise throw a "Tried to push infinite on to the stack" error
                        // So this is an acceptable assumption that improves performance and eliminates an error.
                        if (instruction.Left == instruction.Right)
                            return new IRConstant(Encapsulation.ScalarIntValue.One, instruction);
                        break;
                }
            }
            return instruction.Result;
        }
        private static bool ReduceDivMult(IRBinaryOp instruction, IRConstant secondOperand, out IRValue newResult)
        {
            // X */ 1 = X
            if (Encapsulation.ScalarIntValue.One.Equals(secondOperand.Value))
            {
                newResult = instruction.Left;
                return true;
            }
            // X */ -1 = -X
            if (secondOperand.Value.Equals(-Encapsulation.ScalarIntValue.One))
            {
                IRTemp tempResult = instruction.Result as IRTemp;
                tempResult.Parent = new IRUnaryOp(
                    tempResult,
                    new OpcodeMathNegate()
                    {
                        SourceColumn = instruction.SourceColumn,
                        SourceLine = instruction.SourceLine
                    },
                    instruction.Left);
                newResult = tempResult;
                return true;
            }
            newResult = null;
            return false;
        }

        private static IRValue ReduceSuffixGet(IRSuffixGet instruction)
        {
            if (instruction.Object is IRTemp temp)
            {
                instruction.Object = AttemptReduction(temp.Parent);
            }
            return instruction.Result;
        }
        private static IRValue ReduceIndexGet(IRIndexGet instruction)
        {
            if (instruction.Object is IRTemp tempObj)
            {
                instruction.Object = AttemptReduction(tempObj.Parent);
            }
            if (instruction.Index is IRTemp tempIndex)
            {
                instruction.Index = AttemptReduction(tempIndex.Parent);
            }
            return instruction.Result;
        }
        private static IRValue ReduceCall(IRCall instruction)
        {
            // TODO: Add reduction for specific functions. E.g. mod(X, 1) = 0, round/ceiling/floor, Ln/Log10, min/max
            for (int i = instruction.Arguments.Count - 1; i >= 0; i--)
            {
                if (instruction.Arguments[i] is IRTemp temp)
                    instruction.Arguments[i] = AttemptReduction(temp.Parent);
            }
            string functionName = instruction.Function.Replace("()", "");
            if (Optimizer.FunctionManager.Exists(functionName) && instruction.Arguments.All(arg => arg is IRConstant))
            {
                try
                {
                    switch (functionName)
                    {
                        case "abs":
                        case "mod":
                        case "floor":
                        case "ceiling":
                        case "round":
                        case "sqrt":
                        case "ln":
                        case "log10":
                        case "min":
                        case "max":
                        case "sin":
                        case "cos":
                        case "tan":
                        case "arcsin":
                        case "arccos":
                        case "arctan":
                        case "arctan2":
                        case "anglediff":
                            InterimCPU interimCPU = Optimizer.InterimCPU;
                            interimCPU.Boot();  // Clear the stack out of caution.
                            interimCPU.PushArgumentStack(new Execution.KOSArgMarkerType());
                            foreach (IRValue arg in instruction.Arguments)
                                interimCPU.PushArgumentStack(((IRConstant)arg).Value);
                            Optimizer.FunctionManager.CallFunction(functionName);
                            instruction.Result = new IRConstant(interimCPU.PopValueArgument(), instruction);
                            return instruction.Result;
                    }
                }
                catch (KOSException e)
                {
                    throw new KOSCompileException(instruction, e);
                }
            }
            return instruction.Result;
        }
    }
}
