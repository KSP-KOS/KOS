using System.Collections.Generic;
using kOS.Safe.Compilation.IR;

namespace kOS.Safe.Compilation.Optimization
{
    /// <summary>
    /// Represents an optimization pass to be performed.
    /// </summary>
    public interface IOptimizationPass
    {
        /// <summary>
        /// Gets the optimization level associated with this pass.
        /// </summary>
        /// <remarks>
        /// The result should be constant across all instances.
        /// </remarks>
        OptimizationLevel OptimizationLevel { get; }
        /// <summary>
        /// An index on which passes are sorted before being called.
        /// </summary>
        /// <remarks>
        /// The result should be constant across all instances.
        /// </remarks>
        short SortIndex { get; }
    }
    public interface IOptimizationPass<T> : IOptimizationPass
    {
        /// <summary>
        /// Executes the optimization pass.
        /// </summary>
        /// <param name="code">The code representation to be optimized.</param>
        void ApplyPass(List<T> code);
    }
    public interface IHolisticOptimizationPass : IOptimizationPass
    {
        /// <summary>
        /// Executes the optimization pass.
        /// </summary>
        /// <param name="codePart">The code to be optimized.</param>
        void ApplyPass(IRCodePart codePart);
    }
    public interface ILinkedOptimizationPass : IOptimizationPass
    {
        /// <summary>
        /// Allows the Optimizer to reference itself before executing the
        /// optimization pass.
        /// </summary>
        Optimizer Optimizer { set; }
        /// <summary>
        /// Executes the optimization pass.
        /// </summary>
        void ApplyPass();
    }
}
