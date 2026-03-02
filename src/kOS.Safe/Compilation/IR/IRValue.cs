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
        public IRConstant(object value)
            => Value = value;
        internal override IEnumerable<Opcode> EmitPush()
        {
            yield return new OpcodePush(Value);
        }
        public override string ToString()
            => Value.ToString();
    }
    public class IRVariable : IRValue
    {
        public string Name { get; }
        public bool IsLock { get; }
        public IRVariable(string name, bool isLock = false)
        {
            Name = name;
            IsLock = isLock;
        }
        internal override IEnumerable<Opcode> EmitPush()
        {
            yield return new OpcodePush(Name);
        }
        public override string ToString()
            => Name;
    }
    public class IRRelocateLater : IRConstant
    {
        public IRRelocateLater(string value) : base(value) { }

        internal override IEnumerable<Opcode> EmitPush()
        {
            yield return new OpcodePushRelocateLater((string)Value);
        }
    }
    public class IRDelegateRelocateLater : IRRelocateLater
    {
        public bool WithClosure { get; }
        public IRDelegateRelocateLater(string value, bool withClosure) : base(value)
        {
            WithClosure = withClosure;
        }
        internal override IEnumerable<Opcode> EmitPush()
        {
            yield return new OpcodePushDelegateRelocateLater((string)Value, WithClosure);
        }
    }
    public class IRTemp : IRVariable
    {
        public int ID { get; }
        public IRInstruction Parent { get; internal set; }
        private bool isPromoted = false;
        public IRTemp(int id) : base($"$.temp.{id}", false)
        {
            ID = id;
        }
        public IRAssign PromoteToVariable()
        {
            isPromoted = true;
            return new IRAssign(Name, this) { Scope = IRAssign.StoreScope.Local };
        }
        internal override IEnumerable<Opcode> EmitPush()
        {
            if (isPromoted)
                yield return new OpcodePush(Name);
            else
                foreach (Opcode opcode in Parent.EmitOpcode())
                    yield return opcode;
        }
    }
}
