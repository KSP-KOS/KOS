using System;
using System.Collections.Generic;
using System.Linq;

namespace kOS.Safe.Compilation.IR
{
    public abstract class IRValue
    {
        public virtual Type ValueType { get; set; } = typeof(Encapsulation.Structure);
        public abstract bool IsInvariant { get; set; }
        internal abstract IEnumerable<Opcode> EmitPush();
    }

    public class IRConstant : IRValue
    {
        protected readonly short sourceLine, sourceColumn;

        public override bool IsInvariant
        {
            get => true;
            set
            {
                if (value)
                    return;
                throw new InvalidOperationException("Cannot set the invariant state of a constant to false.");
            }
        }
        public object Value { get; }
        public IRConstant(object value, Opcode opcode) : this(value, opcode.SourceLine, opcode.SourceColumn) { }
        public IRConstant(object value, IRInstruction instruction) : this(value, instruction.SourceLine, instruction.SourceColumn) { }
        public IRConstant(object value, short sourceLine, short sourceColumn)
        {
            Value = value;
            this.sourceLine = sourceLine;
            this.sourceColumn = sourceColumn;
            ValueType = value.GetType();
        }
        internal override IEnumerable<Opcode> EmitPush()
        {
            yield return new OpcodePush(Value)
            {
                SourceLine = sourceLine,
                SourceColumn = sourceColumn
            };
        }
        public override bool Equals(object obj)
            => obj is IRConstant constant && Value.Equals(constant.Value) || Value.Equals(obj);
        public override int GetHashCode()
            => Value.GetHashCode();
        public override string ToString()
            => Value.ToString();
    }
    public abstract class IRVariableBase : IRValue
    {
        public string Name { get; }
        public virtual IRScope Scope { get; protected set; }
        public IRVariableBase(string name, IRScope declaringScope)
        {
            Name = name;
            Scope = declaringScope;
        }

        public void RedefineScope(IRScope newScope)
            => Scope = newScope;

        public override string ToString()
            => $"{Name} {Scope.IndexString()}";
        protected bool NameAndScopeEquals(IRVariableBase variable)
            => string.Equals(Name, variable.Name, StringComparison.OrdinalIgnoreCase) &&
                (Scope.IsEqualOrEncompassedBy(variable.Scope) ||
                variable.Scope.IsEncompassedBy(Scope));
        protected int GetBaseHashCode()
            => Name.ToLower().GetHashCode();
    }
    public class IRVariable : IRVariableBase
    {
        private ushort nextSSAIndex = 0;
        private readonly Dictionary<ushort, SSAVariable> iterations = new Dictionary<ushort, SSAVariable>();
        internal readonly short sourceLine, sourceColumn;

        public override bool IsInvariant { get; set; } = false;
        public bool IsLock { get; }
        public IReadOnlyDictionary<ushort, SSAVariable> Iterations => iterations;

        public IRVariable(OpcodeIdentifierBase opcode, IRScope scope, bool isLock = false) :
            this(opcode.Identifier, scope, opcode, isLock) { }
        public IRVariable(string name, IRScope scope, Opcode opcode, bool isLock = false) :
            this(name, scope, opcode.SourceLine, opcode.SourceColumn, isLock) { }
        public IRVariable(string name, IRScope scope, short sourceLine, short sourceColumn, bool isLock = false) :
            base(name, scope)
        {
            IsLock = isLock;
            this.sourceLine = sourceLine;
            this.sourceColumn = sourceColumn;
        }

        public virtual SSAVariable GetNewSSAVariable()
        {
            SSAVariable nextIteration = new SSAVariable(this, nextSSAIndex);
            iterations[nextSSAIndex] = nextIteration;
            nextSSAIndex += 1;
            return nextIteration;
        }

        public virtual PhiVariable GetNewPhiVariable()
        {
            PhiVariable phiVariable = new PhiVariable(this, nextSSAIndex);
            iterations[nextSSAIndex] = phiVariable;
            nextSSAIndex += 1;
            return phiVariable;
        }

        internal override IEnumerable<Opcode> EmitPush()
        {
            yield return new OpcodePush(Name)
            {
                SourceLine = sourceLine,
                SourceColumn = sourceColumn
            };
        }
        public override bool Equals(object obj)
            => !(obj is SSAVariable) &&
            obj is IRVariable variable &&
            NameAndScopeEquals(variable);
        public override int GetHashCode()
            => GetBaseHashCode();
    }
    public class SSAVariable : IRVariable
    {
        private readonly ushort ssaIndex;

        public override bool IsInvariant
        {
            get => AssignedAt.IsInvariant;
            set => throw new InvalidOperationException($"Cannot set the invariant state of an {nameof(SSAVariable)}.");
        }
        public IRVariable Parent { get; }
        public IRAssign AssignedAt { get; set; }
        internal SSAVariable(IRVariable baseVariable, ushort ssaIndex) :
            base(baseVariable.Name, baseVariable.Scope, baseVariable.sourceLine, baseVariable.sourceColumn)
        {
            this.ssaIndex = ssaIndex;
            Parent = baseVariable;
            ValueType = baseVariable.ValueType;
        }

        public override SSAVariable GetNewSSAVariable()
            => Parent.GetNewSSAVariable();
        public override PhiVariable GetNewPhiVariable()
            => Parent.GetNewPhiVariable();

        public override string ToString()
        {
            string baseString = base.ToString();
            return baseString.Insert(baseString.IndexOf(" "), $".{ssaIndex}");
        }
        public override bool Equals(object obj)
            => obj is SSAVariable ssaVariable &&
            ssaIndex == ssaVariable.ssaIndex &&
            NameAndScopeEquals(ssaVariable);
        public override int GetHashCode()
            => (ssaIndex, GetBaseHashCode()).GetHashCode();
    }

