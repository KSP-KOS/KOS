using System;
using System.Collections.Generic;

namespace kOS.Safe.Compilation.IR
{
    public class InterimConstantValue : IInterimOperand, IEvaluatableToConstant
    {
        protected readonly short sourceLine, sourceColumn;

        public virtual bool IsInvariant => true;
        public object Value { get; }
        public Type Type { get; protected set; }
        public bool IsPrimitive { get => Type.IsPrimitive || typeof(Encapsulation.PrimitiveStructure).IsAssignableFrom(Type); }
        public InterimConstantValue(object value, Opcode opcode) : this(value, opcode.SourceLine, opcode.SourceColumn) { }
        public InterimConstantValue(object value, IRInstruction instruction) : this(value, instruction.SourceLine, instruction.SourceColumn) { }
        public InterimConstantValue(object value, short sourceLine, short sourceColumn)
        {
            Value = value;
            this.sourceLine = sourceLine;
            this.sourceColumn = sourceColumn;
            Type = value.GetType();
        }
        public virtual IEnumerable<Opcode> EmitOpcodes()
        {
            yield return new OpcodePush(Value)
            {
                SourceLine = sourceLine,
                SourceColumn = sourceColumn
            };
        }
        public bool Equals(InterimConstantValue other)
            => Value.Equals(other.Value);
        public bool Equals(IInterimOperand other)
            => (other is InterimConstantValue constant && Equals(constant)) ||
            (other is IEvaluatableToConstant evaluatableToConstant &&
            evaluatableToConstant.IsInvariant &&
            Equals(evaluatableToConstant.Evaluate()));
        public override bool Equals(object obj)
            => (obj is InterimConstantValue constant &&
            Equals(constant)) ||
            Value.Equals(obj);
        public override int GetHashCode()
            => Value.GetHashCode();
        public override string ToString()
            => Value.ToString();

        InterimConstantValue IEvaluatableToConstant.Evaluate()
            => this;
    }

    public class IRRelocateLater : InterimConstantValue
    {
        public IRRelocateLater(string value, OpcodePushRelocateLater opcode) : base(value, opcode)
        {
            // Not technically correct, but this removes it from any optimization.
            Type = null;
        }

        public override IEnumerable<Opcode> EmitOpcodes()
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
        public override IEnumerable<Opcode> EmitOpcodes()
        {
            yield return new OpcodePushDelegateRelocateLater((string)Value, WithClosure)
            {
                SourceLine = sourceLine,
                SourceColumn = sourceColumn
            };
        }
    }

    public class IRParameter : IInterimOperand
    {
        public Type Type => null;//typeof(Encapsulation.Structure);
        public bool IsInvariant => false;
        public IEnumerable<Opcode> EmitOpcodes() => System.Linq.Enumerable.Empty<Opcode>();
        public bool Equals(IInterimOperand other)
            => other == this;
    }
}
