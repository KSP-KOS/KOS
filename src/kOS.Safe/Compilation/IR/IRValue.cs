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
        public override string ToString()
            => Value.ToString();
    }
    public abstract class IRVariableBase : IRValue
    {
        public string Name { get; }
        public IRVariableBase(string name)
            => Name = name;
    }
    public class IRVariable : IRVariableBase
    {
        protected readonly short sourceLine, sourceColumn;
        public bool IsLock { get; }
        public IRVariable(string name, IRInstruction instruction, bool isLock = false) : this(name, instruction.SourceLine, instruction.SourceColumn, isLock) { }
        public IRVariable(string name, Opcode opcode, bool isLock = false) : this(name, opcode.SourceLine, opcode.SourceColumn, isLock) { }
        public IRVariable(string name, short sourceLine, short sourceColumn, bool isLock = false) : base(name)
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
        public override string ToString()
            => Name;
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
        public int ID { get; }
        public IRInstruction Parent { get; internal set; }

        private bool isPromoted = false;
        public IRTemp(int id) : base($"$.temp.{id}")
        {
            ID = id;
        }
        public IRAssign PromoteToVariable()
        {
            isPromoted = true;
            return new IRAssign(new OpcodeStoreLocal(Name)
            {
                SourceLine = -1,
                SourceColumn = 0
            }, this)
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
    }
}
