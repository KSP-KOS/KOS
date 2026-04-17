using System;
using System.Collections.Generic;

namespace kOS.Safe.Compilation.IR
{
    /// <summary>
    /// Represents an instruction that operates on one or more operands.
    /// </summary>
    public interface IOperandInstructionBase
    {
        /// <summary>
        /// Applies an action for each operand.
        /// </summary>
        void ForEachOperand(Action<IRValue> action);

        /// <summary>
        /// Mutates each operand by replacing it with the result of <paramref name="mutateFunc"/> applied to that operand.
        /// </summary>
        /// <param name="mutateFunc">The mutation function.</param>
        void MutateEachOperand(Func<IRValue, IRValue> mutateFunc);
    }

    /// <summary>
    /// Represents instructions which operate on a single operand.
    /// </summary>
    public interface ISingleOperandInstruction : IOperandInstructionBase
    {
        /// <summary>
        /// Gets or sets the operand for this instruction.
        /// </summary>
        IRValue Operand { get; set; }
    }
    /// <summary>
    /// Represents instructions which operate on multiple operands.
    /// </summary>
    public interface IMultipleOperandInstruction : IOperandInstructionBase
    {
        /// <summary>
        /// Gets the collection of operands for the instruction.
        /// </summary>
        IEnumerable<IRValue> Operands { get; }
        /// <summary>
        /// Gets the number of operands for this instruction.
        /// </summary>
        int OperandCount { get; }
    }
}
