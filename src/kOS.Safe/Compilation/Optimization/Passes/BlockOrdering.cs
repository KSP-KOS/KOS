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

            for (int i = 0; i < results.Count - 1; i++)
            {
                BasicBlock block = results[i];
                if (block.Instructions.Count > 0 &&
                    block.Instructions[block.Instructions.Count - 1] is IRBranch branch &&
                    branch.True == results[i + 1])
                    branch.PreferFalse = true;
            }

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
                // If root == loopData.body it's because this was just called recursively
                // below. This can be treated as not a loop since it is already identified
                // as a loop body.
                if (IdentifyLoop(block, regionExits.Peek(), out LoopData loopData) && root != loopData.body)
                {
                    // Add the header to the current sequence, if necessary.
                    if (loopData.body != block)
                        sequence.Add(block);
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
                // If branchData.elseBlock == root, that's because this is the branch
                // instruction at the end of a loop body.
                else if (IdentifyBranch(block, regionExits.Peek(), out BranchData branchData) &&
                    branchData.elseBlock != root && branchData.ifBlock != root)
                {
                    // Add the branching block to the current sequence.
                    // The root is already included.
                    if (block != root)
                        sequence.Add(block);
                    // Add the 'if/then' block to the sequence.
                    // The branch instruction will skip ahead to the exit, so this ordering
                    // allows the 'if/then' block to fall through to the exit.
                    regionExits.Push(branchData.exit ?? regionExits.Peek());
                    sequence.Add(ConstructMetaSequence(branchData.ifBlock, regionExits, out Queue<BasicBlock> childOffshoots));
                    regionExits.Pop();
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
                else
                {
                    if (block != root)
                        sequence.Add(block);
                    // This condition occurs only in branches/loops, or with a single successor.
                    if (block.PostDominator?.Dominator == block)
                    {
                        // Set that successor as the next block for this sequence.
                        block = block.PostDominator;
                    }
                    else
                        return sequence;
                }
            }
            return sequence;
        }

        public static bool IdentifyLoop(BasicBlock block, BasicBlock regionExit, out LoopData loopData)
        {
            loopData = new LoopData();
            // --- Case 1: while/for loop ---
            // The header block has a conditional branch. One successor
            // is inside the loop (the body) and one is outside (the
            // exit). The latch has a back edge to this header.
            if (block.Successors.Count == 2 && HasBranchInstruction(block))
            {
                foreach (BasicBlock successor in block.Successors)
                {
                    // A successor is a latch if it (or the end of its linear chain)
                    // has a back edge to 'block', meaning 'block' dominates it.
                    BasicBlock latch = FindLatch(successor, block);
                    if (latch != null)
                    {
                        // The other successor is the exit.
                        BasicBlock bodyEntry = successor;
                        BasicBlock exit = block.Successors.First(s => s != successor);

                        // Sanity check: the exit should not be dominated by 'block'
                        // (it must be reachable without going through the loop body).
                        // Also verify exit is not inside the loop.
                        if (IsBackEdge(exit, bodyEntry))
                            continue; // both successors loop back — degenerate, skip

                        loopData = new LoopData(
                            header: block,
                            branchBlock: block,
                            body: bodyEntry,
                            exit: exit);
                        return true;
                    }
                }
            }

            // --- Case 2: do-while loop ---
            // The block is entered unconditionally (no branch at the header).
            // The branch is at the end of the body (the latch), and one of its
            // successors is a back edge to 'block' (making 'block' the body entry).
            //
            // We detect this by checking whether 'block' is the target of any back edge
            // from a block that 'block' dominates, and that back-edge block has a branch
            // to an exit outside the loop.
            if (block.Successors.Count == 1)
            {
                BasicBlock latch = FindDoWhileLatch(block, regionExit);
                if (latch != null && latch.Successors.Count == 2 && HasBranchInstruction(latch))
                {
                    // One successor of the latch loops back; the other is the exit.
                    BasicBlock exit = latch.Successors.FirstOrDefault(s => s != block);
                    if (exit != null && (exit == regionExit || IsInsideRegion(exit, regionExit)))
                    {
                        // Do-while: no header (body is entered unconditionally),
                        // branch is at the latch, body starts at 'block'.
                        loopData = new LoopData(
                            header: null,
                            branchBlock: latch,
                            body: block,
                            exit: exit);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Given a block that is a direct successor of the header, walks forward
        /// through linear (single-successor) chains to find a block that has a
        /// back edge to 'header'. Returns the latch block if found, null otherwise.
        /// </summary>
        private static BasicBlock FindLatch(BasicBlock block, BasicBlock header)
        {
            while (block != null)
            {
                // A back edge exists if 'header' dominates 'cursor' and
                // 'cursor' has 'header' as a successor.
                if (IsBackEdge(block, header) && block.Successors.Contains(header))
                    return block;

                // Follow the acyclical post-dominator chain to walk the loop
                // body without going into inner loops or branch bodies.
                if (block.PostDominator?.Dominator == block)
                    block = block.PostDominator;
                else
                    break;
            }
            return null;
        }

        /// <summary>
        /// For do-while detection: starting from the body entry block, walks the
        /// dominator subtree to find a block that has a back edge pointing back to
        /// 'bodyEntry' (i.e. a block dominated by bodyEntry that jumps back to it).
        /// </summary>
        private static BasicBlock FindDoWhileLatch(BasicBlock bodyEntry, BasicBlock regionExit)
        {
            // Walk all blocks dominated by bodyEntry (depth-first through dominates tree)
            // and look for one that has a successor equal to bodyEntry.
            Stack<BasicBlock> stack = new Stack<BasicBlock>(bodyEntry.Dominates);
            while (stack.Count > 0)
            {
                BasicBlock candidate = stack.Pop();

                // Don't cross out of the region.
                if (!IsInsideRegion(candidate, regionExit))
                    continue;

                if (candidate.Successors.Contains(bodyEntry))
                    return candidate;

                foreach (BasicBlock dominated in candidate.Dominates)
                    stack.Push(dominated);
            }
            return null;
        }

        /// <summary>
        /// Returns true if 'block' dominates 'target', meaning the edge
        /// from the predecessor of 'target' back to something dominated by
        /// 'block' constitutes a back edge.
        /// More precisely: is 'target' in the dominator subtree of 'ancestor'?
        /// </summary>
        private static bool IsBackEdge(BasicBlock block, BasicBlock ancestor)
        {
            while (block != null)
            {
                if (block == ancestor)
                    return true;
                block = block.Dominator;
            }
            return false;
        }

        private static bool HasBranchInstruction(BasicBlock block)
            => block.Instructions.Count > 0 &&
            block.Instructions[block.Instructions.Count - 1] is IRBranch;

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
        private static BasicBlock FindLocalMerge(BasicBlock ifBlock, BasicBlock regionExit)
        {
            BasicBlock candidate = ifBlock.PostDominator;
            while (candidate == regionExit || !IsInsideRegion(candidate, regionExit))
                candidate = candidate.PostDominator;
            // If we've walked all the way out, there is no local merge
            return candidate == regionExit ? null : candidate;
        }
        public static bool IsInsideRegion(BasicBlock candidate, BasicBlock regionExit)
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
            /// <summary>
            /// The branch's header block — the block where execution
            /// branches upon exiting.
            /// </summary>
            public readonly BasicBlock branch;
            /// <summary>
            /// The first block of the 'if' body (the block entered
            /// when the branch condition occurs.
            /// </summary>
            public readonly BasicBlock ifBlock;
            /// <summary>
            /// The first block of the 'else' body (the block entered
            /// when the branch condition does not occur. Null when
            /// this would be the exit block.
            /// </summary>
            public readonly BasicBlock elseBlock;
            /// <summary>
            /// The block following the branch — where execution goes when the branch resolves.
            /// </summary>
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
            /// <summary>
            /// The loop's header block — the block entered on each
            /// iteration from outside and jumped back to by the
            /// latch. Null for do-while style loops where the body is
            /// entered unconditionally and the branch is at the end.
            /// </summary>
            public readonly BasicBlock header;

            /// <summary>
            /// The block containing the conditional branch instruction
            /// that either continues the loop or exits it. For while
            /// loops this is the header; for do-while loops this is
            /// the latch (last block of the body).
            /// </summary>
            public readonly BasicBlock branchBlock;

            /// <summary>
            /// The first block of the loop body (the block entered when
            /// the loop entry condition is true / the loop continues).
            /// </summary>
            public readonly BasicBlock body;

            /// <summary>
            /// The block following the loop — where execution goes when the loop exits.
            /// </summary>
            public readonly BasicBlock exit;

            public LoopData(BasicBlock header, BasicBlock branchBlock, BasicBlock body, BasicBlock exit)
            {
                this.header = header;
                this.branchBlock = branchBlock;
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
