using System.Collections.Generic;

namespace kOS.Safe.Compilation.IR
{
    public interface IOperandInstructionBase
    { }
    public interface ISingleOperandInstruction : IOperandInstructionBase
    {
        IRValue Operand { get; }
    }
    public interface IMultipleOperandInstruction : IOperandInstructionBase
    {
        IEnumerable<IRValue> Operands { get; }
    }
}
