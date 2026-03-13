using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Compilation.IR;
using kOS.Safe.Exceptions;

namespace kOS.Safe.Compilation.Optimization.Passes
{
    public class UnnecessaryScopeRemoval : IOptimizationPass<BasicBlock>
    {
        public OptimizationLevel OptimizationLevel => OptimizationLevel.Minimal;

        public short SortIndex => 32000;

        public void ApplyPass(List<BasicBlock> code)
        {
            HashSet<IRScope> scopes = new HashSet<IRScope>();
            HashSet<IRScope> removedScopes = new HashSet<IRScope>();
            foreach (BasicBlock block in code)
                scopes.Add(block.Scope);

            foreach (IRScope scope in scopes)
            {
                if (scope.Variables.Any())
                    continue;

                if (scope.IsGlobalScope)
                    continue;

                CollapseScope(scope);

                RemovePushScope(scope.HeaderBlock);

                RemovePopScope(scope.FooterBlock);

                removedScopes.Add(scope);
            }

            scopes.ExceptWith(removedScopes);
            foreach (BasicBlock block in code)
            {
                if (block.Instructions.Count > 0 &&
                    !(block.Instructions.Last() is IRReturn))
                    continue;

                AttemptPopReturnCollapse(block);
            }
        }

        private static void CollapseScope(IRScope scope)
        {
            IRScope newScope = scope.ParentScope;
            foreach (IRScope childScope in scope.Children.ToArray())
                childScope.ParentScope = newScope;
            foreach (BasicBlock block in scope.Blocks.ToArray())
                block.Scope = newScope;
            scope.ParentScope = null;
        }

        private static void RemovePushScope(BasicBlock header)
        {
            if (header.Instructions[0] is IRNoStackInstruction instruction &&
                instruction.Operation is OpcodePushScope)
                header.Instructions.RemoveAt(0);
            else
                throw new KOSYouShouldNeverSeeThisException($"The first instruction ({header.Instructions[0]}) in scope header block {header} was not OpcodePushScope.");
        }

        private static void RemovePopScope(BasicBlock footer)
        {
            int lastIndex = footer.Instructions.Count - 1;
            IRInstruction lastInstruction = footer.Instructions[lastIndex];

            if (lastInstruction is IRReturn ret)
            {
                ret.Depth -= 1;
                if (ret.Depth < 0)
                    throw new KOSYouShouldNeverSeeThisException($"After removing unecessary scopes, the return statement in {footer} had negative depth.");
            }
            else if (lastInstruction is IRNoStackInstruction popInstruction &&
                popInstruction.Operation is OpcodePopScope)
            {
                footer.Instructions.RemoveAt(lastIndex);
            }
            else
                throw new KOSYouShouldNeverSeeThisException($"The last instruction ({lastInstruction}) in scope footer block {footer} was not OpcodePopScope or OpcodeReturn.");
        }

        private static void AttemptPopReturnCollapse(BasicBlock returnBlock)
        {
            // Don't do anything if there is more than one predecessor
            int numPredecessors = returnBlock.Predecessors.Count;
            if (numPredecessors == 0 || numPredecessors > 1)
                return;

            BasicBlock predecessorBlock = returnBlock.Predecessors.First();

            if (predecessorBlock.Instructions.Last() is IRNoStackInstruction popInstruction &&
                popInstruction.Operation is OpcodePopScope)
            {
                predecessorBlock.Instructions.RemoveAt(predecessorBlock.Instructions.Count - 1);
                ((IRReturn)returnBlock.Instructions.Last()).Depth += 1;
                returnBlock.Scope = predecessorBlock.Scope;
                returnBlock.Scope.FooterBlock = returnBlock;
                //BasicBlock.Merge(predecessorBlock, returnBlock);

                // Since a pop closes a BasicBlock, then if there are no instructions remaining,
                // there's a chance that the pop's predecessor also ends with a pop that we can remove,
                // if the pop only has a single predecessor.
                if (predecessorBlock.Instructions.Count == 0)
                {
                    AttemptPopReturnCollapse(returnBlock);
                }
            }
        }
    }
}
