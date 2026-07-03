using System;
using System.Collections.Generic;
using System.Linq;

namespace kOS.Safe.Compilation.IR
{
    public class InterimConstantValue : IInterimOperand, IEvaluatableToConstant
    {
        protected readonly short sourceLine, sourceColumn;

        public virtual bool IsInvariant => true;
        public object Value { get; set; }
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

        public IInterimOperand Clone(BasicBlock _, bool __ = false)
            => new InterimConstantValue(Value, sourceLine, sourceColumn);
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

    public class IRParameter : IInterimOperand, IEvaluatableToConstant
    {
        private readonly int index;
        private IStackTransferObject stackTransferObj;
        public BasicBlock Block { get; }
        public Type Type => StackTransferObject?.Type ?? typeof(Encapsulation.Structure);
        public bool IsInvariant =>
            (StackTransferObject?.IsResolvable ?? false) &&
            (StackTransferObject.Value?.IsInvariant ?? false);
        public bool IsResolvable => IsSetResolvable(this);
        public IStackTransferObject StackTransferObject
        {
            get => stackTransferObj;
            set
            {
                stackTransferObj?.RemoveReference(this);
                stackTransferObj = value;
                stackTransferObj?.AddReference(this);
            }
        }

        //                Depth: 0      1
        // Left operand     -> Left
        // Right operand    -> Right, Left
        // Opcode           -> Result

        // Left param       -> Left
        // Right operand    -> Right, Left
        // Opcode           -> Result

        // Both params      -> Right, Left
        // Opcode           -> Result

        // Both params      -> Right, (Left)
        // Left resolved    -> Left, Right
        // !! Breaks if not commutative !!
        // Opcode           -> Result

        // Both params      -> (Right), Left
        // Right resolved   -> Right, Left
        // Opcode           -> Result

        // Parameters must not be resolved if a higher stack value (lower depth index) is not resolved.
        // Higher stack values will be later in a Depth First search.
        // Since both values will be consumed by the operand, the search originates from
        // the consuming instruction.

        public HashSet<IRParameter> RequiredToBeResolvable { get; } = new HashSet<IRParameter>();
        public IRParameter(int index, BasicBlock block)
        {
            this.index = index;
            Block = block;
        }
        public IEnumerable<Opcode> EmitOpcodes()
            => StackTransferObject?.IsResolvable ?? false ?
            StackTransferObject.Value?.EmitOpcodes() :
            Enumerable.Empty<Opcode>();
        public bool Equals(IInterimOperand other)
            => other == this ||
            ((StackTransferObject?.IsResolvable ?? false) &&
            StackTransferObject.Value.Equals(other));

        public IInterimOperand Clone(BasicBlock block, bool _ = false)
            => new IRParameter(index, block ?? Block);

        public override string ToString()
            => $"Parameter #{index}, BasicBlock#{Block.ID}, Resolved: {StackTransferObject?.IsResolvable}";

        public InterimConstantValue Evaluate()
        {
            if (!IsInvariant)
                throw new InvalidOperationException();
            return ((IEvaluatableToConstant)StackTransferObject.Value).Evaluate();
        }

        public static bool IsOrContainsParameter(IInterimOperand operand)
        {
            if (operand is IRParameter)
                return true;
            if (operand is IOperandInstructionBase operandInstruction)
                return operandInstruction.AnyOperand(IsOrContainsParameter);
            return false;
        }
        public static bool IsSetResolvable(IStackTransferObject push)
        {
            HashSet<IStackTransferObject> pushes = new HashSet<IStackTransferObject>();
            HashSet<IRParameter> parameters = new HashSet<IRParameter>();
            AddToSet(push, pushes, parameters);
            if (pushes.Any(p => p.IsSelfResolvable == false))
                return false;
            return parameters.All(p => p.RequiredToBeResolvable.All(IsSetResolvable));
        }
        public static bool IsSetResolvable(IRParameter parameter)
        {
            HashSet<IStackTransferObject> pushes = new HashSet<IStackTransferObject>();
            HashSet<IRParameter> parameters = new HashSet<IRParameter>();
            AddToSet(parameter, pushes, parameters);
            if (pushes.Any(p => p.IsSelfResolvable == false))
                return false;
            return parameters.All(p => p.RequiredToBeResolvable.All(IsSetResolvable));
        }
        private static void AddToSet(IStackTransferObject push, HashSet<IStackTransferObject> pushSet, HashSet<IRParameter> paramSet)
        {
            if (push == null)
                return;
            if (pushSet.Add(push))
            {
                foreach (IStackTransferObject childPush in push.StackTransferObjects)
                    AddToSet(childPush, pushSet, paramSet);
                foreach (StackTransferPhi phi in push.Controllers)
                    AddToSet(phi, pushSet, paramSet);
                foreach (IRParameter reference in push.References)
                    AddToSet(reference, pushSet, paramSet);
            }
        }
        private static void AddToSet(IRParameter parameter, HashSet<IStackTransferObject> pushSet, HashSet<IRParameter> paramSet)
        {
            if (paramSet.Add(parameter))
                AddToSet(parameter.stackTransferObj, pushSet, paramSet);
        }
    }
}
