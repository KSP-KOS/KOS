using System.Collections.Generic;
using kOS.Safe.Compilation.IR;

namespace kOS.Safe.Compilation.Optimization
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
        Optimizer Optimizer { set; }
        void ApplyPass();
    }
}
