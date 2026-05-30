using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Compilation.IR;

namespace kOS.Safe.Compilation.Optimization.Passes
{
    public class BlockOrdering : IHolisticOptimizationPass, ILinkedOptimizationPass
    {
        public OptimizationLevel OptimizationLevel => OptimizationLevel.None;
        public short SortIndex => short.MaxValue;
        public Optimizer Optimizer { get; set; }

        // These predicates can be used to filter basic blocks to
        // all blocks or just those that are flagged as IsExecutable.
        public static bool AllBlocksPredicate(BasicBlock _)
            => true;
        public static bool ExecutableBlocksPredicate(BasicBlock block)
            => block.IsExecutable;

        // Optimally arrange blocks, while eliminating non-executable blocks
        // (if the optimization level is Balanced or above).
        public void ApplyPass(IRCodePart codePart)
        {
            if (Optimizer.OptimizationLevel >= OptimizationLevel.Minimal)
                ApplyOrdering(codePart, ExecutableBlocksPredicate);
            else
                ApplyOrdering(codePart, AllBlocksPredicate);
        }
            

        // Optimally arrange blocks assuming that all are executable.
        // This method is publicly available outside the optimization pipeline.
        public static void ApplyInitialOrdering(IRCodePart codePart)
            => ApplyOrdering(codePart, AllBlocksPredicate);

        /// <summary>
        /// Applies the block ordering by overwriting the block list of each code part unit.
        /// </summary>
        /// <param name="codePart">The code part.</param>
        /// <param name="inclusionPredicate">The inclusion predicate (all or executable blocks).</param>
        private static void ApplyOrdering(IRCodePart codePart, Func<BasicBlock, bool> inclusionPredicate)
        {
            foreach (IRCodePart.IRFunction function in codePart.Functions)
                foreach (IRCodePart.IRFunction.IRFunctionFragment fragment in function.Fragments)
                    fragment.FunctionCode = ApplyOrdering(fragment.FunctionCode[0], inclusionPredicate);
            foreach (IRCodePart.IRTrigger trigger in codePart.Triggers)
                trigger.Code = ApplyOrdering(trigger.Code[0], inclusionPredicate);
            codePart.MainCode = ApplyOrdering(codePart.MainCode[0], inclusionPredicate);
        }

