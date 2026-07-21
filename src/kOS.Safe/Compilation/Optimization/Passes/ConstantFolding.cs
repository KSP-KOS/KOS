using System;
using System.Collections.Generic;
using kOS.Safe.Compilation.IR;
using kOS.Safe.Exceptions;

namespace kOS.Safe.Compilation.Optimization.Passes
{
    public class ConstantFolding : IOptimizationPass<BasicBlock>, ILinkedOptimizationPass
    {
        public const bool throwOnDivideByZero = false;
        public Optimizer Optimizer { get; set; }
        public OptimizationLevel OptimizationLevel => OptimizationLevel.Minimal;
        public short SortIndex => 30;

        public void ApplyPass(IEnumerable<BasicBlock> blocks)
        {
            foreach (BasicBlock block in blocks)
                ApplyPass(block, Optimizer.AllowClobberBuiltins);
        }
        public static void ApplyPass(BasicBlock block, bool allowClobberBuiltins)
        {
            IInterimOperand AttemptReduction_Internal(IInterimOperand operand)
                => AttemptReduction(operand, allowClobberBuiltins);

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
                foreach (IOperandInstructionBase operandInstruction in instruction.DepthFirst())
                    operandInstruction.MutateEachOperand(AttemptReduction_Internal);
            }
            if (block.Continuation is IOperandInstructionBase operandContinuation)
                operandContinuation.MutateEachOperand(AttemptReduction_Internal);

            if (block.Continuation is BranchContinuation branch)
            {
                if (branch.True == branch.False)
                {
                    block.Continuation = new JumpContinuation(branch.True, branch.SourceLine, branch.SourceColumn);
                }
                else if (branch.IsInvariant)
                {
                    BasicBlock permanentBlock, deprecatedBlock;
                    InterimConstantValue branchConstant = (branch.Condition as IEvaluatableToConstant).Evaluate();
                    if (branchConstant != null)
                    {
                        (permanentBlock, deprecatedBlock) = Convert.ToBoolean(branchConstant.Value) ? (branch.True, branch.False) : (branch.False, branch.True);
                        block.Continuation = new JumpContinuation(permanentBlock, branch.SourceLine, branch.SourceColumn);
                        if (deprecatedBlock.Predecessors.Count == 0)
                            deprecatedBlock.IsExecutable = false;
                    }
                }
            }
        }

        public static IInterimOperand AttemptReduction(IInterimOperand input, bool allowClobberBuiltins)
        {
            // Skip calls if builtins may be clobbered because they may not be what is expected.
            if (!(allowClobberBuiltins && input is IRCall))
            {
                // Don't simplify a divide-by-zero when it is the only operation
                // because that would break existing scripts.
                // TODO: Add an "EXIT" (EOP) command to the language because reducing 1/0 will break the
                // PRINT(1/0) shortcut to cause a program to terminate.
                if (!throwOnDivideByZero &&
                    input is IRBinaryOp binaryOp &&
                    binaryOp.Operation is OpcodeMathDivide &&
                    binaryOp.Right is InterimConstantValue zeroDivisor &&
                    Encapsulation.ScalarIntValue.Zero.Equals(zeroDivisor.Value))
                    return input;
                // Only fold into an IRConstant when it is a primitive that can be stored in ksm.
                if (input is IEvaluatableToConstant evaluatableToConstant &&
                    evaluatableToConstant.IsInvariant &&
                    typeof(Encapsulation.PrimitiveStructure).IsAssignableFrom(input.Type))
                    return evaluatableToConstant.Evaluate();
            }

            return Simplify(input, allowClobberBuiltins);
        }

        private static IInterimOperand Simplify(IInterimOperand input, bool allowClobberBuiltins)
        {
            switch (input)
            {
                case IRUnaryOp unaryOp:
                    return AlgebraicSimplifications.AttemptUnarySimplification(unaryOp);
                case IRBinaryOp binaryOp:
                    return AttemptBinarySimplification(binaryOp, allowClobberBuiltins);
                default:
                    return input;
            }
        }

