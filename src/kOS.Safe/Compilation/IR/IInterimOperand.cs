using System;
using System.Collections.Generic;

namespace kOS.Safe.Compilation.IR
{
    public interface IInterimOperand
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
        /// Gets the type of the operand.
        /// </summary>
        Type Type { get; }

        /// <summary>
        /// Emits the opcodes that will generate the operand.
        /// </summary>
        /// <returns>One or more <see cref="Opcode"/> instances.</returns>
        IEnumerable<Opcode> EmitOpcodes();
    }
}
