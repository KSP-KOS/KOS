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
            else
                throw new NotImplementedException();
            yield return operandInstruction;
        }

        public static bool ContentsEqual<TKey, TValue>(this IDictionary<TKey, TValue> a, IDictionary<TKey, TValue> b, IEqualityComparer<TValue> comparer = null)
        {
            if (comparer == null)
                comparer = EqualityComparer<TValue>.Default;

            if (a.Count != b.Count)
                return false;
            foreach (TKey key in a.Keys)
            {
                if (!b.TryGetValue(key, out TValue value) || !comparer.Equals(a[key], value))
                    return false;
            }
            return true;
        }

        public static IEnumerable<IInterimOperand> GetOperandsWhere(this IOperandInstructionBase instruction, Predicate<IInterimOperand> predicate)
        {
            if (instruction is ISingleOperandInstruction singleOperandInstruction)
            {
                if (singleOperandInstruction.Operand is IOperandInstructionBase operandInstruction)
                    foreach (IInterimOperand operand in operandInstruction.GetOperandsWhere(predicate))
                        yield return operand;
                if (predicate(singleOperandInstruction.Operand))
                    yield return singleOperandInstruction.Operand;
            }
            else if (instruction is IMultipleOperandInstruction multipleOperandInstruction)
            {
                foreach (IInterimOperand operand in multipleOperandInstruction.Operands)
                {
                    if (operand is IOperandInstructionBase operandInstruction)
                        foreach (IInterimOperand op in operandInstruction.GetOperandsWhere(predicate))
                            yield return op;
                    if (predicate(operand))
                        yield return operand;
                }
            }
            else
                throw new NotImplementedException();
        }
    }
}
