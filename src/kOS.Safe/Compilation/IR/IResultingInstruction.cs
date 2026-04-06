using System;

namespace kOS.Safe.Compilation.IR
{
    /// <summary>
    /// Represents instructions that push a value back onto the stack.
    /// </summary>
    public interface IResultingInstruction
    {
        /// <summary>
        /// Gets the representation of the value that would be pushed
        /// to the stack.
        /// </summary>
        IRValue Result { get; }
        /// <summary>
        /// Gets the <see cref="Type"/> of the result, for type
        /// inferencing purposes.
        /// </summary>
        Type ResultType { get; }
    }
}