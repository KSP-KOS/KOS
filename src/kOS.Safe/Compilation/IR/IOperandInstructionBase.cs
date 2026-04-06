using System.Collections.Generic;

namespace kOS.Safe.Compilation.IR
{
    /// <summary>
    /// Represents instructions which operate on a single operand.
    /// </summary>
    public interface ISingleOperandInstruction
    {
        /// <summary>
        /// Gets or sets the operand for this instruction.
        /// </summary>
        IRValue Operand { get; set; }
    }
    /// <summary>
    /// Represents instructions which operate on multiple operands.
    /// </summary>
    public interface IMultipleOperandInstruction
    {
        /// <summary>
        /// Gets the collection of operands for the instruction.
        /// </summary>
        IEnumerable<IRValue> Operands { get; }
        /// <summary>
        /// Gets or sets the operand at the specified index.
        /// </summary>
        /// <param name="index">The index to get or set. The meaning of this will be implementation-specific.</param>
        IRValue this[int index] { get; set; }
        /// <summary>
        /// Gets the number of operands for this instruction.
        /// </summary>
        int OperandCount { get; }
    }
}
