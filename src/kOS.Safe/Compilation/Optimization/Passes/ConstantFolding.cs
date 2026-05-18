using System;
using System.Collections.Generic;
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
                        operandInstruction.MutateEachOperand(AttemptReduction);
                    }
                }
            }
        }

        public static IInterimOperand AttemptReduction(IInterimOperand input)
        {
            // Only fold into an IRConstant when it is a primitive that can be stored in ksm.
            if (input is IEvaluatableToConstant evaluatableToConstant &&
                evaluatableToConstant.IsInvariant &&
                typeof(Encapsulation.PrimitiveStructure).IsAssignableFrom(input.Type))
                return evaluatableToConstant.Evaluate();

            return Simplify(input);
        }

        private static IInterimOperand Simplify(IInterimOperand input)
        {
            switch (input)
            {
                case IRUnaryOp unaryOp:
                    return AlgebraicSimplifications.AttemptUnarySimplification(unaryOp);
                case IRBinaryOp binaryOp:
                    return AttemptBinarySimplification(binaryOp);
                default:
                    return input;
            }
        }

        private static IInterimOperand AttemptBinarySimplification(IRBinaryOp instruction)
        {
            instruction = AlgebraicSimplifications.AttemptAlgebraicSimplification(instruction);

            // Put constants to the left, if there are any
            if (instruction.IsCommutative && instruction.Right is InterimConstantValue && !(instruction.Left is InterimConstantValue))
            {
                BinaryOpcode originalOperation = instruction.Operation;
                bool swapped = instruction.SwapOperands();
                // The value on the left is now a negation operation
                // wrapping a constant. Simplify that,
                if (swapped && originalOperation is OpcodeMathSubtract &&
                    instruction.Left is IRUnaryOp negateOp)
                {
                    instruction.Left = negateOp.Evaluate();
                }
            }
            // If this is false, neither are constants after the last step, unless this isn't commutative, in which case this cleverness doesn't matter.
            if (instruction.Left is InterimConstantValue constantL)
            {
                // If this is true, both are constants
                if (instruction.Right is InterimConstantValue)
                {
                    // Both may be constants, but if AttemptReduction()
                    // didn't evaluate this, the return type is not a valid
                    // opcode argument. Return the unchanged instruction.
                    return instruction;
                }
                else if (instruction.IsCommutative)
                {
                    // The left is constant and the right is not...
                    // But what if the right is commutative with this operation and has a constant?
                    if (instruction.Right is IRBinaryOp rightOp &&
                        rightOp.IsCommutative &&
                        OperationsHaveEqualPriority(rightOp.Operation, instruction.Operation) &&
                        rightOp.Left is InterimConstantValue constantR1)
                    {
                        object left = constantR1.Value;
                        object right = constantL.Value;
                        try
                        {
                            InterimConstantValue result = new InterimConstantValue(instruction.Operation.ExecuteCalculation(left, right), instruction);
                            rightOp.Left = result;
                        }
                        catch (KOSBinaryOperandTypeException binaryTypeException)
                        {
                            throw new KOSCompileException(instruction, binaryTypeException);
                        }
                        return rightOp;
                    }
                }
                // Shortcuts for math operations where both sides don't need to be constant
                switch (instruction.Operation)
                {
                    case OpcodeMathMultiply _:
                        // 0 * X = 0
                        if (Encapsulation.ScalarIntValue.Zero.Equals(constantL.Value))
                            return constantL;
                        if (ReduceDivMult(instruction, constantL, out IInterimOperand newResult))
                            return newResult;
                        break;
                    case OpcodeMathDivide _:
                        // 0 / X = 0
                        // Technically not true when X = 0
                        // But that would otherwise throw a "Tried to push infinite on to the stack" error
                        // So this is an acceptable assumption that improves performance and eliminates an error.
                        // TODO: Add an "EXIT" (EOP) command to the language because this will break the
                        // PRINT(1/0) shortcut to cause a program to terminate.
                        if (Encapsulation.ScalarIntValue.Zero.Equals(constantL.Value))
                            return constantL;
                        if (ReduceDivMult(instruction, constantL, out newResult))
                            return newResult;
                        break;
                    case OpcodeMathAdd _:
                    case OpcodeMathSubtract _:
                        // 0 +- X = X
                        if (Encapsulation.ScalarIntValue.Zero.Equals(constantL.Value))
                            return instruction.Right;
                        break;
                }
            }
            else if (instruction.Right is InterimConstantValue constantR)
            {
                switch (instruction.Operation)
                {
                    case OpcodeMathDivide _:
                        // X / 0 = Error
                        if (Encapsulation.ScalarIntValue.Zero.Equals(constantR.Value))
                            throw new KOSCompileException(instruction, new DivideByZeroException());
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
            else    // Neither operand is constant
            {
                switch (instruction.Operation)
                {
                    case OpcodeMathDivide _:
                        // X / X = 1
                        // Technically not true when X = 0
                        // But that would otherwise throw a "Tried to push infinite on to the stack" error
                        // So this is an acceptable assumption that improves performance and eliminates an error.
                        if (instruction.Left.Equals(instruction.Right))
                            return new InterimConstantValue(Encapsulation.ScalarIntValue.One, instruction);
                        break;
                }
            }
            return instruction;
        }

        private static bool OperationsHaveEqualPriority(BinaryOpcode operation1, BinaryOpcode operation2)
        {
            Type op1 = operation1.GetType();
            Type op2 = operation2.GetType();
            if (op1 == op2)
                return true;
            if (op1 == typeof(OpcodeMathAdd) && op2 == typeof(OpcodeMathSubtract))
                return true;
            if (op2 == typeof(OpcodeMathAdd) && op1 == typeof(OpcodeMathSubtract))
                return true;
            if (op1 == typeof(OpcodeMathMultiply) && op2 == typeof(OpcodeMathDivide))
                return true;
            if (op2 == typeof(OpcodeMathMultiply) && op1 == typeof(OpcodeMathDivide))
                return true;
            return false;
        }

        private static bool ReduceDivMult(IRBinaryOp instruction, InterimConstantValue constantOperand, out IInterimOperand newResult)
        {
            // 1 */ X = X
            if (Encapsulation.ScalarIntValue.One.Equals(constantOperand.Value))
            {
                newResult = instruction.Right;
                return true;
            }
            // -1 */ X = -X
            if (constantOperand.Value.Equals(-Encapsulation.ScalarIntValue.One))
            {
                newResult = new IRUnaryOp(
                    instruction.Block,
                    new OpcodeMathNegate()
                    {
                        SourceColumn = instruction.SourceColumn,
                        SourceLine = instruction.SourceLine
                    },
                    instruction.Right);
                return true;
            }
            newResult = null;
            return false;
        }
    }
}
