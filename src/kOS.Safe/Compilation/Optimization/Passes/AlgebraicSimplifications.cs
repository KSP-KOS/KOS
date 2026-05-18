using System;
using kOS.Safe.Compilation.IR;

namespace kOS.Safe.Compilation.Optimization.Passes
{
    public static class AlgebraicSimplifications
    {
        public static IInterimOperand AttemptUnarySimplification(IRUnaryOp unaryOp)
        {

            // Redundant unary operation replacements
            // E.g. !!X = X and --X = X
            IInterimOperand potentialResult = AttempReplaceRedundantUnaryOp(unaryOp);
            if (potentialResult != null)
                return potentialResult;
            return unaryOp;
        }

        private static IInterimOperand AttempReplaceRedundantUnaryOp(IRUnaryOp unaryParent)
        {
            // Replace --X with X
            if (CanOperationBeReplaced(unaryParent, typeof(OpcodeMathNegate)))
                return GetDoubleNestedValue(unaryParent);
            // Replace !!X with X
            if (CanOperationBeReplaced(unaryParent, typeof(OpcodeLogicNot)))
                return GetDoubleNestedValue(unaryParent);
            return null;
        }
        private static bool CanOperationBeReplaced(IRUnaryOp operation, Type opcodeType)
        {
            return operation.Operation.GetType() == opcodeType &&
                operation.Operand is IRUnaryOp neg2 &&
                neg2.Operation.GetType() == opcodeType;
        }
        public static IInterimOperand GetDoubleNestedValue(ISingleOperandInstruction operation)
        {
            return ((IRUnaryOp)operation.Operand).Operand;
        }