        /// <summary>
        /// Applies the ordering within a code part unit.
        /// </summary>
        /// <param name="root">The root block.</param>
        /// <param name="inclusionPredicate">The inclusion predicate (all or executable).</param>
        /// <returns>The ordered list of blocks.</returns>
        /// <exception cref="Exceptions.KOSYouShouldNeverSeeThisException">Root block isn't executable!</exception>
        public static List<BasicBlock> ApplyOrdering(BasicBlock root, Func<BasicBlock, bool> inclusionPredicate)
        {
            // Local function to get the edges to subsequent blocks.
            IEnumerable<BasicBlock> GetEdges(BasicBlock block)
                => block.Successors.Where(inclusionPredicate);

            // If the root itself isn't executable, throw an exception because this will
            // probably break labels somewhere.
            if (!inclusionPredicate(root))
                throw new Exceptions.KOSYouShouldNeverSeeThisException("Root block isn't executable!");

            // Find the last executable block and push it onto the stack
            // of region exit blocks.
            List<BasicBlock> reversePostOrder = BasicBlock.GetReversePostOrder(root, GetEdges);
            Stack<BasicBlock> regionExits = new Stack<BasicBlock>();
            regionExits.Push(reversePostOrder[reversePostOrder.Count - 1]);

            // MetaBlockSequences are sequences of sequences of blocks.
            // The first item will be the primary execution path.
            // Subsequent items will be orphaned paths, like the 'else' body of branching statements.
            List<MetaBlockSequence> metaSequences = new List<MetaBlockSequence>();

            // This queue is used to process the root blocks of execution paths.
            // It starts with the entry block (root) and will add orphaned paths root blocks.
            Queue<BasicBlock> sequenceStarts = new Queue<BasicBlock>();
            sequenceStarts.Enqueue(root);

            while (sequenceStarts.Count > 0)
            {
                MetaBlockSequence sequence = ConstructMetaSequence(sequenceStarts.Dequeue(), regionExits, out Queue<BasicBlock> newStarts);
                foreach (BasicBlock start in newStarts)
                    sequenceStarts.Enqueue(start);
                metaSequences.Add(sequence);
            }

            // Create a unified sequence of BasicBlocks.
            List<BasicBlock> results = metaSequences.SelectMany(s => s.Blocks).ToList();

            // This block is to avoid leaving branch instructions to blocks
            // that aren't actually being emitted, since that will throw
            // an exeption during block labelling.
            // The Constant Folding pass does this more thoroughly, but
            // if it is skipped, this becomes necessary.
            if (Optimizer.PassesToSkip.Contains(typeof(ConstantFolding)))
            {
                foreach (BasicBlock b in results)
                {
                    List<IRInstruction> instructions = b.Instructions;
                    int branchIdx = instructions.Count - 1;
                    if (branchIdx >= 0 && instructions[branchIdx] is IRBranch branch)
                    {
                        if (!inclusionPredicate(branch.True))
                            instructions[branchIdx] = new IRJump(branch.Block, branch.False, branch.SourceLine, branch.SourceColumn);
                        else if (!inclusionPredicate(branch.False))
                            instructions[branchIdx] = new IRJump(branch.Block, branch.True, branch.SourceLine, branch.SourceColumn);
                    }
                }
            }

            return results;
        }

        private static MetaBlockSequence ConstructMetaSequence(BasicBlock root, Stack<BasicBlock> regionExits, out Queue<BasicBlock> newOffshoots)
        {
            // Create a new sequence with just the root block.
            MetaBlockSequence sequence = new MetaBlockSequence(root);
            BasicBlock block = root;
            newOffshoots = new Queue<BasicBlock>();
            // If the block is the next region exit (or null),
            // that is the end of the current sequence.
            // Note that region exits are part of the enclosing region's sequence.
            while (block != null && block != regionExits.Peek())
            {
                // Identify loops first because loop branches are subsets of branches.
                if (IdentifyLoop(block, regionExits.Peek(), out LoopData loopData))
                {
                    // Push the next region exit.
                    regionExits.Push(loopData.exit);
                    // Add the sequence from the loop's body.
                    sequence.Add(ConstructMetaSequence(loopData.body, regionExits, out Queue<BasicBlock> childOffshoots));
                    // That region is now popped.
                    regionExits.Pop();
                    // Enqueue any new offshoots.
                    foreach (BasicBlock child in childOffshoots)
                        newOffshoots.Enqueue(child);
                    // Set the exit block as the next block for this sequence.
                    block = loopData.exit;
                }
                else if (IdentifyBranch(block, regionExits.Peek(), out BranchData branchData) &&
                    // These checks are for edge cases of loop structures that are neither loops or branches.
                    !((branchData.ifBlock?.Dominator != null && branchData.ifBlock.Dominator != block) ||
                    (branchData.elseBlock?.Dominator != null && branchData.elseBlock.Dominator != block)))
                {
                    // Add the 'if/then' block to the sequence.
                    // The branch instruction will skip ahead to the exit, so this ordering
                    // allows the 'if/then' block to fall through to the exit.
                    sequence.Add(ConstructMetaSequence(branchData.ifBlock, regionExits, out Queue<BasicBlock> childOffshoots));
                    // Enqueue any new offshoots.
                    foreach (BasicBlock child in childOffshoots)
                        newOffshoots.Enqueue(child);
                    // If the exit block is null, that's probably because the
                    // 'else' block was categorized as the exit.
                    if (branchData.exit == null && branchData.elseBlock != null)
                    {
                        // Set the 'exit' block as the next block for this sequence.
                        block = branchData.elseBlock;
                    }
                    else
                    {
                        // The 'else' block, if present, becomes a new offshoot.
                        if (branchData.elseBlock != null)
                            newOffshoots.Enqueue(branchData.elseBlock);
                        // Set the 'exit' block as the next block for this sequence.
                        block = branchData.exit;
                    }
                }
                // This condition occurs only in branches/loops, or with a single successor.
                else if (block.PostDominator?.Dominator == block)
                {
                    // Set that successor as the next block for this sequence.
                    block = block.PostDominator;
                }
                else
                    return sequence;
                // Add the next block to the sequence.
                if (block != null)
                    sequence.Add(block);
            }
            return sequence;
        }