        private static IInterimOperand AttemptBinarySimplification(IRBinaryOp instruction, bool allowClobberBuiltins)
        {
            instruction = AlgebraicSimplifications.AttemptAlgebraicSimplification(instruction, allowClobberBuiltins);

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

                // Reorder/reflow operations if it helps to fold constants
                if (instruction.Right is IRBinaryOp rightOp &&
                    OperationsHaveEqualPriority(instruction.Operation, rightOp.Operation))
                {
                    // L _ (L1 _ R1)
                    // C _ (A _ B) = (C _ A) _ B    (doesn't require commutativity)
                    if (rightOp.Left is InterimConstantValue constantRL)
                    {
                        InterimConstantValue result = ExecuteOperation(instruction, constantL, constantRL);
                        if (result != null)
                        {
                            rightOp.Left = result;
                            instruction = rightOp;
                        }
                    }
                    // C _ (A _ B) = A _ (C _ B) = (C _ B) _ A  (requires commutativity between A and C)
                    else if (instruction.IsCommutative &&
                        rightOp.Right is InterimConstantValue constantRR)
                    {
                        InterimConstantValue result = ExecuteOperation(rightOp, constantL, constantRR);
                        if (result != null)
                        {
                            instruction.Right = instruction.Left;
                            instruction.Left = result;
                        }
                    }
                }

                // Shortcuts for math operations where both sides don't need to be constant
                switch (instruction.Operation)
                {
                    case OpcodeMathMultiply _:
                        // 0 * X = 0
                        if (Encapsulation.ScalarIntValue.Zero.Equals(constantL.Value))
                            return constantL;
                        // +/-1 * X = X * +/-1 = +/-X
                        if (ReduceDivMult(instruction, constantL, out IInterimOperand newResult))
                            return newResult;
                        {
                            if (instruction.Right is InterimConstantValue constantR &&
                                ReduceDivMult(instruction, constantR, out newResult))
                                return newResult;
                        }
                        break;
                    case OpcodeMathDivide _:
                        // 0 / X = 0
                        // Technically not true when X = 0
                        // But that would otherwise throw a "Tried to push infinite on to the stack" error
                        // So this is an acceptable assumption that improves performance and eliminates an error.
                        if (Encapsulation.ScalarIntValue.Zero.Equals(constantL.Value))
                            return constantL;
                        {
                            // X / +/-1 = +/-X
                            if (instruction.Right is InterimConstantValue constantR && 
                                ReduceDivMult(instruction, constantR, out newResult))
                                return newResult;
                        }
                        break;
                    case OpcodeMathAdd _:
                    case OpcodeMathSubtract _:
                        // 0 +- X = X
                        if (Encapsulation.ScalarIntValue.Zero.Equals(constantL.Value))
                            return instruction.Right;
                        break;
                    case OpcodeCompareEqual _:
                    case OpcodeCompareNE _:
                    case OpcodeCompareGT _:
                    case OpcodeCompareGTE _:
                    case OpcodeCompareLT _:
                    case OpcodeCompareLTE _:
                        // C2 == X + C1 => C2 - C1 == X
                        // C2 == X - C1 => C2 + C1 == X
                        // C2 == X * C1 => C2 / C1 == X
                        // C2 == X / C1 => C2 * C1 == X
                        {
                            while (instruction.Right is IRBinaryOp binaryRight &&
                                binaryRight.IsReversible &&
                                binaryRight.Right is InterimConstantValue constantR)
                            {
                                instruction.Right = binaryRight.Left;
                                IRBinaryOp newOperation = binaryRight.Reverse(instruction.Left, constantR);
                                instruction.Left = newOperation;

                                if ((newOperation.Operation is OpcodeMathDivide || newOperation.Operation is OpcodeMathMultiply) &&
                                    constantR.Value is Encapsulation.ScalarValue scalar &&
                                    scalar < 0)
                                {
                                    switch (instruction.Operation)
                                    {
                                        case OpcodeCompareGT _:
                                            instruction.Operation = new OpcodeCompareLT();
                                            break;
                                        case OpcodeCompareGTE _:
                                            instruction.Operation = new OpcodeCompareLTE();
                                            break;
                                        case OpcodeCompareLT _:
                                            instruction.Operation = new OpcodeCompareGT();
                                            break;
                                        case OpcodeCompareLTE _:
                                            instruction.Operation = new OpcodeCompareGTE();
                                            break;
                                    }
                                }

                                if (instruction.Left is IEvaluatableToConstant constantL2 &&
                                    constantL2.IsInvariant)
                                    instruction.Left = constantL2.Evaluate();
                            }
                        }
                        break;
                }
            }
            else if (instruction.Right is InterimConstantValue constantR)
            {
                // Reorder/reflow operations if it helps to fold constants
                if (instruction.Left is IRBinaryOp leftOp &&
                    OperationsHaveEqualPriority(instruction.Operation, leftOp.Operation))
                {
                    // (L1 _ R1) _ R
                    // (A _ B) _ C = A _ (B _ C)    (doesn't require commutativity)
                    if (leftOp.Right is InterimConstantValue constantLR)
                    {
                        InterimConstantValue result = ExecuteOperation(instruction, constantLR, constantR);
                        if (result != null)
                        {
                            leftOp.Right = result;
                            instruction = leftOp;
                        }
                    }
                }

                switch (instruction.Operation)
                {
                    case OpcodeMathDivide _:
                        // X / 0 = Error
                        if (throwOnDivideByZero &&
                            Encapsulation.ScalarIntValue.Zero.Equals(constantR.Value))
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
                    case OpcodeMathSubtract _:
                        // X - X = 0
                        if (instruction.Left.Equals(instruction.Right))
                            return new InterimConstantValue(Encapsulation.ScalarIntValue.Zero, instruction);
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

        private static InterimConstantValue ExecuteOperation(IRBinaryOp operation, InterimConstantValue left, InterimConstantValue right)
        {
            object leftValue = left.Value;
            object rightValue = right.Value;
            try
            {
                return new InterimConstantValue(operation.Operation.ExecuteCalculation(leftValue, rightValue), operation);
            }
            catch (KOSBinaryOperandTypeException binaryTypeException)
            {
#if DEBUG
                throw new KOSCompileException(operation, binaryTypeException);
#else
                return null;
#endif
            }
        }

        private static bool ReduceDivMult(IRBinaryOp instruction, InterimConstantValue constantOperand, out IInterimOperand newResult)
        {
            // X */ 1 = X
            if (Encapsulation.ScalarIntValue.One.Equals(constantOperand.Value))
            {
                newResult = instruction.Right;
                return true;
            }
            // X */ -1 = -X
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