        public static IRBinaryOp AttemptAlgebraicSimplification(IRBinaryOp binaryOp)
        {
            switch (binaryOp.Operation)
            {
                case OpcodeMathAdd _:
                    {
                        // A*B + A*C = A*(B+C)
                        {
                            if (binaryOp.Left is IRBinaryOp opL &&
                                binaryOp.Right is IRBinaryOp opR &&
                                opL.Operation is OpcodeMathMultiply &&
                                opR.Operation is OpcodeMathMultiply &&
                                binaryOp.IsCommutative &&
                                opL.IsCommutative &&
                                opR.IsCommutative)
                            {
                                return DistributeMultiplication(binaryOp, opL, opR);
                            }
                        }
                        // A/B + C/B = (A+C)/B
                        {
                            if (binaryOp.Left is IRBinaryOp opL &&
                                binaryOp.Right is IRBinaryOp opR &&
                                opL.Operation is OpcodeMathDivide &&
                                opR.Operation is OpcodeMathDivide &&
                                binaryOp.IsCommutative)
                            {
                                return DistributeDivision(binaryOp);
                            }
                        }
                        // -B+A = A+-B = A-B
                        {
                            if (binaryOp.Right is IRUnaryOp opR &&
                                opR.Operation is OpcodeMathNegate)
                            {
                                return DistributeNegationB(binaryOp);
                            }
                            if (binaryOp.Left is IRUnaryOp opL &&
                                opL.Operation is OpcodeMathNegate &&
                                binaryOp.SwapOperands())
                            {
                                return DistributeNegationB(binaryOp);
                            }
                        }
                        // -A+B = B-A
                        {
                            if (binaryOp.Left is IRUnaryOp opL &&
                                opL.Operation is OpcodeMathNegate)
                            {
                                return DistributeNegationA(binaryOp);
                            }
                        }
                    }
                    break;
                case OpcodeMathSubtract _:
                    // A*B - A*C = A*(B-C)
                    {
                        if (binaryOp.Left is IRBinaryOp opL &&
                            binaryOp.Right is IRBinaryOp opR &&
                            opL.Operation is OpcodeMathMultiply &&
                            opR.Operation is OpcodeMathMultiply &&
                            binaryOp.IsCommutative &&
                            opL.IsCommutative &&
                            opR.IsCommutative)
                        {
                            return DistributeMultiplication(binaryOp, opL, opR);
                        }
                    }
                    // A--B=A+B
                    {
                        if (binaryOp.Right is IRUnaryOp unaryOp &&
                            unaryOp.Operation is OpcodeMathNegate)
                        {
                            return ReplaceNegateSubtract(binaryOp);
                        }
                    }
                    // A/B - C/B = (A-C)/B
                    {
                        if (binaryOp.Left is IRBinaryOp opL &&
                            binaryOp.Right is IRBinaryOp opR &&
                            opL.Operation is OpcodeMathDivide &&
                            opR.Operation is OpcodeMathDivide &&
                            binaryOp.IsCommutative)
                        {
                            return DistributeDivision(binaryOp);
                        }
                    }
                    break;
                case OpcodeMathDivide _:
                    // TODO: Handle distributivity
                    if (binaryOp.Right is InterimConstantValue constantR &&
                        Encapsulation.ScalarIntValue.Zero.Equals(constantR.Value))
                        throw new Exceptions.KOSCompileException(binaryOp, new DivideByZeroException());
                    // X^N/X=X^(N-1)
                    {
                        if (binaryOp.Left is IRBinaryOp opL &&
                            opL.Operation is OpcodeMathPower &&
                            opL.Left.Equals(binaryOp.Right))
                        {
                            return IncreasePower(binaryOp, -1);
                        }
                    }
                    // X^N/X^M=X^(N-M)
                    {
                        if (binaryOp.Left is IRBinaryOp opL &&
                            opL.Operation is OpcodeMathPower &&
                            binaryOp.Right is IRBinaryOp opR &&
                            opR.Operation is OpcodeMathPower &&
                            opL.Left.Equals(opR.Left))
                        {
                            return DividePowers(binaryOp);
                        }
                    }
                    break;
                case OpcodeMathMultiply _:
                    // X^N*X=X^(N+1)
                    {
                        if (binaryOp.Left is IRBinaryOp opL &&
                            opL.Operation is OpcodeMathPower &&
                            opL.Left.Equals(binaryOp.Right))
                        {
                            return IncreasePower(binaryOp, 1);
                        }
                        if (binaryOp.Right is IRBinaryOp opR &&
                            opR.Operation is OpcodeMathPower &&
                            opR.Left.Equals(binaryOp.Left) &&
                            binaryOp.SwapOperands())
                        {
                            return IncreasePower(binaryOp, 1);
                        }
                    }
                    // X*X*...*X=N^X
                    // Start with (X*X)*X = X*(X*X) = X^3
                    // Then the previous rule will kick in and exponentiate from there
                    {
                        // This rule only applies to known scalar values.
                        // Exponentiation is meaningless to other types.
                        if (typeof(Encapsulation.ScalarValue).IsAssignableFrom(binaryOp.Left.Type) &&
                            binaryOp.Right is IRBinaryOp opR &&
                            opR.Operation is OpcodeMathMultiply &&
                            opR.Left.Equals(opR.Right) &&
                            opR.Left.Equals(binaryOp.Left))
                        {
                            return CreatePower(binaryOp);
                        }
                        if (typeof(Encapsulation.ScalarValue).IsAssignableFrom(binaryOp.Right.Type) &&
                            binaryOp.Left is IRBinaryOp opL &&
                            opL.Operation is OpcodeMathMultiply &&
                            opL.Left.Equals(opL.Right) &&
                            opL.Left.Equals(binaryOp.Right) &&
                            binaryOp.SwapOperands())
                        {
                            return CreatePower(binaryOp);
                        }
                    }
                    // X^N*X^M=X^(N+M)
                    {
                        if (binaryOp.Left is IRBinaryOp opL &&
                            opL.Operation is OpcodeMathPower &&
                            binaryOp.Right is IRBinaryOp opR &&
                            opR.Operation is OpcodeMathPower &&
                            opL.Left.Equals(opR.Left))
                        {
                            return CombinePowers(binaryOp);
                        }
                    }
                    break;
            }
            return binaryOp;
        }

