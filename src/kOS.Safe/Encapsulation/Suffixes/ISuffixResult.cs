using System;
using kOS.Safe.Execution;

namespace kOS.Safe.Encapsulation.Suffixes
{
    public interface ISuffixResult
    {
        Structure Value { get; }

        bool HasValue { get; }

        void Invoke(ICpu cpu);
    }
}