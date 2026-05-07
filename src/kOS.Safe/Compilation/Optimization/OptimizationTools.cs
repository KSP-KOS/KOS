using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Compilation.IR;

namespace kOS.Safe.Compilation.Optimization
{
    public static class OptimizationTools
    {
        public static IEnumerable<IRInstruction> DepthFirst(this IEnumerable<IRInstruction> instructions)
            => instructions.SelectMany(DepthFirst);
        
        public static IEnumerable<IRInstruction> DepthFirst(this IRInstruction instruction)
        {
            if (instruction is ISingleOperandInstruction singleOperandInstruction)
            {
                if (singleOperandInstruction.Operand is IRInstruction inst)
                    foreach (IRInstruction predecessor in DepthFirst(inst))
                        yield return predecessor;
            }
            else if (instruction is IMultipleOperandInstruction multipleOperandInstruction)
            {
                foreach (IInterimOperand operand in multipleOperandInstruction.Operands)
                {
                    if (operand is IRInstruction inst)
                        foreach (IRInstruction predecessor in DepthFirst(inst))
                            yield return predecessor;
                }
            }
            yield return instruction;
        }
    }
}
