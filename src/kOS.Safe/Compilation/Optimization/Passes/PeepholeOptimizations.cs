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


            // Algebraic simplifications
            if (operand is IRBinaryOp binaryOp)
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
                            if (binaryOp.Right is IRBinaryOp opR &&
                                opR.Operation is OpcodeMathMultiply &&
                                opR.Left.Equals(opR.Right) &&
                                opR.Left.Equals(binaryOp.Left))
                            {
                                return CreatePower(binaryOp);
                            }
                            if (binaryOp.Left is IRBinaryOp opL &&
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
            }

            return operand;
        }

        private static void InstructionPeepholeFilter(IRInstruction instruction)
        {
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

        private static IInterimOperand DistributeMultiplication(IRBinaryOp binaryOp, IRBinaryOp opL, IRBinaryOp opR)
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

        private static IInterimOperand DistributeMultiplication(IRBinaryOp instruction)
        {
            // A*B + A*C = A*(B+C)
            IRBinaryOp opL = (IRBinaryOp)instruction.Left;
            IRBinaryOp opR = (IRBinaryOp)instruction.Right;
            // Restructure to create the parentheses
            opR.Operation = instruction.Operation;
            opR.Left = opL.Right;
            // Restructure the multiplication term.
            instruction.Left = opL.Left;
            instruction.Operation = new OpcodeMathMultiply();
            return instruction;
        }

        private static IInterimOperand DistributeNegationA(IRBinaryOp instruction)
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

        private static IInterimOperand DistributeNegationB(IRBinaryOp instruction)
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

        private static IInterimOperand ReplaceNegateSubtract(IRBinaryOp instruction)
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

        private static IInterimOperand IncreasePower(IRBinaryOp instruction, int powerIncrease)
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
                else if (Encapsulation.ScalarIntValue.One.Equals(constantR.Value))
                    return instruction.Left;
                else if (Encapsulation.ScalarIntValue.Zero.Equals(constantR.Value))
                    return new InterimConstantValue(Encapsulation.ScalarIntValue.One, instruction);
            }
            return instruction;
        }

        private static IInterimOperand CreatePower(IRBinaryOp instruction)
        {
            // X*(X*X) = X^3
            instruction.Operation = new OpcodeMathPower();
            instruction.Right = new InterimConstantValue(new Encapsulation.ScalarIntValue(3), (IRInstruction)instruction.Right);
            return instruction;
        }

        private static IInterimOperand CombinePowers(IRBinaryOp instruction)
            => CombinePowers(instruction, new OpcodeMathAdd());
        private static IInterimOperand DividePowers(IRBinaryOp instruction)
            => CombinePowers(instruction, new OpcodeMathSubtract());
        private static IInterimOperand CombinePowers(IRBinaryOp instruction, BinaryOpcode newOperation)
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
                else if (Encapsulation.ScalarIntValue.One.Equals(constantR.Value))
                    return instruction.Left;
                else if (Encapsulation.ScalarIntValue.Zero.Equals(constantR.Value))
                    return new InterimConstantValue(Encapsulation.ScalarIntValue.One, instruction);
            }
            return instruction;
        }
    }
}