        public static bool IdentifyLoop(BasicBlock headerBlock, BasicBlock regionExit, out LoopData loopData)
        {
            loopData = new LoopData();
            // This just checks that there is a branch instruction and classifies the two branches for the loop.
            // It should really not call IdentifyBranch, but the code reuse was too tempting.
            if (!IdentifyBranch(headerBlock, regionExit, out BranchData branchData))
                return false;

            if (headerBlock.Successors.Count != 2)
                return false;

            loopData = new LoopData(headerBlock, branchData.ifBlock, branchData.exit ?? branchData.elseBlock);
            // At least one of the branches must return to the header
            // block or the top of that branch to be a loop.
            foreach (BasicBlock successor in headerBlock.Successors)
            {
                if (BackEdgeDetection(successor, headerBlock))
                    return true;
            }
            loopData = new LoopData();
            return false;
        }
        public static bool IdentifyBranch(BasicBlock branchingBlock, BasicBlock regionExit, out BranchData branchData)
        {
            branchData = new BranchData();

            // Get the branch instruction (or return false).
            if (branchingBlock.Instructions.Count == 0)
                return false;
            if (!(branchingBlock.Instructions[branchingBlock.Instructions.Count - 1] is IRBranch branch))
                return false;

            // Use the context cues for which branch is which.
            // This is critical for loops.
            BasicBlock ifBlock, elseBlock, rejoinsAt;
            if (!branch.PreferFalse)
            {
                ifBlock = branch.False;
                elseBlock = branch.True;
            }
            else
            {
                ifBlock = branch.True;
                elseBlock = branch.False;
            }

            // Find the next post-dominator that is in the region.
            // This accounts for branch bodies that break from a loop
            // or return from a function.
            rejoinsAt = FindLocalMerge(branchingBlock, regionExit);

            // Use actual control flow to correct assumptions about
            // which block is which.
            if (elseBlock == rejoinsAt || GetSequenceEnd(elseBlock) == rejoinsAt)
                elseBlock = null;
            else if (ifBlock == rejoinsAt || GetSequenceEnd(ifBlock) == rejoinsAt)
            {
                ifBlock = elseBlock;
                elseBlock = null;
                branch.PreferFalse = !branch.PreferFalse;
            }
            branchData = new BranchData(branchingBlock, ifBlock, elseBlock, rejoinsAt);
            return true;
        }
        private static BasicBlock GetSequenceEnd(BasicBlock block)
        {
            while (block.PostDominator?.Dominator == block)
                block = block.PostDominator;
            return block;
        }
        private static bool BackEdgeDetection(BasicBlock successor, BasicBlock target)
        {
            BasicBlock firstSuccessor = successor;
            if (successor.Dominator != target)
                return false;

            while (successor.Successors.Count == 1)
            {
                successor = successor.Successors.First();
                if (successor == target)
                    return true;
            }

            return successor.Successors.Contains(target) || successor.Successors.Contains(firstSuccessor);
        }
        private static BasicBlock FindLocalMerge(BasicBlock ifBlock, BasicBlock regionExit)
        {
            BasicBlock candidate = ifBlock.PostDominator;
            while (candidate == regionExit || !IsInsideRegion(candidate, regionExit))
                candidate = candidate.PostDominator;
            // If we've walked all the way out, there is no local merge
            return candidate == regionExit ? null : candidate;
        }
        private static bool IsInsideRegion(BasicBlock candidate, BasicBlock regionExit)
        {
            // Walk the post-dominator chain of regionExit upward.
            // If we encounter candidate, it is outside or on the boundary.
            BasicBlock block = regionExit;
            while (block != null)
            {
                if (block == candidate)
                    return false;
                block = block.PostDominator;
            }
            return true;
        }

