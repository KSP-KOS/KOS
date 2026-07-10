using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Compilation.Optimization;

namespace kOS.Safe.Compilation.IR
{
    public partial class BasicBlock
    {
        public static BasicBlock InsertBlockBetween(BasicBlock precursor, BasicBlock successor, bool useHeaderScope = false)
        {
            if (!precursor.Successors.Contains(successor))
                throw new InvalidOperationException("The successor block must be a successor of the precursor block.");
            BasicBlock betweenBlock = new BasicBlock(precursor.CodeComponent, precursor.EndIndex, successor.StartIndex)
            {
                Scope = useHeaderScope ? precursor.Scope : successor.Scope,
                ExtendedBlock = successor.ExtendedBlock,
                IsExecutable = successor.IsExecutable
            };
            betweenBlock.TriggerPropagationBlacklist.UnionWith(successor.TriggerPropagationBlacklist);
            foreach (var key in successor.TriggerUnsetBlacklist.Keys)
                betweenBlock.TriggerUnsetBlacklist[key] = successor.TriggerUnsetBlacklist[key];
            betweenBlock.IncomingVariableDefinitions = new Dictionary<(string, IRScope), SSADefinition>();
            foreach (var key in successor.IncomingVariableDefinitions.Keys)
                betweenBlock.IncomingVariableDefinitions[key] = successor.IncomingVariableDefinitions[key];
            betweenBlock.FallthroughJump = new IRJump(betweenBlock, successor, -1, -1);
            betweenBlock.AddSuccessor(successor);
            precursor.AddSuccessor(betweenBlock);
            precursor.RemoveSuccessor(successor);
            if (precursor.FallthroughJump != null)
                precursor.FallthroughJump = new IRJump(precursor,
                    betweenBlock,
                    precursor.FallthroughJump.SourceLine,
                    precursor.FallthroughJump.SourceColumn);
            if (precursor.Instructions.Count > 0 &&
                precursor.Instructions[precursor.Instructions.Count - 1] is IRBranch branch)
            {
                if (branch.True == successor)
                    branch.True = betweenBlock;
                if (branch.False == successor)
                    branch.False = betweenBlock;
            }
            betweenBlock.IncomingStackState.AddRange(SingleStaticAssignment.GetOutgoingStack(precursor));
            successor.IncomingStackState.Clear();
            successor.IncomingStackState.AddRange(SingleStaticAssignment.GetOutgoingStack(betweenBlock));
            betweenBlock.CodeComponent.Blocks.Add(betweenBlock);
            return betweenBlock;
        }
        public BasicBlock Split(int newStartIndex)
        {
            if (newStartIndex < 0 ||
                newStartIndex > Instructions.Count - 1)
                throw new ArgumentException(nameof(newStartIndex));
            BasicBlock successorBlock = new BasicBlock(CodeComponent, StartIndex, EndIndex)
            {
                Scope = Scope,
                ExtendedBlock = ExtendedBlock,
                IsExecutable = IsExecutable
            };

            if (Scope.FooterBlock == this)
                Scope.FooterBlock = successorBlock;

            successorBlock.TriggerPropagationBlacklist.UnionWith(TriggerPropagationBlacklist);
            foreach (var key in TriggerUnsetBlacklist.Keys)
                successorBlock.TriggerUnsetBlacklist[key] = TriggerUnsetBlacklist[key];
            successorBlock.IncomingVariableDefinitions = new Dictionary<(string, IRScope), SSADefinition>();
            foreach (var key in IncomingVariableDefinitions.Keys)
                successorBlock.IncomingVariableDefinitions[key] = IncomingVariableDefinitions[key];
            FallthroughJump = new IRJump(this,
                successorBlock,
                Instructions[Math.Max(newStartIndex - 1, 0)].SourceLine,
                Instructions[Math.Max(newStartIndex - 1, 0)].SourceColumn);

            foreach (BasicBlock successor in Successors)
                successorBlock.AddSuccessor(successor);
            AddSuccessor(successorBlock);
            foreach (BasicBlock successor in Successors.Where(b => b != successorBlock).ToArray())
                RemoveSuccessor(successor);
            CodeComponent.Blocks.Add(successorBlock);

            for (int i = newStartIndex; i < Instructions.Count; i++)
            {
                foreach (IRInstruction instruction in Instructions[i].DepthFirstInstructions())
                    instruction.Block = successorBlock;
                successorBlock.Instructions.Add(Instructions[i]);
            }
            Instructions.RemoveRange(newStartIndex, successorBlock.Instructions.Count);

            return successorBlock;
        }
        public static void Stitch(BasicBlock before, BasicBlock after, IEnumerable<BasicBlock> pattern)
        {
            if (pattern == null || !pattern.Any())
                throw new ArgumentException($"{nameof(pattern)} cannot be null or empty.");
            if (before.Successors.Count > 1 ||
                (before.Successors.Count == 1 &&
                !before.Successors.Contains(after)))
                throw new ArgumentException("The 'before' block must have either zero successors or only the 'after' block.");

            BasicBlock patternRoot = pattern.First();
            while (patternRoot.Dominator != null &&
                pattern.Contains(patternRoot.Dominator))
                patternRoot = patternRoot.Dominator;

            before.AddSuccessor(patternRoot);
            if (before.FallthroughJump != null)
                before.FallthroughJump.Target = patternRoot;
            else
                before.FallthroughJump = new IRJump(before, patternRoot, before.Instructions.LastOrDefault()?.SourceLine ?? -1, before.Instructions.LastOrDefault()?.SourceColumn ?? -1);
            if (before.Instructions.LastOrDefault() is IRJump)
                before.Instructions.RemoveAt(before.Instructions.Count - 1);
            
            foreach (BasicBlock returnBlock in pattern.Where(b => b.PostDominator == null || b.PostDominator is SyntheticReturnBlock))
            {
                returnBlock.AddSuccessor(after);
                if (returnBlock.FallthroughJump != null)
                    returnBlock.FallthroughJump.Target = after;
                else
                    returnBlock.FallthroughJump = new IRJump(returnBlock, after, returnBlock.Instructions.LastOrDefault()?.SourceLine ?? -1, returnBlock.Instructions.LastOrDefault()?.SourceColumn ?? -1);
                if (returnBlock.Instructions.LastOrDefault() is IRJump)
                    returnBlock.Instructions.RemoveAt(returnBlock.Instructions.Count - 1);


                SyntheticReturnBlock syntheticReturn = (SyntheticReturnBlock)returnBlock.Successors.FirstOrDefault(b => b is SyntheticReturnBlock);
                while (syntheticReturn != null)
                {
                    returnBlock.RemoveSuccessor(syntheticReturn);
                    syntheticReturn = (SyntheticReturnBlock)returnBlock.Successors.FirstOrDefault(b => b is SyntheticReturnBlock);
                }
            }
            if (before.Successors.Contains(after))
                before.RemoveSuccessor(after);
            else
            {
                before.EstablishDominance();
                after.EstablishPostDominance();
            }

            before.CodeComponent.Blocks.AddRange(pattern.Where(b => !before.CodeComponent.Blocks.Contains(b)));
        }
        public static IEnumerable<BasicBlock> ClonePattern(IEnumerable<BasicBlock> pattern, bool stackAdoptsTypeHints = false, Dictionary<(string, IRScope), SSADefinition> incomingVariables = null)
        {
            if (pattern == null || !pattern.Any())
                return Enumerable.Empty<BasicBlock>();

            Dictionary<BasicBlock, BasicBlock> replacementBlocks = new Dictionary<BasicBlock, BasicBlock>();
            foreach (BasicBlock block in pattern)
                replacementBlocks[block] = new BasicBlock(block.CodeComponent, block.StartIndex, block.EndIndex);
            Dictionary<IRScope, IRScope> replacementScopes = new Dictionary<IRScope, IRScope>();
            foreach (BasicBlock block in replacementBlocks.Keys)
            {
                if (replacementScopes.ContainsKey(block.Scope))
                    continue;
                if (replacementBlocks.ContainsKey(block.Scope.HeaderBlock))
                    replacementScopes[block.Scope] = CloneScope(block.Scope, replacementBlocks, replacementScopes);
            }
            Dictionary<IRInstruction, IRInstruction> replacementInstructions = new Dictionary<IRInstruction, IRInstruction>();
            foreach (KeyValuePair<BasicBlock, BasicBlock> replacementPair in replacementBlocks)
                CloneBlock(replacementPair.Value, replacementPair.Key, replacementBlocks, replacementScopes, replacementInstructions);

            foreach (IRScope originalScope in replacementScopes.Keys)
            {
                IRScope newScope = replacementScopes[originalScope];
                foreach (IRAssign assignment in originalScope.Assignments)
                {
                    if (replacementInstructions.TryGetValue(assignment, out IRInstruction newAssignment))
                        newScope.Assignments.Add((IRAssign)newAssignment);
                }
            }

            BasicBlock root = replacementBlocks[pattern.First()];
            while (root.Dominator != null)
                root = root.Dominator;
            SingleStaticAssignment.BuildPhis(root, stackAdoptsTypeHints, incomingVariables);
            foreach (BasicBlock block in replacementBlocks.Values)
                SingleStaticAssignment.ApplyUses(block);

            return replacementBlocks.Values;
        }
        private static IRScope CloneScope(IRScope original, Dictionary<BasicBlock, BasicBlock> replacementBlocks, Dictionary<IRScope, IRScope> replacementScopes)
        {
            if (replacementBlocks.ContainsKey(original.ParentScope.HeaderBlock))
                replacementScopes[original.ParentScope] = CloneScope(original.ParentScope, replacementBlocks, replacementScopes);

            IRScope scope;
            if (replacementScopes.TryGetValue(original.ParentScope, out IRScope newParent))
                scope = new IRScope(newParent, replacementBlocks[original.HeaderBlock]);
            else
                scope = new IRScope(original.ParentScope, replacementBlocks[original.HeaderBlock]);

            if (replacementBlocks.TryGetValue(original.FooterBlock, out BasicBlock newFooter))
                scope.FooterBlock = newFooter;
            else
                scope.FooterBlock = original.FooterBlock;

            return scope;
        }
        private static void CloneBlock(BasicBlock block, BasicBlock original, Dictionary<BasicBlock, BasicBlock> replacements, Dictionary<IRScope, IRScope> replacementScopes, Dictionary<IRInstruction, IRInstruction> replacementInstructions)
        {
            if (original.Dominator != null &&
                replacements.ContainsKey(original.Dominator))
                block.Dominator = replacements[original.Dominator];
            if (original.PostDominator != null &&
                replacements.ContainsKey(original.PostDominator))
                block.PostDominator = replacements[original.PostDominator];
            if (original.FallthroughJump != null)
            {
                block.FallthroughJump = (IRJump)original.FallthroughJump.Clone(block);
                if (replacements.TryGetValue(block.FallthroughJump.Target, out BasicBlock target))
                    block.FallthroughJump.Target = target;
            }

            block.IsExecutable = original.IsExecutable;

            foreach ((string, IRScope) key in original.Phis.Keys)
                block.Phis[key] = original.Phis[key];

            foreach (BasicBlock predecessor in original.Predecessors)
            {
                if (replacements.TryGetValue(predecessor, out BasicBlock replacementPredecessor))
                    replacementPredecessor.AddSuccessor(block);
                else
                    predecessor.AddSuccessor(block);
            }
            foreach (BasicBlock successor in original.Successors)
            {
                if (replacements.TryGetValue(successor, out BasicBlock replacementSuccessor))
                    block.AddSuccessor(replacementSuccessor);
                else
                    block.AddSuccessor(successor);
            }

            foreach (IRInstruction instruction in original.Instructions)
            {
                IRInstruction newInstruction = instruction.Clone(block, false);
                block.Instructions.Add(newInstruction);
                if (instruction is IRAssign ||
                    instruction is IRUnset)
                    replacementInstructions[instruction] = newInstruction;
                if (newInstruction is IRBranch branch)
                {
                    if (replacements.TryGetValue(branch.True, out BasicBlock newTarget))
                        branch.True = newTarget;
                    if (replacements.TryGetValue(branch.False, out newTarget))
                        branch.False = newTarget;
                }
                if (newInstruction is IRJump jump)
                {
                    if (replacements.TryGetValue(jump.Target, out BasicBlock newTarget))
                        jump.Target = newTarget;
                }
                if (newInstruction is IRJumpStack jumpStack)
                {
                    for (int i = 0; i < jumpStack.Targets.Count; i++)
                    {
                        if (replacements.TryGetValue(jumpStack.Targets[i], out BasicBlock newTarget))
                            jumpStack.Targets[i] = newTarget;
                    }
                }
            }

            if (replacementScopes.TryGetValue(original.Scope, out IRScope newScope))
                block.Scope = newScope;
            else
                block.Scope = original.Scope;
        }
    }
}
