using System;
using System.Collections.Generic;
using kOS.Safe.Compilation.IR;

namespace kOS.Safe.Compilation.Optimization.Passes
{
    public class PeepholeOptimizations : IOptimizationPass<IRInstruction>
    {
        public OptimizationLevel OptimizationLevel => OptimizationLevel.Minimal;

        public short SortIndex => 1050;

        public void ApplyPass(List<IRInstruction> code)
        {
            for (int i = 0; i < code.Count; i++)
            {
                IRInstruction instruction = code[i];
                foreach (IRInstruction nestedInstruction in instruction.DepthFirst())
                {
                    if (nestedInstruction is IOperandInstructionBase operandInstruction)
                        operandInstruction.MutateEachOperand(OperandPeepholeFilter);

                    InstructionPeepholeFilter(nestedInstruction);
                }

                //  Replace lex indexing with string constant with suffixing where possible.
                if (instruction is IRIndexSet indexSet)
                {
                    IRInstruction potentialResult = AttemptReplaceIndexSetWithSuffixSet(indexSet);
                    if (potentialResult != null)
                        code[i] = potentialResult;
                }
            }
        }

        private static IInterimOperand OperandPeepholeFilter(IInterimOperand operand)
        {
            // Replace parameterless suffix method calls with get member
            // TODO: Skip this if clobber built-ins is active.
            if (operand is IRCall suffixCall &&
                !suffixCall.Direct &&
                suffixCall.Arguments.Count == 0 &&
                suffixCall.IndirectMethod is IRSuffixGetMethod)
            {
                return ReplaceParameterlessSuffix(suffixCall);
            }

            // Replace calls to VectorDotProduct with multiplication
            // TODO: Skip this if clobber built-ins is active.
            if (operand is IRCall vDotCall &&
                (vDotCall.Function == "vdot" || vDotCall.Function == "vectordotproduct") &&
                vDotCall.Arguments.Count == 2)
            {
                return ReplaceVectorDotProduct(vDotCall);
            }

            // Redundant unary operation replacements
            // E.g. !!X = X and --X = X
            if (operand is IRUnaryOp unaryParent)
            {
                IInterimOperand potentialResult = AttempReplaceRedundantUnaryOp(unaryParent);
                if (potentialResult != null)
                    return potentialResult;
            }

            // Replace lex indexing using string constant with suffixing where possible
            if (operand is IRIndexGet indexGet &&
                indexGet.Index is InterimConstantValue indexConstant &&
                (indexConstant.Value is Encapsulation.StringValue ||
                indexConstant.Value is string))
            {
                IRSuffixGet potentialResult = AttemptReplaceIndexGetWithSuffixGet(indexGet);
                if (potentialResult != null)
                    return potentialResult;
            }

            return operand;
        }

