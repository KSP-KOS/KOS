using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kOS.Safe.Compilation.IR
{
    public class IROptimizer
    {
        public OptimizationLevel OptimizationLevel { get; }

        public IROptimizer(OptimizationLevel optimizationLevel)
        {
            OptimizationLevel = optimizationLevel;
        }

        public List<BasicBlock> Optimize(List<BasicBlock> blocks)
        {
            return blocks;
        }
    }
}
