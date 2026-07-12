using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Compilation.IR;

namespace kOS.Safe.Compilation.Optimization
{
    public static class OptimizationTools
    {
        public static IEnumerable<IOperandInstructionBase> DepthFirstOperandInstructions(this BasicBlock block)
        {
            foreach (IOperandInstructionBase operandInstruction in block.Instructions.DepthFirst())
                yield return operandInstruction;
            if (block.Continuation is IOperandInstructionBase operandContinuation)
                foreach (IOperandInstructionBase operandInstruction in operandContinuation.DepthFirst())
                    yield return operandInstruction;
        }

        public static IEnumerable<IRInstruction> DepthFirstInstructions(this IEnumerable<IRInstruction> instructions)
            => instructions.SelectMany(DepthFirstInstructions);
        
        public static IEnumerable<IRInstruction> DepthFirstInstructions(this IRInstruction instruction)
        {
            if (instruction is IOperandInstructionBase operandInstruction)
            {
                foreach (IOperandInstructionBase predecessor in DepthFirst(operandInstruction))
                    if (predecessor is IRInstruction predecessorInstruction)
                        yield return predecessorInstruction;
            }
            else
                yield return instruction;
        }

        public static IEnumerable<IOperandInstructionBase> DepthFirst(this IEnumerable<IRInstruction> instructions)
            => instructions.SelectMany(DepthFirst);

        public static IEnumerable<IOperandInstructionBase> DepthFirst(this IRInstruction instruction)
            => instruction is IOperandInstructionBase operandInstruction ?
            DepthFirst(operandInstruction) :
            Enumerable.Empty<IOperandInstructionBase>();

        public static IEnumerable<IOperandInstructionBase> DepthFirst(this IOperandInstructionBase operandInstruction)
        {
            if (operandInstruction is ISingleOperandInstruction singleOperandInstruction)
            {
                if (singleOperandInstruction.Operand is IOperandInstructionBase op)
                    foreach (IOperandInstructionBase predecessor in DepthFirst(op))
                        yield return predecessor;
            }
            else if (operandInstruction is IMultipleOperandInstruction multipleOperandInstruction)
            {
                foreach (IInterimOperand operand in multipleOperandInstruction.Operands)
                {
                    if (operand is IOperandInstructionBase op)
                        foreach (IOperandInstructionBase predecessor in DepthFirst(op))
                            yield return predecessor;
                }
            }
            yield return operandInstruction;
        }
    }
}
