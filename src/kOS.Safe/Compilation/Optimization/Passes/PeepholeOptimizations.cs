using System;
using System.Collections.Generic;
using kOS.Safe.Compilation.IR;

namespace kOS.Safe.Compilation.Optimization.Passes
{
    public class PeepholeOptimizations : IOptimizationPass<IRInstruction>, ILinkedOptimizationPass
    {
        public Optimizer Optimizer { get; set; }
        public OptimizationLevel OptimizationLevel => OptimizationLevel.Minimal;

        public short SortIndex => 1050;

        public void ApplyPass(IEnumerable<IRInstruction> codeList)
        {
            List<IRInstruction> code = (List<IRInstruction>)codeList;
            IInterimOperand OperandPeepholeFilter_Internal(IInterimOperand operand)
                => OperandPeepholeFilter(operand, Optimizer.AllowClobberBuiltins);

            for (int i = 0; i < code.Count; i++)
            {
                IRInstruction instruction = code[i];
                foreach (IRInstruction nestedInstruction in instruction.DepthFirst())
                {
                    if (nestedInstruction is IOperandInstructionBase operandInstruction)
                        operandInstruction.MutateEachOperand(OperandPeepholeFilter_Internal);

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

        private static IInterimOperand OperandPeepholeFilter(IInterimOperand operand, bool allowClobberBuiltins)
        {
            // These calls may not be what is expected if clobbering is permitted.
            if (!allowClobberBuiltins)
            {
                // Replace parameterless suffix method calls with get member
                if (operand is IRCall suffixCall &&
                    !suffixCall.Direct &&
                    suffixCall.Arguments.Count == 0 &&
                    suffixCall.IndirectMethod is IRSuffixGetMethod)
                {
                    return ReplaceParameterlessSuffix(suffixCall);
                }

                // Replace calls to VectorDotProduct with multiplication
                if (operand is IRCall vDotCall &&
                    (vDotCall.Function == "vdot" || vDotCall.Function == "vectordotproduct") &&
                    vDotCall.Arguments.Count == 2)
                {
                    return ReplaceVectorDotProduct(vDotCall);
                }
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
            branch.Condition = AlgebraicSimplifications.GetDoubleNestedValue(branch);
            branch.PreferFalse = !branch.PreferFalse;
            (branch.True, branch.False) = (branch.False, branch.True);
        }
    }
}