        private static void InstructionPeepholeFilter(IRInstruction instruction)
        {
            // Algebraic simplifications
            if (instruction is IRBinaryOp binaryOp)
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
                                    if (opR.Left == opL.Left)
                                    {
                                        DistributeMultiplication(binaryOp);
                                        return;
                                    }
                                    if (opR.Left == opL.Right)
                                    {
                                        if (!opL.SwapOperands())
                                            return;
                                        DistributeMultiplication(binaryOp);
                                        return;
                                    }
                                    if (opR.Right == opL.Left)
                                    {
                                        if (!opR.SwapOperands())
                                            return;
                                        DistributeMultiplication(binaryOp);
                                        return;
                                    }
                                    if (opR.Right == opL.Right)
                                    {
                                        if (!opL.SwapOperands() ||
                                            !opR.SwapOperands())
                                            return;
                                        DistributeMultiplication(binaryOp);
                                        return;
                                    }
                                }
                            }
                            // -B+A = A+-B = A-B
                            {
                                if (binaryOp.Right is IRUnaryOp opR &&
                                    opR.Operation is OpcodeMathNegate)
                                {
                                    DistributeNegationB(binaryOp);
                                    return;
                                }
                                if (binaryOp.Left is IRUnaryOp opL &&
                                    opL.Operation is OpcodeMathNegate &&
                                    binaryOp.SwapOperands())
                                {
                                    DistributeNegationB(binaryOp);
                                    return;
                                }
                            }
                            // -A+B = B-A
                            {
                                if (binaryOp.Left is IRUnaryOp opL &&
                                    opL.Operation is OpcodeMathNegate)
                                {
                                    DistributeNegationA(binaryOp);
                                    return;
                                }
                            }
                        }
                        break;
                    case OpcodeMathSubtract _:
                        // A--B=A+B
                        {
                            if (binaryOp.Right is IRUnaryOp unaryOp &&
                                unaryOp.Operation is OpcodeMathNegate)
                            {
                                ReplaceNegateSubtract(binaryOp);
                                return;
                            }
                        }
                        break;
                    case OpcodeMathDivide _:
                        // TODO: Handle distributivity
                        if (binaryOp.Right is InterimConstantValue constantR &&
                            Encapsulation.ScalarIntValue.Zero.Equals(constantR.Value))
                            throw new Exceptions.KOSCompileException(instruction, new DivideByZeroException());
                        // X^N/X=X^(N-1)
                        {
                            if (binaryOp.Left is IRBinaryOp opL &&
                                opL.Operation is OpcodeMathPower &&
                                opL.Left == binaryOp.Right)
                            {
                                IncreasePower(binaryOp, -1);
                                return;
                            }
                        }
                        // X^N/X^M=X^(N-M)
                        {
                            if (binaryOp.Left is IRBinaryOp opL &&
                                opL.Operation is OpcodeMathPower &&
                                binaryOp.Right is IRBinaryOp opR &&
                                opR.Operation is OpcodeMathPower &&
                                opL.Left == opR.Left)
                            {
                                DividePowers(binaryOp);
                                return;
                            }
                        }
                        break;
                    case OpcodeMathMultiply _:
                        // X^N*X=X^(N+1)
                        {
                            if (binaryOp.Left is IRBinaryOp opL &&
                                opL.Operation is OpcodeMathPower &&
                                opL.Left == binaryOp.Right)
                            {
                                IncreasePower(binaryOp, 1);
                                return;
                            }
                            if (binaryOp.Right is IRBinaryOp opR &&
                                opR.Operation is OpcodeMathPower &&
                                opR.Left == binaryOp.Left &&
                                binaryOp.SwapOperands())
                            {
                                IncreasePower(binaryOp, 1);
                                return;
                            }
                        }
                        // X*X*...*X=N^X
                        // Start with (X*X)*X = X*(X*X) = X^3
                        // Then the previous rule will kick in and exponentiate from there
                        {
                            if (binaryOp.Right is IRBinaryOp opR &&
                                opR.Operation is OpcodeMathMultiply &&
                                opR.Left == opR.Right &&
                                opR.Left == binaryOp.Left)
                            {
                                CreatePower(binaryOp);
                                return;
                            }
                            if (binaryOp.Left is IRBinaryOp opL &&
                                opL.Operation is OpcodeMathMultiply &&
                                opL.Left == opL.Right &&
                                opL.Left == binaryOp.Right &&
                                binaryOp.SwapOperands())
                            {
                                CreatePower(binaryOp);
                                return;
                            }
                        }
                        // X^N*X^M=X^(N+M)
                        {
                            if (binaryOp.Left is IRBinaryOp opL &&
                                opL.Operation is OpcodeMathPower &&
                                binaryOp.Right is IRBinaryOp opR &&
                                opR.Operation is OpcodeMathPower &&
                                opL.Left == opR.Left)
                            {
                                CombinePowers(binaryOp);
                                return;
                            }
                        }
                        break;
                }
            }

            // Branch logical simplification (e.g. !X branch = X branch!)
            if (instruction is IRBranch branch &&
                branch.Condition is IRUnaryOp negateBranch &&
                negateBranch.Operation is OpcodeLogicNot)
            {
                ReplaceRedundantNotBranch(branch);
                return;
            }
        }

        private static IRSuffixGet ReplaceParameterlessSuffix(IRCall call)
        {
            IRSuffixGet suffixMethod = (IRSuffixGet)call.IndirectMethod;
            return new IRSuffixGet(
                call.Block,
                suffixMethod.Object,
                new OpcodeGetMember(suffixMethod.Suffix)
                {
                    SourceLine = suffixMethod.SourceLine,
                    SourceColumn = suffixMethod.SourceColumn
                });
        }

        private static IRBinaryOp ReplaceVectorDotProduct(IRCall call)
        {
            return new IRBinaryOp(
                call.Block,
                new OpcodeMathMultiply()
                {
                    SourceLine = call.SourceLine,
                    SourceColumn = call.SourceColumn
                },
                call.Arguments[0],
                call.Arguments[1]);
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
        private static IInterimOperand GetDoubleNestedValue(ISingleOperandInstruction operation)
        {
            return ((IRUnaryOp)operation.Operand).Operand;
        }

        private static IRSuffixGet AttemptReplaceIndexGetWithSuffixGet(IRIndexGet indexGet)
        {
            if (indexGet.Index is InterimConstantValue indexConstant &&
                indexConstant.Value is Encapsulation.StringValue stringIndex &&
                StringUtil.IsValidIdentifier(stringIndex))
            {
                return new IRSuffixGet(
                    indexGet.Block,
                    indexGet.Object,
                    new OpcodeGetMember(stringIndex)
                    {
                        SourceLine = indexGet.SourceLine,
                        SourceColumn = indexGet.SourceColumn
                    });
            }
            return null;
        }

        private static IRInstruction AttemptReplaceIndexSetWithSuffixSet(IRIndexSet indexSet)
        {
            if (indexSet.Index is InterimConstantValue indexConstant &&
                indexConstant.Value is Encapsulation.StringValue stringIndex &&
                StringUtil.IsValidIdentifier(stringIndex))
            {
                return new IRSuffixSet(
                    indexSet.Block,
                    indexSet.Object,
                    indexSet.Value,
                    new OpcodeSetMember(stringIndex)
                    {
                        SourceLine = indexSet.SourceLine,
                        SourceColumn = indexSet.SourceColumn
                    });
            }
            return null;
        }

        private static void ReplaceRedundantNotBranch(IRBranch branch)
        {
            branch.Condition = GetDoubleNestedValue(branch);
            branch.PreferFalse = !branch.PreferFalse;
            (branch.True, branch.False) = (branch.False, branch.True);
        }

        private static void DistributeMultiplication(IRBinaryOp instruction)
        {
            // A*B + A*C = A*(B+C)
            IRBinaryOp opL = (IRBinaryOp)instruction.Left;
            IRBinaryOp opR = (IRBinaryOp)instruction.Right;
            // Restructure to create the parentheses
            opR.Operation = new OpcodeMathAdd();
            opR.Left = opL.Right;
            // Restructure the multiplication term.
            instruction.Left = opL.Left;
            instruction.Operation = new OpcodeMathMultiply();
        }

        private static void DistributeNegationA(IRBinaryOp instruction)
        {
            // -A+B = B-A
            if (!(instruction.Left is IRUnaryOp opL))
                return;
            if (!instruction.IsCommutative)
                return;
            BinaryOpcode originalOperation = instruction.Operation;
            instruction.Operation = new OpcodeMathSubtract();
            if (!instruction.IsCommutative)
            {
                instruction.Operation = originalOperation;
                return;
            }
            instruction.Left = opL.Operand;
            instruction.SwapOperands();
        }

        private static void DistributeNegationB(IRBinaryOp instruction)
        {
            // A+-B = A-B
            if (!(instruction.Right is IRUnaryOp opR))
                return;
            if (!instruction.IsCommutative)
                return;
            BinaryOpcode originalOperation = instruction.Operation;
            instruction.Operation = new OpcodeMathSubtract();
            if (!instruction.IsCommutative)
            {
                instruction.Operation = originalOperation;
                return;
            }
            instruction.Right = opR.Operand;
        }

        private static void ReplaceNegateSubtract(IRBinaryOp instruction)
        {
            // A--B=A+B
            if (!instruction.IsCommutative)
                return;
            BinaryOpcode originalOperation = instruction.Operation;
            instruction.Operation = new OpcodeMathAdd();
            if (!instruction.IsCommutative)
            {
                instruction.Operation = originalOperation;
                return;
            }
            IRUnaryOp opR = (IRUnaryOp)instruction.Right;
            instruction.Right = opR.Operand;
        }

        private static void IncreasePower(IRBinaryOp instruction, int powerIncrease)
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
            if (instruction.Right is InterimConstantValue constantR &&
                Encapsulation.ScalarIntValue.Two.Equals(constantR.Value))
            {
                instruction.Right = instruction.Left;
                instruction.Operation = new OpcodeMathMultiply();
            }
        }

        private static void CreatePower(IRBinaryOp instruction)
        {
            // X*(X*X) = X^3
            instruction.Operation = new OpcodeMathPower();
            instruction.Right = new InterimConstantValue(new Encapsulation.ScalarIntValue(3), (IRInstruction)instruction.Right);
        }

        private static void CombinePowers(IRBinaryOp instruction)
            => CombinePowers(instruction, new OpcodeMathAdd());
        private static void DividePowers(IRBinaryOp instruction)
            => CombinePowers(instruction, new OpcodeMathSubtract());
        private static void CombinePowers(IRBinaryOp instruction, BinaryOpcode newOperation)
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
            if (instruction.Right is InterimConstantValue constantR &&
                Encapsulation.ScalarIntValue.Two.Equals(constantR.Value))
            {
                instruction.Right = instruction.Left;
                instruction.Operation = new OpcodeMathMultiply();
            }
        }
    }
}
