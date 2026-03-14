using System.Collections.Generic;

namespace kOS.Safe.Compilation.IR
{
    public abstract class IRValue
    {
        public enum ValueType
        {
            Unknown = 0,
            Value = 1,
            GameObject = 2
        }
        public ValueType Type { get; }
        internal abstract IEnumerable<Opcode> EmitPush();
    }

    public class IRConstant : IRValue
    {
        public object Value { get; }
        protected readonly short sourceLine, sourceColumn;
        public IRConstant(object value, Opcode opcode) : this(value, opcode.SourceLine, opcode.SourceColumn) { }
        public IRConstant(object value, IRInstruction instruction) : this(value, instruction.SourceLine, instruction.SourceColumn) { }
        public IRConstant(object value, short sourceLine, short sourceColumn)
        {
            Value = value;
            this.sourceLine = sourceLine;
            this.sourceColumn = sourceColumn;
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
        public override string ToString()
            => $"{Name} {Scope.IndexString()}";
        public override bool Equals(object obj)
            => obj is IRVariableBase variable &&
                (Scope == variable.Scope ||
                Scope.IsEncompassedBy(variable.Scope) ||
                variable.Scope.IsEncompassedBy(Scope)) &&
                string.Equals(Name, variable.Name, System.StringComparison.OrdinalIgnoreCase);
        public override int GetHashCode()
            => Name.ToLower().GetHashCode();
    }
    public class IRVariable : IRVariableBase
    {
        protected readonly short sourceLine, sourceColumn;
        public bool IsLock { get; }
        public override IRScope Scope { get => base.Scope; }
        public IRVariable(string name, IRScope scope, IRInstruction instruction, bool isLock = false) :
            this(name, scope, instruction.SourceLine, instruction.SourceColumn, isLock) { }
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
        internal override IEnumerable<Opcode> EmitPush()
        {
            yield return new OpcodePush(Name)
            {
                SourceLine = sourceLine,
                SourceColumn = sourceColumn
            };
        }
    }
    public class IRRelocateLater : IRConstant
    {
        public IRRelocateLater(string value, OpcodePushRelocateLater opcode) : base(value, opcode) { }

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

        public int ID { get; }
        public IRInstruction Parent { get; internal set; }

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
                    base.Equals(variable);
            }
            return false;
        }
        public override string ToString()
        {
            if (isPromoted)
                return base.ToString();
            return $"| {Parent}";
        }
        public override int GetHashCode()
            => base.GetHashCode();
    }
    public class IRParameter : IRValue
    {
        internal override IEnumerable<Opcode> EmitPush() => System.Linq.Enumerable.Empty<Opcode>();
    }
}
