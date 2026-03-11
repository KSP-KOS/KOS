using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Compilation.IR;

namespace kOS.Safe.Compilation.Optimization
{
    public static class OptimizationTools
    {
        /*public static IEnumerable<IRTemp> FindExpressionsMatching(this IEnumerable<IRInstruction> instructions, Predicate<IRInstruction> predicate)
            => instructions.SelectMany(i => ExpressionMatchDepthFirst(i, predicate));

        public static IEnumerable<IRTemp> FindFirstExpressionsMatching(this IEnumerable<IRInstruction> instructions, Predicate<IRInstruction> predicate)
            => instructions.SelectMany(i => ExpressionMatchBreadthFirst(i, predicate));
        */

        public static IEnumerable<IRInstruction> DepthFirst(this IEnumerable<IRInstruction> instructions)
            => instructions.SelectMany(DepthFirst);
        
        public static IEnumerable<IRInstruction> FindInstructionsMatching(this IEnumerable<IRInstruction> instructions, Predicate<IRInstruction> predicate)
            => instructions.SelectMany(i => InstructionMatchDepthFirst(i, predicate));

        public static IEnumerable<IRInstruction> FindFirstInstructionsMatching(this IEnumerable<IRInstruction> instructions, Predicate<IRInstruction> predicate)
            => instructions.SelectMany(i => InstructionMatchBreadthFirst(i, predicate));

        private static IEnumerable<IRTemp> ExpressionMatchDepthFirst(IRInstruction instruction, Predicate<IRInstruction> predicate)
        {
            if (instruction is ISingleOperandInstruction singleOperandInstruction)
            {
                if (singleOperandInstruction.Operand is IRTemp temp)
                {
                    foreach (IRTemp predecessorMatch in ExpressionMatchDepthFirst(temp.Parent, predicate))
                        yield return predecessorMatch;
                    if (predicate(temp.Parent))
                        yield return temp;
                }
            }
            else if (instruction is IMultipleOperandInstruction multipleOperandInstruction)
            {
                foreach (IRValue operand in multipleOperandInstruction.Operands)
                {
                    if (operand is IRTemp temp)
                    {
                        foreach (IRTemp predecessorMatch in ExpressionMatchDepthFirst(temp.Parent, predicate))
                            yield return predecessorMatch;
                        if (predicate(temp.Parent))
                            yield return temp;
                    }
                }
            }
        }

        private static IEnumerable<IRTemp> ExpressionMatchBreadthFirst(IRInstruction instruction, Predicate<IRInstruction> predicate)
        {
            if (instruction is ISingleOperandInstruction singleOperandInstruction)
            {
                if (singleOperandInstruction.Operand is IRTemp temp)
                {
                    if (predicate(temp.Parent))
                        yield return temp;
                    foreach (IRTemp predecessorMatch in ExpressionMatchBreadthFirst(temp.Parent, predicate))
                        yield return predecessorMatch;
                }
            }
            else if (instruction is IMultipleOperandInstruction multipleOperandInstruction)
            {
                foreach (IRValue operand in multipleOperandInstruction.Operands)
                {
                    if (operand is IRTemp temp && predicate(temp.Parent))
                        yield return temp;
                }
                foreach (IRValue operand in multipleOperandInstruction.Operands)
                {
                    if (operand is IRTemp temp)
                    {
                        foreach (IRTemp predecessorMatch in ExpressionMatchBreadthFirst(temp.Parent, predicate))
                            yield return predecessorMatch;
                    }
                }
            }
        }

        private static IEnumerable<IRInstruction> InstructionMatchDepthFirst(IRInstruction instruction, Predicate<IRInstruction> predicate)
        {
            foreach (IRInstruction match in CrawlTreeForMatch(instruction, predicate, InstructionMatchDepthFirst))
                yield return match;
            if (predicate(instruction))
                yield return instruction;
        }

        private static IEnumerable<IRInstruction> InstructionMatchBreadthFirst(IRInstruction instruction, Predicate<IRInstruction> predicate)
        {
            if (predicate(instruction))
                yield return instruction;
            foreach (IRInstruction match in CrawlTreeForMatch(instruction, predicate, InstructionMatchBreadthFirst))
                yield return match;
        }

        private static IEnumerable<IRInstruction> DepthFirst(IRInstruction instruction)
        {
            if (instruction is ISingleOperandInstruction singleOperandInstruction)
            {
                if (singleOperandInstruction.Operand is IRTemp temp)
                    foreach (IRInstruction predecessor in DepthFirst(temp.Parent))
                        yield return predecessor;
            }
            else if (instruction is IMultipleOperandInstruction multipleOperandInstruction)
            {
                foreach (IRValue operand in multipleOperandInstruction.Operands)
                {
                    if (operand is IRTemp temp)
                        foreach (IRInstruction predecessor in DepthFirst(temp.Parent))
                            yield return predecessor;
                }
            }
            yield return instruction;
        }

        private static IEnumerable<IRInstruction> CrawlTreeForMatch(IRInstruction instruction, Predicate<IRInstruction> predicate, Func<IRInstruction, Predicate<IRInstruction>, IEnumerable<IRInstruction>> searchFunc)
        {
            if (instruction is ISingleOperandInstruction singleOperandInstruction)
            {
                if (singleOperandInstruction.Operand is IRTemp temp)
                    foreach (IRInstruction match in searchFunc(temp.Parent, predicate))
                        yield return match;
            }
            else if (instruction is IMultipleOperandInstruction multipleOperandInstruction)
            {
                foreach (IRValue operand in multipleOperandInstruction.Operands)
                {
                    if (operand is IRTemp temp)
                        foreach (IRInstruction match in searchFunc(temp.Parent, predicate))
                            yield return match;
                }
            }
        }
    }
}
