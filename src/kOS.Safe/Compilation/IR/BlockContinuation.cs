using System;
using System.Collections.Generic;
using System.Linq;

namespace kOS.Safe.Compilation.IR
{
    public abstract class BlockContinuation
    {
        public abstract bool IsInvariant { get; }
        public BasicBlock AssignedTo { get; internal set; }
        public short SourceLine { get; }
        public short SourceColumn { get; }
        public abstract IEnumerable<BasicBlock> Destinations { get; }

        protected BlockContinuation(short sourceLine, short sourceColumn)
        {
            SourceLine = sourceLine;
            SourceColumn = sourceColumn;
        }
        public event EventHandler<TargetChangedEvent> DestinationChanged;

        public abstract IEnumerable<Opcode> EmitOpcodes();
        public abstract BlockContinuation Clone(BasicBlock block, bool maintainSSAReferences = false);
        public abstract int OpcodeCount();

        protected void OnDestinationChanged(IEnumerable<BasicBlock> blocksAdded, IEnumerable<BasicBlock> blocksRemoved)
            => DestinationChanged?.Invoke(this, new TargetChangedEvent(blocksAdded, blocksRemoved));

        public class TargetChangedEvent : EventArgs
        {
            public IEnumerable<BasicBlock> BlocksAdded { get; }
            public IEnumerable<BasicBlock> BlocksRemoved { get; }
            public TargetChangedEvent(IEnumerable<BasicBlock> blocksAdded, IEnumerable<BasicBlock> blocksRemoved)
            {
                BlocksAdded = blocksAdded;
                BlocksRemoved = blocksRemoved;
            }
        }
    }

    public class JumpContinuation : BlockContinuation
    {
        private BasicBlock target;
        public override bool IsInvariant => true;
        public BasicBlock Target
        {
            get => target;
            set
            {
                if (value == target)
                    return;
                BasicBlock removed = target;
                target = value;
                OnDestinationChanged(new[] { target }, new[] { removed });
            }
        }
        public override IEnumerable<BasicBlock> Destinations
        {
            get
            {
                yield return Target;
            }
        }

        public JumpContinuation(BasicBlock target, short sourceLine, short sourceColumn) : base(sourceLine, sourceColumn)
            => this.target = target;

        public override BlockContinuation Clone(BasicBlock block, bool maintainSSAReferences = false)
            => new JumpContinuation(Target, SourceLine, SourceColumn);

        public override int OpcodeCount()
            => 1;

        public override IEnumerable<Opcode> EmitOpcodes()
        {
            if (Target is SyntheticReturnBlock)
                yield break;
            yield return new OpcodeBranchJump() { DestinationLabel = Target.Label, SourceLine = SourceLine, SourceColumn = SourceColumn };
        }

        public override bool Equals(object obj)
            => obj is JumpContinuation jump &&
                Target == jump.Target;
        public override int GetHashCode()
            => Target.GetHashCode();
        public override string ToString()
            => string.Format("{{jump {0}}}", Target.Label);
    }

    public class BranchContinuation : BlockContinuation, ISingleOperandInstruction
    {
        private BasicBlock onTrue, onFalse;
        public override bool IsInvariant => Condition.IsInvariant;
        public IInterimOperand Condition { get; set; }
        public BasicBlock True
        {
            get => onTrue;
            set
            {
                if (value == onTrue)
                    return;
                BasicBlock removed = onTrue;
                onTrue = value;
                if (onFalse == removed)
                    OnDestinationChanged(new[] { onTrue }, Enumerable.Empty<BasicBlock>());
                else
                    OnDestinationChanged(new[] { onTrue }, new[] { removed });
            }
        }
        public BasicBlock False
        {
            get => onFalse;
            set
            {
                if (value == onFalse)
                    return;
                BasicBlock removed = onFalse;
                onFalse = value;
                if (onTrue == removed)
                    OnDestinationChanged(new[] { onFalse }, Enumerable.Empty<BasicBlock>());
                else
                    OnDestinationChanged(new[] { onFalse }, new[] { removed });
            }
        }
        public override IEnumerable<BasicBlock> Destinations
        {
            get
            {
                yield return onTrue;
                yield return onFalse;
            }
        }
        public bool PreferFalse { get; set; } = false;
        IInterimOperand ISingleOperandInstruction.Operand
        {
            get => Condition;
            set => Condition = value;
        }

        public BranchContinuation(IInterimOperand condition, BasicBlock onTrue, BasicBlock onFalse, BranchOpcode opcodeBranch) :
            this(condition, onTrue, onFalse, opcodeBranch.SourceLine, opcodeBranch.SourceColumn, opcodeBranch is OpcodeBranchIfFalse) { }

        public BranchContinuation(IInterimOperand condition, BasicBlock onTrue, BasicBlock onFalse, short sourceLine, short sourceColumn, bool preferFalse = false) : base(sourceLine, sourceColumn)
        {
            Condition = condition;
            this.onTrue = onTrue;
            this.onFalse = onFalse;
            PreferFalse = preferFalse;
        }

