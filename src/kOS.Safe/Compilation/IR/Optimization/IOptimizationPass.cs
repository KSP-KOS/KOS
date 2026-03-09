using System;
using System.Collections.Generic;

namespace kOS.Safe.Compilation.IR.Optimization
{
    public interface IOptimizationPass
    {
        OptimizationLevel OptimizationLevel { get; }
        short SortIndex { get; }
    }
    public interface IOptimizationPass<T> : IOptimizationPass
    {
        void ApplyPass(List<T> code);
    }
    public interface IHolisticOptimizationPass : IOptimizationPass
    {
        void ApplyPass(IRCodePart codePart);
    }
    public interface ILinkedOptimizationPass : IOptimizationPass
    {
        IROptimizer Optimizer { set; }
        void ApplyPass();
    }
}
