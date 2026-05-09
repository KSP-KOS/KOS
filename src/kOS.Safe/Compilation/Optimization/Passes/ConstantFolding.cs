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
                if (instruction is IRPop pop)
                {
                    if (pop.IsInvariant)
                    {
                        block.Instructions.RemoveAt(i);
                        i--;
                        continue;
                    }
                }
                else if (instruction is IRBranch branch)
                {
                    if (branch.True == branch.False)
                    {
                        block.Instructions[i] = new IRJump(block, branch.True, branch.SourceLine, branch.SourceColumn);
                        continue;
                    }
                    if (branch.IsInvariant)
                    {
                        BasicBlock permanentBlock, deprecatedBlock;
                        InterimConstantValue branchConstant = (branch.Condition as IEvaluatableToConstant).Evaluate();
                        if (branchConstant != null)
                        {
                            (permanentBlock, deprecatedBlock) = Convert.ToBoolean(branchConstant.Value) ? (branch.True, branch.False) : (branch.False, branch.True);
                            block.Instructions[i] = new IRJump(block, permanentBlock, new OpcodeBranchJump() { SourceLine = branch.SourceLine, SourceColumn = branch.SourceColumn });
                            block.RemoveSuccessor(deprecatedBlock);
                        }
                    }
                }
                foreach (IRInstruction inst in instruction.DepthFirst())
                {
                    if (inst is IOperandInstructionBase operandInstruction)
                    {
                        operandInstruction.MutateEachOperand(AttemptReductionToConstant);
                    }
                }
            }
        }
        private static IInterimOperand AttemptReductionToConstant(IInterimOperand input)
        {
            if (input is IResultingInstruction instruction)
                return AttemptReduction(instruction);
            return input;
        }

        public static IInterimOperand AttemptReduction(IResultingInstruction instruction)
        {
            // Only fold into an IRConstant when it is a primitive that can be stored in ksm.
            if (!typeof(Encapsulation.PrimitiveStructure).IsAssignableFrom(instruction.Type))
                return instruction;

            switch (instruction)
            {
                case IRUnaryOp unaryOp:
                    return ReduceUnary(unaryOp);
                case IRBinaryOp binaryOp:
                    return ReduceBinary(binaryOp);
                case IRCall call:
                    return ReduceCall(call);
                default:
                    return instruction;
            }
        }
        private static IInterimOperand ReduceUnary(IRUnaryOp instruction)
        {
            if (instruction.IsInvariant)
                return instruction.Evaluate();
            return instruction;
        }
        private static IInterimOperand ReduceBinary(IRBinaryOp instruction)
        {
            if (instruction.IsInvariant)
                return instruction.Evaluate();

            // Put constants to the right, if there are any
            if (instruction.IsCommutative && instruction.Left is InterimConstantValue && !(instruction.Right is InterimConstantValue))
            {
                instruction.SwapOperands();
            }
            // If this is false, neither are constants after the last step, unless this isn't commutative, in which case this cleverness doesn't matter.
            if (instruction.Right is InterimConstantValue constantR)
            {
                // If this is true, both are constants
                if (instruction.Left is InterimConstantValue)
                {
                    return instruction.Evaluate();
                }
                else if (instruction.IsCommutative)
                {
                    // The right is constant and the left is not...
                    // But what if left.Parent is commutative with this operation and has a constant?
                    if (instruction.Left is IRBinaryOp leftOp &&
                        leftOp.Operation.GetType() == instruction.Operation.GetType() &&
                        leftOp.Right is InterimConstantValue constantL1)
                    {
                        object right = constantR.Value;
                        object left = constantL1.Value;
                        try
                        {
                            InterimConstantValue result = new InterimConstantValue(instruction.Operation.ExecuteCalculation(left, right), instruction);
                            leftOp.Right = result;
                        }
                        catch (KOSBinaryOperandTypeException binaryTypeException)
                        {
                            throw new KOSCompileException(instruction, binaryTypeException);
                        }
                        return leftOp;
                    }
                }
                // Shortcuts for math operations where both sides don't need to be constant
                switch (instruction.Operation)
                {
                    case OpcodeMathMultiply _:
                        // X * 0 = 0
                        if (Encapsulation.ScalarIntValue.Zero.Equals(constantR.Value))
                            return constantR;
                        if (ReduceDivMult(instruction, constantR, out IInterimOperand newResult))
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
                            return new InterimConstantValue(Encapsulation.ScalarIntValue.One, instruction);
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
                        if (instruction.Left is InterimConstantValue constantL &&
                            Encapsulation.ScalarIntValue.Zero.Equals(constantL.Value))
                            return constantL;
                        // X / X = 1
                        // Technically not true when X = 0
                        // But that would otherwise throw a "Tried to push infinite on to the stack" error
                        // So this is an acceptable assumption that improves performance and eliminates an error.
                        if (instruction.Left == instruction.Right)
                            return new InterimConstantValue(Encapsulation.ScalarIntValue.One, instruction);
                        break;
                }
            }
            return instruction;
        }
        private static bool ReduceDivMult(IRBinaryOp instruction, InterimConstantValue secondOperand, out IInterimOperand newResult)
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
                newResult = new IRUnaryOp(
                    instruction.Block,
                    new OpcodeMathNegate()
                    {
                        SourceColumn = instruction.SourceColumn,
                        SourceLine = instruction.SourceLine
                    },
                    instruction.Left);
                return true;
            }
            newResult = null;
            return false;
        }

        private static IInterimOperand ReduceCall(IRCall instruction)
        {
            if (instruction.IsInvariant)
                return instruction.Evaluate();

            return instruction;
        }
    }
}