        public readonly struct BranchData
        {
            public readonly BasicBlock branch;
            public readonly BasicBlock ifBlock;
            public readonly BasicBlock elseBlock;
            public readonly BasicBlock exit;
            public BranchData(BasicBlock branch, BasicBlock ifBlock, BasicBlock elseBlock, BasicBlock exit)
            {
                this.branch = branch;
                this.ifBlock = ifBlock;
                this.elseBlock = elseBlock;
                this.exit = exit;
            }
        }
        public readonly struct LoopData
        {
            public readonly BasicBlock header;
            public readonly BasicBlock body;
            public readonly BasicBlock exit;
            public LoopData(BasicBlock header, BasicBlock body, BasicBlock exit)
            {
                this.header = header;
                this.body = body;
                this.exit = exit;
            }
        }

        public abstract class BlockSequence
        {
            public abstract IReadOnlyList<BasicBlock> Blocks { get; }
            public BasicBlock First { get; }
            public BasicBlock Last { get; protected set; }
            public IReadOnlyCollection<BasicBlock> Predecessors => First.Predecessors;
            public IReadOnlyCollection<BasicBlock> Successors => Last.Successors;
            public virtual bool Contains(BasicBlock block)
                => Blocks.Contains(block);
            public abstract void Add(BasicBlock block);
            protected BlockSequence(BasicBlock first)
            {
                First = first;
            }
            public static explicit operator BlockSequence(BasicBlock block)
                => new BasicBlockSequence(block);
        }
        public class BasicBlockSequence : BlockSequence
        {
            private readonly List<BasicBlock> blocks;
            public override IReadOnlyList<BasicBlock> Blocks => blocks;

            public BasicBlockSequence(IEnumerable<BasicBlock> blocks) : base(blocks.First())
            {
                this.blocks = blocks.ToList();
                Last = Blocks[Blocks.Count - 1];
            }
            public BasicBlockSequence(BasicBlock block) : base(block)
            {
                blocks = new List<BasicBlock>() { block };
                Last = block;
            }
            public override void Add(BasicBlock block)
            {
                blocks.Add(block);
                Last = block;
            }
        }
        public class MetaBlockSequence : BlockSequence
        {
            private readonly List<BlockSequence> blocks;
            public override IReadOnlyList<BasicBlock> Blocks => blocks.SelectMany(b => b.Blocks).ToList();
            public MetaBlockSequence(IEnumerable<BlockSequence> blocks) : base(blocks.First().First)
            {
                this.blocks = blocks.ToList();
                Last = Blocks[Blocks.Count - 1];
            }
            public MetaBlockSequence(BasicBlock block) : base(block)
            {
                blocks = new List<BlockSequence>() { new BasicBlockSequence(block) };
                Last = block;
            }
            public MetaBlockSequence(BlockSequence block) : base(block.First)
            {
                blocks = new List<BlockSequence>() { block };
                Last = block.Last;
            }
            public override void Add(BasicBlock block)
            {
                blocks[blocks.Count - 1].Add(block);
                Last = block;
            }
            public void Add(BlockSequence block)
            {
                blocks.Add(block);
                Last = block.Last;
            }
            public override bool Contains(BasicBlock block)
                => blocks.Any(b => b.Contains(block));
        }
    }
}
