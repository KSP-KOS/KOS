using System;
using System.Collections.Generic;
using kOS.Safe.Compilation.IR;

namespace kOS.Safe.Compilation.Optimization.Passes
{
    public class CallSuffixElimination : IOptimizationPass<BasicBlock>
    {
        public OptimizationLevel OptimizationLevel => OptimizationLevel.Minimal;
        public short SortIndex => -12000;

        public void ApplyPass(IEnumerable<BasicBlock> blocks)
        {
            foreach (BasicBlock block in blocks)
            {
                foreach (IOperandInstructionBase operandInstruction in block.DepthFirstOperandInstructions())
                    operandInstruction.ForEachOperand(AttemptReplacement);
            }
        }

        private static void AttemptReplacement(IInterimOperand operand)
        {
            if (operand is IRCall call)
                ReplaceSuffx(call);
        }

        public static void ReplaceSuffx(IRCall call)
        {
            if (call.Direct)
                return;

            if (!(call.TargetMethod is IRSuffixGet suffixGet) ||
                !suffixGet.Suffix.Equals("call", StringComparison.OrdinalIgnoreCase))
                return;

            if (suffixGet.Object is IInterimVariableReference delegateReference)
            {
                call.Direct = true;
                call.Function = delegateReference.Name.Replace("$", "");
            }
            else
            {
                call.TargetMethod = suffixGet.Object;
            }
        }
    }
}