        private static IRBinaryOp DistributeMultiplication(IRBinaryOp binaryOp, IRBinaryOp opL, IRBinaryOp opR)
        {
            if (opR.Left.Equals(opL.Left))
            {
                return DistributeMultiplication(binaryOp);
            }
            if (opR.Left.Equals(opL.Right))
            {
                if (!opL.SwapOperands())
                    return binaryOp;
                return DistributeMultiplication(binaryOp);
            }
            if (opR.Right.Equals(opL.Left))
            {
                if (!opR.SwapOperands())
                    return binaryOp;
                return DistributeMultiplication(binaryOp);
            }
            if (opR.Right.Equals(opL.Right))
            {
                if (!opL.SwapOperands() ||
                    !opR.SwapOperands())
                    return binaryOp;
                return DistributeMultiplication(binaryOp);
            }
            return binaryOp;
        }

        private static IRBinaryOp DistributeMultiplication(IRBinaryOp instruction)
        {
            // A*B + A*C = A*(B+C)
            IRBinaryOp opL = (IRBinaryOp)instruction.Left;
            IRBinaryOp opR = (IRBinaryOp)instruction.Right;
            // Restructure to create the parentheses
            opR.Operation = instruction.Operation;
            opR.Left = opL.Right;
            // Restructure the multiplication term.
            instruction.Left = opL.Left;
            instruction.Operation = opL.Operation;
            return instruction;
        }
        private static IRBinaryOp DistributeDivision(IRBinaryOp instruction)
        {
            // B/A + C/A = (B+C)/A
            IRBinaryOp opL = (IRBinaryOp)instruction.Left;
            IRBinaryOp opR = (IRBinaryOp)instruction.Right;
            // Restructure to create the parentheses
            opL.Operation = instruction.Operation;
            opL.Right = opR.Left;
            // Restructure the multiplication term.
            instruction.Right = opR.Right;
            instruction.Operation = opR.Operation;
            return instruction;
        }

        private static IRBinaryOp DistributeNegationA(IRBinaryOp instruction)
        {
            // -A+B = B-A
            if (!(instruction.Left is IRUnaryOp opL))
                return instruction;
            if (!instruction.IsCommutative)
                return instruction;
            BinaryOpcode originalOperation = instruction.Operation;
            instruction.Operation = new OpcodeMathSubtract();
            if (!instruction.IsCommutative)
            {
                instruction.Operation = originalOperation;
                return instruction;
            }
            instruction.Left = opL.Operand;
            instruction.SwapOperands();
            return instruction;
        }

        private static IRBinaryOp DistributeNegationB(IRBinaryOp instruction)
        {
            // A+-B = A-B
            if (!(instruction.Right is IRUnaryOp opR))
                return instruction;
            if (!instruction.IsCommutative)
                return instruction;
            BinaryOpcode originalOperation = instruction.Operation;
            instruction.Operation = new OpcodeMathSubtract();
            if (!instruction.IsCommutative)
            {
                instruction.Operation = originalOperation;
                return instruction;
            }
            instruction.Right = opR.Operand;
            return instruction;
        }

        private static IRBinaryOp ReplaceNegateSubtract(IRBinaryOp instruction)
        {
            // A--B=A+B
            if (!instruction.IsCommutative)
                return instruction;
            BinaryOpcode originalOperation = instruction.Operation;
            instruction.Operation = new OpcodeMathAdd();
            if (!instruction.IsCommutative)
            {
                instruction.Operation = originalOperation;
                return instruction;
            }
            IRUnaryOp opR = (IRUnaryOp)instruction.Right;
            instruction.Right = opR.Operand;
            return instruction;
        }

