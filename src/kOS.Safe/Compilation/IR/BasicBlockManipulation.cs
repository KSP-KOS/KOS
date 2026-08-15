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

            betweenBlock.Dominator = precursor;
            betweenBlock.PostDominator = successor;
            betweenBlock.Continuation = new JumpContinuation(successor, -1, -1);

            switch (precursor.Continuation)
            {
                case null:
                    precursor.Continuation = new JumpContinuation(betweenBlock, -1, -1);
                    break;
                case JumpContinuation jump:
                    jump.Target = betweenBlock;
                    break;
                case BranchContinuation branch:
                    if (branch.True == successor)
                        branch.True = betweenBlock;
                    if (branch.False == successor)
                        branch.False = betweenBlock;
                    break;
                case JumpStackContinuation _:
                    throw new NotImplementedException();
                default:
                    throw new NotImplementedException();
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

            if (Scope.FooterBlocks.Contains(this))
            {
                Scope.FooterBlocks.Remove(this);
                Scope.FooterBlocks.Add(successorBlock);
            }

            successorBlock.TriggerPropagationBlacklist.UnionWith(TriggerPropagationBlacklist);
            foreach (var key in TriggerUnsetBlacklist.Keys)
                successorBlock.TriggerUnsetBlacklist[key] = TriggerUnsetBlacklist[key];
            successorBlock.IncomingVariableDefinitions = new Dictionary<(string, IRScope), SSADefinition>();
            foreach (var key in IncomingVariableDefinitions.Keys)
                successorBlock.IncomingVariableDefinitions[key] = IncomingVariableDefinitions[key];

            successorBlock.Continuation = Continuation;
            Continuation = new JumpContinuation(successorBlock, Continuation?.SourceLine ?? -1, Continuation?.SourceColumn ?? -1);

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
            if (!before.Successors.Contains(after))
                throw new ArgumentException("The 'before' block must have the 'after' block as a successor.");

            foreach (BasicBlock block in pattern)
                block.CodeComponent = before.CodeComponent;

            BasicBlock patternRoot = pattern.First();
            while (patternRoot.Dominator != null &&
                pattern.Contains(patternRoot.Dominator))
                patternRoot = patternRoot.Dominator;

            switch (before.Continuation)
            {
                case null:
                    before.Continuation = new JumpContinuation(patternRoot, -1, -1);
                    break;
                case JumpContinuation jump:
                    jump.Target = patternRoot;
                    break;
                case BranchContinuation branch:
                    if (branch.True == after)
                        branch.True = patternRoot;
                    if (branch.False == after)
                        branch.False = patternRoot;
                    break;
                case JumpStackContinuation _:
                    throw new NotImplementedException();
                default:
                    throw new NotImplementedException();
            }

            foreach (BasicBlock returnBlock in pattern.Where(b => b.PostDominator == null || b.PostDominator is SyntheticReturnBlock))
                ((JumpContinuation)returnBlock.Continuation).Target = after;

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

            foreach (BasicBlock footer in original.FooterBlocks)
            {
                if (replacementBlocks.TryGetValue(footer, out BasicBlock newFooter))
                    scope.FooterBlocks.Add(newFooter);
                else
                    scope.FooterBlocks.Add(footer);
            }

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

            block.Continuation = original.Continuation?.Clone(block);

            switch (block.Continuation)
            {
                case JumpContinuation jump:
                    if (replacements.TryGetValue(jump.Target, out BasicBlock newTarget))
                        jump.Target = newTarget;
                    break;
                case BranchContinuation branch:
                    if (replacements.TryGetValue(branch.True, out newTarget))
                        branch.True = newTarget;
                    if (replacements.TryGetValue(branch.False, out newTarget))
                        branch.False = newTarget;
                    break;
                case JumpStackContinuation jumpStack:
                    List<BasicBlock> destinations = jumpStack.Targets.ToList();
                    for (int i = 0; i < destinations.Count; i++)
                    {
                        if (replacements.TryGetValue(destinations[i], out newTarget))
                            destinations[i] = newTarget;
                    }
                    jumpStack.Targets = destinations;
                    break;
                case null:
                    break;
                default:
                    throw new NotImplementedException();

            }

            block.IsExecutable = original.IsExecutable;

            foreach ((string, IRScope) key in original.Phis.Keys)
                block.Phis[key] = original.Phis[key];

            foreach (IRInstruction instruction in original.Instructions)
            {
                IRInstruction newInstruction = instruction.Clone(block, false);
                block.Instructions.Add(newInstruction);
                if (instruction is IRAssign ||
                    instruction is IRUnset)
                    replacementInstructions[instruction] = newInstruction;
            }

            if (replacementScopes.TryGetValue(original.Scope, out IRScope newScope))
                block.Scope = newScope;
            else
                block.Scope = original.Scope;
        }
    }
}
