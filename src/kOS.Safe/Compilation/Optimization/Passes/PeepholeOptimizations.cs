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
                    PeepholeFilter(nestedInstruction);

                //  Replace lex indexing with string constant with suffixing where possible.
                if (instruction is IRIndexSet indexSet)
                {
                    IRInstruction potentialResult = AttemptReplaceIndexSetWithSuffixSet(indexSet);
                    if (potentialResult != null)
                        code[i] = potentialResult;
                }
            }
        }

        private static void PeepholeFilter(IRInstruction instruction)
        {
            // Replace parameterless suffix method calls with get member
            // TODO: Skip this if clobber built-ins is active.
            if (instruction is IRCall suffixCall &&
                !suffixCall.Direct &&
                suffixCall.Arguments.Count == 0 &&
                suffixCall.IndirectMethod is IRTemp tempSuffixCall &&
                tempSuffixCall.Parent is IRSuffixGetMethod)
            {
                ReplaceParameterlessSuffix(suffixCall);
                return;
            }
            // Replace calls to VectorDotProduct with multiplication
            // TODO: Skip this if clobber built-ins is active.
            if (instruction is IRCall vDotCall &&
                (vDotCall.Function == "vdot" || vDotCall.Function == "vectordotproduct") &&
                vDotCall.Arguments.Count == 2)
            {
                ReplaceVectorDotProduct(vDotCall);
                return;
            }
            // Algebraic simplifications
            if (instruction is IRBinaryOp binaryOp)
            {
                IRTemp tempL = binaryOp.Left as IRTemp;
                IRTemp tempR = binaryOp.Right as IRTemp;
                switch (binaryOp.Operation)
                {
                    case OpcodeMathAdd _:
                        {
                            // A*B + A*C = A*(B+C)
                            {
                                if (tempL != null &&
                                    tempL.Parent is IRBinaryOp opL &&
                                    tempR != null &&
                                    tempR.Parent is IRBinaryOp opR &&
                                    opL.Operation is OpcodeMathMultiply &&
                                    opR.Operation is OpcodeMathMultiply)
                                {
                                    if (opR.Left == opL.Left)
                                    {
                                        DistributeMultiplication(binaryOp);
                                        return;
                                    }
                                    if (opR.Left == opL.Right)
                                    {
                                        opL.SwapOperands();
                                        DistributeMultiplication(binaryOp);
                                        return;
                                    }
                                    if (opR.Right == opL.Left)
                                    {
                                        opR.SwapOperands();
                                        DistributeMultiplication(binaryOp);
                                        return;
                                    }
                                    if (opR.Right == opL.Right)
                                    {
                                        opL.SwapOperands();
                                        opR.SwapOperands();
                                        DistributeMultiplication(binaryOp);
                                        return;
                                    }
                                }
                            }
                            // -B+A = A+-B = A-B
                            {
                                if (tempR != null &&
                                    tempR.Parent is IRUnaryOp opR &&
                                    opR.Operation is OpcodeMathNegate)
                                {
                                    DistributeNegationB(binaryOp);
                                    return;
                                }
                                if (tempL != null &&
                                    tempL.Parent is IRUnaryOp opL &&
                                    opL.Operation is OpcodeMathNegate)
                                {
                                    binaryOp.SwapOperands();
                                    DistributeNegationB(binaryOp);
                                    return;
                                }
                            }
                            // -A+B = B-A
                            {
                                if (tempL != null &&
                                    tempL.Parent is IRUnaryOp opL &&
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
                            if (tempR != null &&
                                tempR.Parent is IRUnaryOp unaryOp &&
                                unaryOp.Operation is OpcodeMathNegate)
                            {
                                ReplaceNegateSubtract(binaryOp);
                                return;
                            }
                        }
                        break;
                    case OpcodeMathDivide _:
                        if (binaryOp.Right is IRConstant constantR &&
                            Encapsulation.ScalarIntValue.Zero.Equals(constantR.Value))
                            throw new Exceptions.KOSCompileException(instruction, new DivideByZeroException());
                        // X^N/X=X^(N-1)
                        {
                            if (tempL != null &&
                                tempL.Parent is IRBinaryOp opL &&
                                opL.Operation is OpcodeMathPower &&
                                opL.Left == binaryOp.Right)
                            {
                                IncreasePower(binaryOp, -1);
                                return;
                            }
                        }
                        // X^N/X^M=X^(N-M)
                        {
                            if (tempL != null &&
                                tempL.Parent is IRBinaryOp opL &&
                                opL.Operation is OpcodeMathPower &&
                                tempR != null &&
                                tempR.Parent is IRBinaryOp opR &&
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
                            if (tempL != null &&
                                tempL.Parent is IRBinaryOp opL &&
                                opL.Operation is OpcodeMathPower &&
                                opL.Left == binaryOp.Right)
                            {
                                IncreasePower(binaryOp, 1);
                                return;
                            }
                            if (tempR != null &&
                                tempR.Parent is IRBinaryOp opR &&
                                opR.Operation is OpcodeMathPower &&
                                opR.Left == binaryOp.Left)
                            {
                                binaryOp.SwapOperands();
                                IncreasePower(binaryOp, 1);
                                return;
                            }
                        }
                        // X*X*...*X=N^X
                        // Start with (X*X)*X = X*(X*X) = X^3
                        // Then the previous rule will kick in and exponentiate from there
                        {
                            if (tempR != null &&
                                tempR.Parent is IRBinaryOp opR &&
                                opR.Operation is OpcodeMathMultiply &&
                                opR.Left == opR.Right &&
                                opR.Left == binaryOp.Left)
                            {
                                CreatePower(binaryOp);
                                return;
                            }
                            if (tempL != null &&
                                tempL.Parent is IRBinaryOp opL &&
                                opL.Operation is OpcodeMathMultiply &&
                                opL.Left == opL.Right &&
                                opL.Left == binaryOp.Right)
                            {
                                binaryOp.SwapOperands();
                                CreatePower(binaryOp);
                                return;
                            }
                        }
                        // X^N*X^M=X^(N+M)
                        {
                            if (tempL != null &&
                                tempL.Parent is IRBinaryOp opL &&
                                opL.Operation is OpcodeMathPower &&
                                tempR != null &&
                                tempR.Parent is IRBinaryOp opR &&
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

            if (instruction is IOperandInstructionBase operandInstruction)
            {
                // Redundant unary operation replacements
                // E.g. !!X = X and --X = X
                operandInstruction.MutateEachOperand(op =>
                {
                    if (op is IRTemp tempOperand &&
                    tempOperand.Parent is IRUnaryOp unaryParent)
                    {
                        IRValue potentialResult = AttempReplaceRedundantUnaryOp(unaryParent);
                        if (potentialResult != null)
                            return potentialResult;
                    }
                    return op;
                });

                // Replace lex indexing using string constant with suffixing where possible
                operandInstruction.ForEachOperand(op =>
                {
                    if (op is IRTemp tempOperand &&
                        tempOperand.Parent is IRIndexGet indexGet &&
                        indexGet.Index is IRConstant indexConstant &&
                        (indexConstant.Value is Encapsulation.StringValue ||
                        indexConstant.Value is string))
                    {
                        IRInstruction potentialResult = AttemptReplaceIndexGetWithSuffixGet(indexGet);
                        if (potentialResult != null)
                            tempOperand.Parent = potentialResult;
                    }

                });
            }
            // Branch logical simplification (e.g. !X branch = X branch!)
            if (instruction is IRBranch branch &&
                branch.Condition is IRTemp temp &&
                temp.Parent is IRUnaryOp negateBranch &&
                negateBranch.Operation is OpcodeLogicNot)
            {
                ReplaceRedundantNotBranch(branch);
                return;
            }
        }

        private static void ReplaceParameterlessSuffix(IRCall call)
        {
            IRSuffixGet suffixMethod = (IRSuffixGet)((IRTemp)call.IndirectMethod).Parent;
            ((IRTemp)call.Result).Parent = new IRSuffixGet(
                (IRTemp)call.Result,
                suffixMethod.Object,
                new OpcodeGetMember(suffixMethod.Suffix)
                {
                    SourceLine = suffixMethod.SourceLine,
                    SourceColumn = suffixMethod.SourceColumn
                });
        }

        private static void ReplaceVectorDotProduct(IRCall call)
        {
            ((IRTemp)call.Result).Parent = new IRBinaryOp(
                (IRTemp)call.Result,
                new OpcodeMathMultiply()
                {
                    SourceLine = call.SourceLine,
                    SourceColumn = call.SourceColumn
                },
                call.Arguments[0],
                call.Arguments[1]);
        }

        private static IRValue AttempReplaceRedundantUnaryOp(IRUnaryOp unaryParent)
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
                    operation.Operand is IRTemp operand &&
                    operand.Parent is IRUnaryOp neg2 &&
                    neg2.Operation.GetType() == opcodeType;
        }
        private static IRValue GetDoubleNestedValue(ISingleOperandInstruction operation)
        {
            return ((IRUnaryOp)((IRTemp)operation.Operand).Parent).Operand;
        }

        private static IRInstruction AttemptReplaceIndexGetWithSuffixGet(IRIndexGet indexGet)
        {
            if (indexGet.Index is IRConstant indexConstant &&
                indexConstant.Value is Encapsulation.StringValue stringIndex &&
                StringUtil.IsValidIdentifier(stringIndex))
            {
                return new IRSuffixGet((IRTemp)indexGet.Result,
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
            if (indexSet.Index is IRConstant indexConstant &&
                indexConstant.Value is Encapsulation.StringValue stringIndex &&
                StringUtil.IsValidIdentifier(stringIndex))
            {
                return new IRSuffixSet(indexSet.Object,
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
            IRTemp tempL = (IRTemp)instruction.Left;
            IRTemp tempR = (IRTemp)instruction.Right;
            IRBinaryOp opL = (IRBinaryOp)tempL.Parent;
            IRBinaryOp opR = (IRBinaryOp)tempR.Parent;
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
            IRTemp tempL = (IRTemp)instruction.Left;
            IRUnaryOp opL = (IRUnaryOp)tempL.Parent;
            instruction.Left = opL.Operand;
            instruction.SwapOperands();
            instruction.Operation = new OpcodeMathSubtract();
        }

        private static void DistributeNegationB(IRBinaryOp instruction)
        {
            // A+-B = A-B
            IRTemp tempR = (IRTemp)instruction.Right;
            IRUnaryOp opR = (IRUnaryOp)tempR.Parent;
            instruction.Right = opR.Operand;
            instruction.Operation = new OpcodeMathSubtract();
        }

        private static void ReplaceNegateSubtract(IRBinaryOp instruction)
        {
            // A--B=A+B
            IRTemp tempR = (IRTemp)instruction.Right;
            IRUnaryOp opR = (IRUnaryOp)tempR.Parent;
            instruction.Right = opR.Operand;
            instruction.Operation = new OpcodeMathAdd();
        }

        private static void IncreasePower(IRBinaryOp instruction, int powerIncrease)
        {
            // X^N*X=X^(N+1)
            // X^N/X=X^(N-1)
            // Assumes |N +/- 1| < 2^31 - 1 (for integer scalars) or 2^53 (for double scalars)
            // Going beyond that overflows an integer or the mantissa for double.

            IRTemp tempL = (IRTemp)instruction.Left;
            IRBinaryOp opL = (IRBinaryOp)tempL.Parent;

            short divLine = instruction.SourceLine;
            short divColumn = instruction.SourceColumn;

            IRConstant powConst = new IRConstant(new Encapsulation.ScalarIntValue(powerIncrease), instruction);

            // Set up the new power operation
            instruction.Left = opL.Left;
            instruction.Operation = new OpcodeMathPower();
            instruction.OverwriteSourceLocation(opL.SourceLine, opL.SourceColumn);

            // Reuse the old power instruction for the addition/subtraction
            instruction.Right = tempL;
            opL.Left = powConst;
            opL.Operation = new OpcodeMathAdd();
            opL.OverwriteSourceLocation(divLine, divColumn);

            // Attempt constant folding afterwards
            instruction.Right = ConstantFolding.AttemptReduction(opL);

            // Special case for when N == 2 afterwards
            // then revert back to X * X
            if (instruction.Right is IRConstant constantR &&
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
            IRTemp tempR = (IRTemp)instruction.Right;
            instruction.Right = new IRConstant(new Encapsulation.ScalarIntValue(3), tempR.Parent);
        }

        private static void CombinePowers(IRBinaryOp instruction)
            => CombinePowers(instruction, new OpcodeMathAdd());
        private static void DividePowers(IRBinaryOp instruction)
            => CombinePowers(instruction, new OpcodeMathSubtract());
        private static void CombinePowers(IRBinaryOp instruction, BinaryOpcode newOperation)
        {
            // X^N*X^M=X^(N+M)
            // Assumes |N + M| < 2^31 - 1 (for integer scalars) or 2^53 (for double scalars)

            IRTemp tempL = (IRTemp)instruction.Left;
            IRTemp tempR = (IRTemp)instruction.Right;
            IRBinaryOp opL = (IRBinaryOp)tempL.Parent;
            IRBinaryOp opR = (IRBinaryOp)tempR.Parent;

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
            if (instruction.Right is IRConstant constantR &&
                Encapsulation.ScalarIntValue.Two.Equals(constantR.Value))
            {
                instruction.Right = instruction.Left;
                instruction.Operation = new OpcodeMathMultiply();
            }
        }
    }
}
