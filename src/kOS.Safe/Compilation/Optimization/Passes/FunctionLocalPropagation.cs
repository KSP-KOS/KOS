using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Compilation.IR;

namespace kOS.Safe.Compilation.Optimization.Passes
{
    public class FunctionLocalPropagation : IHolisticOptimizationPass
    {
        public OptimizationLevel OptimizationLevel => OptimizationLevel.Aggressive;

        public short SortIndex => 21;

        public void ApplyPass(IRCodePart codePart)
        {
            foreach (IRCodePart.IRFunction function in codePart.Functions.Where(f => !f.IsGlobal && f.CallSites.Count > 0))
                ApplyPass(function);
        }

        public static void ApplyPass(IRCodePart.IRFunction function)
        {
            Dictionary<IRInstruction, HashSet<IInterimVariableReference>> reachableVariables = function.CodePart.ReachableVariables;
            
            // For each external read, check if the definition is equal for each call site.
            foreach (string variable in function.ExternalReads)
            {
                List<IInterimVariableReference> callSiteReferences = function.CallSites.Select(c => reachableVariables[c].FirstOrDefault(r => variable.Equals(r.Name, StringComparison.OrdinalIgnoreCase))).ToList();

                // A null or generic variable reference means it is not known to be present
                // at this call site.
                // TODO: There is an opportunity for improvement here to find the values
                // for nested function calls. For now, no optimization is done in that case.
                if (callSiteReferences.Any(r => r == null || r is InterimVariableReference))
                    continue;
                // Multiple possible values will be left as-is.
                // Distinct uses the default equality comparer,
                // which in this case checks the value of the
                // variable definition (even accounting for
                // executable blocks).
                if (callSiteReferences.Distinct().Count() != 1)
                    continue;

                IInterimVariableReference resolvedReference = callSiteReferences.First();
                foreach (IRCodePart.IRFunction.IRFunctionFragment fragment in function.Fragments)
                {
                    foreach (BasicBlock block in fragment.Blocks)
                    {
                        foreach (IRInstruction instruction in block.Instructions.DepthFirst())
                        {
                            if (instruction is IOperandInstructionBase operandInstruction)
                                operandInstruction.MutateEachOperand(op =>
                                {
                                    if (op is InterimVariableReference reference &&
                                    reference.Name.Equals(resolvedReference.Name, StringComparison.OrdinalIgnoreCase))
                                        return resolvedReference;
                                    return op;
                                });
                        }
                    }
                }
            }
        }
    }
}