        private static IRBinaryOp IncreasePower(IRBinaryOp instruction, int powerIncrease)
        {
            // X^N*X=X^(N+1)
            // X^N/X=X^(N-1)
            // Assumes |N +/- 1| < 2^31 - 1 (for integer scalars) or 2^53 (for double scalars)
            // Going beyond that overflows an integer or the mantissa for double.

            IRBinaryOp opL = (IRBinaryOp)instruction.Left;

            short divLine = instruction.SourceLine;
            short divColumn = instruction.SourceColumn;

            InterimConstantValue powConst = new InterimConstantValue(new Encapsulation.ScalarIntValue(powerIncrease), instruction);

            // Set up the new power operation
            instruction.Left = opL.Left;
            instruction.Operation = new OpcodeMathPower();
            instruction.OverwriteSourceLocation(opL.SourceLine, opL.SourceColumn);

            // Reuse the old power instruction for the addition/subtraction
            instruction.Right = opL;
            opL.Left = powConst;
            opL.Operation = new OpcodeMathAdd();
            opL.OverwriteSourceLocation(divLine, divColumn);

            // Attempt constant folding afterwards
            instruction.Right = ConstantFolding.AttemptReduction(opL);

            // Special case for when N == 2 afterwards
            // then revert back to X * X
            if (instruction.Right is InterimConstantValue constantR)
            {
                if (Encapsulation.ScalarIntValue.Two.Equals(constantR.Value))
                {
                    instruction.Right = instruction.Left;
                    instruction.Operation = new OpcodeMathMultiply();
                }
                /*else if (Encapsulation.ScalarIntValue.One.Equals(constantR.Value))
                    return instruction.Left;
                else if (Encapsulation.ScalarIntValue.Zero.Equals(constantR.Value))
                    return new InterimConstantValue(Encapsulation.ScalarIntValue.One, instruction);*/
            }
            return instruction;
        }

        private static IRBinaryOp DividePowers(IRBinaryOp instruction)
            => CombinePowers(instruction, new OpcodeMathSubtract());

        private static IRBinaryOp CombinePowers(IRBinaryOp instruction)
            => CombinePowers(instruction, new OpcodeMathAdd());

        private static IRBinaryOp CombinePowers(IRBinaryOp instruction, BinaryOpcode newOperation)
        {
            // X^N*X^M=X^(N+M)
            // Assumes |N + M| < 2^31 - 1 (for integer scalars) or 2^53 (for double scalars)

            IRBinaryOp opL = (IRBinaryOp)instruction.Left;
            IRBinaryOp opR = (IRBinaryOp)instruction.Right;

            short divLine = instruction.SourceLine;
            short divColumn = instruction.SourceColumn;

            // Set up the new power operation
            instruction.Left = opL.Left;
            instruction.Operation = new OpcodeMathPower();
            instruction.OverwriteSourceLocation(opL.SourceLine, opL.SourceColumn);

            // Reuse an old power instruction for the addition/subtraction
            opR.Left = opL.Right;
            opR.Operation = newOperation;
            opR.OverwriteSourceLocation(divLine, divColumn);

            // Attempt constant folding afterwards
            instruction.Right = ConstantFolding.AttemptReduction(opR);

            // Special case for when N == 2 afterwards
            // then revert back to X * X
            if (instruction.Right is InterimConstantValue constantR)
            {
                if (Encapsulation.ScalarIntValue.Two.Equals(constantR.Value))
                {
                    instruction.Right = instruction.Left;
                    instruction.Operation = new OpcodeMathMultiply();
                }
                /*else if (Encapsulation.ScalarIntValue.One.Equals(constantR.Value))
                    return instruction.Left;
                else if (Encapsulation.ScalarIntValue.Zero.Equals(constantR.Value))
                    return new InterimConstantValue(Encapsulation.ScalarIntValue.One, instruction);*/
            }
            return instruction;
        }

        private static IRBinaryOp CreatePower(IRBinaryOp instruction)
        {
            // X*(X*X) = X^3
            instruction.Operation = new OpcodeMathPower();
            instruction.Right = new InterimConstantValue(new Encapsulation.ScalarIntValue(3), (IRInstruction)instruction.Right);
            return instruction;
        }
    }
}
