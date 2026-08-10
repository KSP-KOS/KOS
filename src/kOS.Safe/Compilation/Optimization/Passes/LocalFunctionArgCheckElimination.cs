using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Compilation.IR;

namespace kOS.Safe.Compilation.Optimization.Passes
{
    public class LocalFunctionArgCheckElimination : IHolisticOptimizationPass
    {
        public OptimizationLevel OptimizationLevel => OptimizationLevel.Balanced;
        public short SortIndex => -1500;

        public void ApplyPass(IRCodePart codePart)
        {
            foreach (IRCodePart.IRFunction function in codePart.Functions)
                ApplyPass(function);
        }

        private static void ApplyPass(IRCodePart.IRFunction function)
        {
            if (function.Fragments.Count != 1)
                return;

            BasicBlock functionRoot = function.Fragments.First().RootBlock;
            int maxArgs = functionRoot.IncomingStackState.Count;
            int minArgs = functionRoot.IncomingStackState.FindIndex(obj => obj.Controllers.Count > 0);
            if (minArgs < 0)
                minArgs = maxArgs;
            int minUsedArgs = maxArgs;

            foreach (IRCall call in function.CallSites)
            {
                if (call.Arguments.Count < minArgs)
                    throw new Exceptions.KOSCompileException(call,
                        new Exceptions.KOSArgumentMismatchException("Too few arguments were passed to " + call.Function.Replace("$", "").Replace("*", "")));
                if (call.Arguments.Count > maxArgs)
                    throw new Exceptions.KOSCompileException(call,
                        new Exceptions.KOSArgumentMismatchException("Too many arguments were passed to " + call.Function.Replace("$", "").Replace("*", "")));

                if (call.Arguments.Count < minUsedArgs)
                    minUsedArgs = call.Arguments.Count;
            }

            if (function.IsGlobal)
                return;

            List<BasicBlock> blocks = BasicBlock.GetReversePostOrder(functionRoot, BasicBlock.GetSuccessors);
            foreach (BasicBlock block in blocks)
            {
                // Remove TArg checks that are less than the first optional parameter that is ever skipped.
                if (minUsedArgs > minArgs &&
                    block.Continuation is BranchContinuation branch &&
                    branch.Condition is IRNonVarPush testArgBottom &&
                    testArgBottom.Operation is OpcodeTestArgBottom)
                {
                    block.Continuation = new JumpContinuation(branch.False, branch.SourceLine, branch.SourceColumn);
                    branch.True.IsExecutable = false;
                    minUsedArgs--;
                }
                // Remove all ArgB instructions (there should only be one).
                if (block.Instructions.RemoveAll(i => i is IRNoStackInstruction noStackInstruction && noStackInstruction.Operation is OpcodeArgBottom) > 0)
                    break;
            }
        }
    }
}