    public class PhiVariable : SSAVariable, IMultipleOperandInstruction
    {
        public Dictionary<BasicBlock, SSAVariable> PossibleValues { get; } = new Dictionary<BasicBlock, SSAVariable>();
        public override bool IsInvariant { get => !PossibleValues.Skip(1).Any() && PossibleValues.Values.First().IsInvariant; set => throw new InvalidOperationException(); }

        IEnumerable<IRValue> IMultipleOperandInstruction.Operands => PossibleValues.Values;

        int IMultipleOperandInstruction.OperandCount => PossibleValues.Count;

        internal PhiVariable(IRVariable baseVariable, ushort ssaIndex) : base(baseVariable, ssaIndex) { }

        public void RefreshType()
        {
            Type proposedType = PossibleValues.Values.First().ValueType;

            foreach (SSAVariable variable in PossibleValues.Values.Skip(1))
                proposedType = GetFirstCommonBaseType(proposedType, variable.ValueType);

            ValueType = proposedType;
        }

        public static Type GetFirstCommonBaseType(Type typeA, Type typeB)
        {
            if (typeA == null || typeB == null) return null;

            Type current = typeA;
            while (current != null)
            {
                if (current.IsAssignableFrom(typeB))
                {
                    return current;
                }
                current = current.BaseType;
            }

            throw new Exceptions.KOSYouShouldNeverSeeThisException($"Couldn't find a base class between {typeA} and {typeB}, when all kOS types should derive from {nameof(Encapsulation.Structure)}.");
        }

        void IOperandInstructionBase.ForEachOperand(Action<IRValue> action)
        {
            foreach (SSAVariable variable in PossibleValues.Values)
                action(variable);
        }

        void IOperandInstructionBase.MutateEachOperand(Func<IRValue, IRValue> mutateFunc)
        {
            foreach (BasicBlock block in PossibleValues.Keys.ToArray())
                PossibleValues[block] = (SSAVariable)mutateFunc(PossibleValues[block]);
        }
    }

    public class IRRelocateLater : IRConstant
    {
        public IRRelocateLater(string value, OpcodePushRelocateLater opcode) : base(value, opcode)
        {
            // Not technically correct, but this removes it from any optimization.
            ValueType = null;
        }

        internal override IEnumerable<Opcode> EmitPush()
        {
            yield return new OpcodePushRelocateLater((string)Value)
            {
                SourceLine = sourceLine,
                SourceColumn = sourceColumn
            };
        }
    }

    public class IRDelegateRelocateLater : IRRelocateLater
    {
        public bool WithClosure { get; }
        public IRDelegateRelocateLater(string value, bool withClosure, OpcodePushDelegateRelocateLater opcode) : base(value, opcode)
        {
            WithClosure = withClosure;
        }
        internal override IEnumerable<Opcode> EmitPush()
        {
            yield return new OpcodePushDelegateRelocateLater((string)Value, WithClosure)
            {
                SourceLine = sourceLine,
                SourceColumn = sourceColumn
            };
        }
    }

    public class IRTemp : IRVariableBase
    {
        private bool isPromoted = false;

        public override Type ValueType
        {
            get => ((IResultingInstruction)Parent).ResultType;
            set => throw new InvalidOperationException($"Cannot set the result type state of an {nameof(IRTemp)}.");
        }
        public int ID { get; }
        public IRInstruction Parent { get; set; }
        public override bool IsInvariant
        {
            get => Parent.IsInvariant;
            set => throw new InvalidOperationException($"Cannot set the invariant state of an {nameof(IRTemp)}.");
        }

        public IRTemp(int id) : base($"$.temp.{id}", null)
        {
            ID = id;
            Scope = null;
        }
        public IRAssign PromoteToVariable(IRScope scope)
        {
            isPromoted = true;
            Scope = scope;
            return new IRAssign(new OpcodeStoreLocal(Name)
            {
                SourceLine = -1,
                SourceColumn = 0
            },
            this,
            this)
            {
                Scope = IRAssign.StoreScope.Local
            };
        }
        internal override IEnumerable<Opcode> EmitPush()
        {
            if (isPromoted)
                yield return new OpcodePush(Name)
                {
                    SourceLine = -1,
                    SourceColumn = 0
                };
            else
                foreach (Opcode opcode in Parent.EmitOpcode())
                    yield return opcode;
        }

        public override string ToString()
        {
            if (isPromoted)
                return base.ToString();
            return $"| {Parent}";
        }
        public override bool Equals(object obj)
        {
            if (obj is IRTemp temp)
            {
                /*if (isPromoted)
                {
                    return temp.isPromoted &&
                        Scope == temp.Scope &&
                        string.Equals(Name, temp.Name, System.StringComparison.OrdinalIgnoreCase);
                }*/
                return Parent.Equals(temp.Parent);
            }
            if (obj is IRVariable variable)
            {
                return isPromoted &&
                    NameAndScopeEquals(variable);
            }
            return false;
        }
        public override int GetHashCode()
            => GetBaseHashCode();
    }
    public class IRParameter : IRValue
    {
        public override Type ValueType
        {
            get => typeof(Encapsulation.Structure);
            set => throw new InvalidOperationException("Cannot set the value type of a parameter.");
        }
        public override bool IsInvariant
        {
            get => false;
            set
            {
                if (!value)
                    return;
                throw new InvalidOperationException("Cannot set the invariant state of a parameter to true.");
            }
        }
        internal override IEnumerable<Opcode> EmitPush() => System.Linq.Enumerable.Empty<Opcode>();
    }
}
