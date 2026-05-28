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

        public static bool InitialPredicate(BasicBlock _)
            => true;
        public static bool IsExecutablePredicate(BasicBlock block)
            => block.IsExecutable;

        // Optimally arrange blocks, while eliminating non-executable blocks.
        public void ApplyPass(IRCodePart codePart)
        {
            if (Optimizer.OptimizationLevel >= OptimizationLevel.Minimal)
                ApplyOrdering(codePart, IsExecutablePredicate);
            else
                ApplyOrdering(codePart, InitialPredicate);
        }
            

        // Optimally arrange blocks assuming that all are executable
        public static void ApplyInitialOrdering(IRCodePart codePart)
            => ApplyOrdering(codePart, InitialPredicate);

        private static void ApplyOrdering(IRCodePart codePart, Func<BasicBlock, bool> inclusionPredicate)
        {
            foreach (IRCodePart.IRFunction function in codePart.Functions)
                foreach (IRCodePart.IRFunction.IRFunctionFragment fragment in function.Fragments)
                    fragment.FunctionCode = ApplyOrdering(fragment.FunctionCode[0], inclusionPredicate);
            foreach (IRCodePart.IRTrigger trigger in codePart.Triggers)
                trigger.Code = ApplyOrdering(trigger.Code[0], inclusionPredicate);
            codePart.MainCode = ApplyOrdering(codePart.MainCode[0], inclusionPredicate);
        }

        public static List<BasicBlock> ApplyOrdering(BasicBlock root, Func<BasicBlock, bool> inclusionPredicate)
        {
            IEnumerable<BasicBlock> GetEdges(BasicBlock block)
                => block.Successors.Where(inclusionPredicate);

            // If the root itself isn't executable, throw an exception because this will
            // probably break labels somewhere.
            if (!inclusionPredicate(root))
                throw new Exceptions.KOSYouShouldNeverSeeThisException("Root block isn't executable!");

            List<BasicBlock> reversePostOrder = BasicBlock.GetReversePostOrder(root, GetEdges);
            Stack<BasicBlock> regionExits = new Stack<BasicBlock>();
            regionExits.Push(reversePostOrder[reversePostOrder.Count - 1]);

            List<MetaBlockSequence> metaSequences = new List<MetaBlockSequence>();

            Queue<BasicBlock> sequenceStarts = new Queue<BasicBlock>();
            sequenceStarts.Enqueue(root);

            while (sequenceStarts.Count > 0)
            {
                MetaBlockSequence sequence = ConstructMetaSequence(sequenceStarts.Dequeue(), regionExits, out Queue<BasicBlock> newStarts);
                foreach (BasicBlock start in newStarts)
                    sequenceStarts.Enqueue(start);
                metaSequences.Add(sequence);
            }

            List<BasicBlock> results = metaSequences.SelectMany(s => s.Blocks).ToList();

            if (Optimizer.PassesToSkip.Contains(typeof(ConstantFolding)))
            {
                foreach (BasicBlock b in results)
                {
                    List<IRInstruction> instructions = b.Instructions;
                    int branchIdx = instructions.Count - 1;
                    if (branchIdx >= 0 && instructions[branchIdx] is IRBranch branch)
                    {
                        if (!branch.True.IsExecutable)
                            instructions[branchIdx] = new IRJump(branch.Block, branch.False, branch.SourceLine, branch.SourceColumn);
                        else if (!branch.False.IsExecutable)
                            instructions[branchIdx] = new IRJump(branch.Block, branch.True, branch.SourceLine, branch.SourceColumn);
                    }
                }
            }

            return results;
        }

        private static MetaBlockSequence ConstructMetaSequence(BasicBlock root, Stack<BasicBlock> regionExits, out Queue<BasicBlock> newOffshoots)
        {
            MetaBlockSequence sequence = new MetaBlockSequence(root);
            BasicBlock block = root;
            newOffshoots = new Queue<BasicBlock>();
            while (block != null && block != regionExits.Peek())
            {
                if (IdentifyLoop(block, regionExits.Peek(), out LoopData loopData))
                {
                    regionExits.Push(loopData.exit);
                    sequence.Add(ConstructMetaSequence(loopData.body, regionExits, out Queue<BasicBlock> childOffshoots));
                    regionExits.Pop();
                    foreach (BasicBlock child in childOffshoots)
                        newOffshoots.Enqueue(child);
                    block = loopData.exit;
                }
                else if (IdentifyBranch(block, regionExits.Peek(), out BranchData branchData))
                {
                    sequence.Add(ConstructMetaSequence(branchData.ifBlock, regionExits, out Queue<BasicBlock> childOffshoots));
                    foreach (BasicBlock child in childOffshoots)
                        newOffshoots.Enqueue(child);
                    if (branchData.exit == null && branchData.elseBlock != null)
                    {
                        block = branchData.elseBlock;
                    }
                    else
                    {
                        if (branchData.elseBlock != null)
                            newOffshoots.Enqueue(branchData.elseBlock);
                        block = branchData.exit;
                    }
                }
                else if (block.PostDominator?.Dominator == block)
                {
                    block = block.PostDominator;
                }
                else
                    return sequence;
                if (block != null)
                    sequence.Add(block);
            }
            return sequence;
        }

        public static bool IdentifyLoop(BasicBlock headerBlock, BasicBlock regionExit, out LoopData loopData)
        {
            loopData = new LoopData();
            if (!IdentifyBranch(headerBlock, regionExit, out BranchData branchData))
                return false;

            if (headerBlock.Successors.Count != 2)
                return false;

            loopData = new LoopData(headerBlock, branchData.ifBlock, branchData.exit ?? branchData.elseBlock);
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

            if (branchingBlock.Instructions.Count == 0)
                return false;
            if (!(branchingBlock.Instructions[branchingBlock.Instructions.Count - 1] is IRBranch branch))
                return false;

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

            rejoinsAt = FindLocalMerge(branchingBlock, regionExit);
            //rejoinsAt = _branchingBlock.PostDominator;
            //if (rejoinsAt == null || rejoinsAt is SyntheticReturnBlock)
                //return false;

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
            if (successor.Dominator != target)
                return false;

            while (successor.Successors.Count == 1)
            {
                successor = successor.Successors.First();
                if (successor == target)
                    return true;
            }

            return successor.Successors.Contains(target);
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