        public override int OpcodeCount()
        {
            int length = 0;
            if (Condition is IOperandInstructionBase operandInstruction)
            {
                operandInstruction.ForEachOperand(op =>
                {
                    if (op is IResultingInstruction resultingInstruction)
                        length += resultingInstruction.OpcodeCount;
                    else
                        length += 1;

                });
            }
            return length + 1;
        }

        public override IEnumerable<Opcode> EmitOpcodes()
        {
            foreach (Opcode opcode in Condition.EmitOpcodes())
                yield return opcode;
            if (PreferFalse)
            {
                yield return new OpcodeBranchIfFalse() { DestinationLabel = False.Label, SourceLine = SourceLine, SourceColumn = SourceColumn };
                yield return new OpcodeBranchJump() { DestinationLabel = True.Label, SourceLine = SourceLine, SourceColumn = SourceColumn };
            }
            else
            {
                yield return new OpcodeBranchIfTrue() { DestinationLabel = True.Label, SourceLine = SourceLine, SourceColumn = SourceColumn };
                yield return new OpcodeBranchJump() { DestinationLabel = False.Label, SourceLine = SourceLine, SourceColumn = SourceColumn };
            }
        }

        public override BlockContinuation Clone(BasicBlock block, bool maintainSSAReferences = false)
            => new BranchContinuation(Condition.Clone(block, maintainSSAReferences), True, False, SourceLine, SourceColumn, PreferFalse);

        public override bool Equals(object obj)
            => obj is BranchContinuation branch &&
                Condition.Equals(branch.Condition) &&
                True == branch.True &&
                False == branch.False;
        public override int GetHashCode()
            => True.GetHashCode() ^ False.GetHashCode();
        public override string ToString()
            => string.Format("{{br.?{2} {0}/{1}}}", True.Label, False.Label, PreferFalse ? "f" : "t");

        public void ForEachOperand(Action<IInterimOperand> action)
            => action(Condition);
        public void MutateEachOperand(Func<IInterimOperand, IInterimOperand> mutateFunc)
            => Condition = mutateFunc(Condition);
        public bool AnyOperand(Func<IInterimOperand, bool> predicate)
            => predicate(Condition);
        public bool AllOperands(Func<IInterimOperand, bool> predicate)
            => predicate(Condition);
    }

    public class JumpStackContinuation : BlockContinuation, ISingleOperandInstruction
    {
        private List<BasicBlock> targets;
        public override bool IsInvariant => Distance.IsInvariant;
        public IInterimOperand Distance { get; set; }
        public IReadOnlyCollection<BasicBlock> Targets
        {
            get => targets;
            set
            {
                if (targets.SequenceEqual(value))
                    return;
                IEnumerable<BasicBlock> removed = targets;
                targets = new List<BasicBlock>(value);
                OnDestinationChanged(targets, removed);
            }
        }
        public override IEnumerable<BasicBlock> Destinations => Targets;
        IInterimOperand ISingleOperandInstruction.Operand
        {
            get => Distance;
            set => Distance = value;
        }

        public JumpStackContinuation(IInterimOperand distance, IEnumerable<BasicBlock> targets, short sourceLine, short sourceColumn) : base(sourceLine, sourceColumn)
        {
            Distance = distance;
            this.targets = new List<BasicBlock>(targets);
        }

        public override int OpcodeCount()
        {
            int length = 0;
            if (Distance is IOperandInstructionBase operandInstruction)
            {
                operandInstruction.ForEachOperand(op =>
                {
                    if (op is IResultingInstruction resultingInstruction)
                        length += resultingInstruction.OpcodeCount;
                    else
                        length += 1;

                });
            }
            return length + 1;
        }

        public override BlockContinuation Clone(BasicBlock block, bool maintainSSAReferences = false)
            => new JumpStackContinuation(Distance, Targets, SourceLine, SourceColumn);
        
        public override IEnumerable<Opcode> EmitOpcodes()
        {
            foreach (Opcode opcode in Distance.EmitOpcodes())
                yield return opcode;
            yield return new OpcodeJumpStack() { SourceLine = SourceLine, SourceColumn = SourceColumn };
        }
        public void ForEachOperand(Action<IInterimOperand> action)
            => action(Distance);
        public void MutateEachOperand(Func<IInterimOperand, IInterimOperand> mutateFunc)
            => Distance = mutateFunc(Distance);
        public bool AnyOperand(Func<IInterimOperand, bool> predicate)
            => predicate(Distance);
        public bool AllOperands(Func<IInterimOperand, bool> predicate)
            => predicate(Distance);

        public override bool Equals(object obj)
            => obj is JumpStackContinuation jumpStack &&
                Distance == jumpStack.Distance &&
                Targets.SequenceEqual(jumpStack.Targets);
        public override int GetHashCode()
            => Targets.GetHashCode();
    }
}
