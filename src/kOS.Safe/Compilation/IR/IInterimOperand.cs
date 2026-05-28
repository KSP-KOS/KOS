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

        /// <summary>
        /// Tests equality between operands.
        /// </summary>
        /// <returns><c>true</c> if the operands are equal, otherwise <c>false</c>.</returns>
        bool Equals(IInterimOperand other);

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <param name="block">The block within which this instance is being cloned.</param>
        IInterimOperand Clone(BasicBlock block);
    }
}
