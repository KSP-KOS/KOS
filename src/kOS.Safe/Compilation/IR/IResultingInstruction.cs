using System;

namespace kOS.Safe.Compilation.IR
{
    public interface IResultingInstruction
    {
        IRValue Result { get; }
        Type ResultType { get; }
    }
}