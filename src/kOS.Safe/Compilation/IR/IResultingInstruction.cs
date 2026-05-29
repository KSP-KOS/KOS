using System;

namespace kOS.Safe.Compilation.IR
{
    /// <summary>
    /// Represents instructions that push a value back onto the stack.
    /// </summary>
    public interface IResultingInstruction : IEvaluatableToConstant, IInterimOperand
    {
        ushort OpcodeCount { get; }
    }

    public interface IEvaluatableToConstant
    {
        /// <summary>
        /// Gets a value indicating whether this instance is invariant.
        /// That is, if the value of the operand can be known at
        /// compile time.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is invariant; otherwise, <c>false</c>.
        /// </value>
        bool IsInvariant { get; }

        /// <summary>
        /// Evaluates the instruction to a constant value.
        /// </summary>
        /// <remarks>This method should only be valid if <see cref="IInterimOperand.IsInvariant"/> is <c>true</c>.</remarks>
        /// <returns>A constant value that equates to the value of the runtime result of this instruction.</returns>
        /// <exception cref="InvalidOperationException">If this instruction is not invariant. <seealso cref="IInterimOperand.IsInvariant"/></exception>
        InterimConstantValue Evaluate();
    }
}