using System;
using System.Collections.Generic;

namespace kOS.Safe.Compilation.IR
{
    public interface IStackTransferObject
    {
        IReadOnlyCollection<StackTransferPhi> Controllers { get; }
        IReadOnlyCollection<IRParameter> References { get; }
        IEnumerable<IStackTransferObject> StackTransferObjects { get; }
        bool IsInvariant { get; }
        bool IsResolvable { get; }
        bool IsSelfResolvable { get; }
        IInterimOperand Value { get; set; }
        Type Type { get; }
        void AddController(StackTransferPhi phi);
        void AddReference(IRParameter reference);
        void RemoveReference(IRParameter reference);
    }
}
