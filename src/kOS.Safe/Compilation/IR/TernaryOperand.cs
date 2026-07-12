using System;
using System.Collections.Generic;
using System.Linq;

namespace kOS.Safe.Compilation.IR
{
    public class TernaryOperand : IInterimOperand, IMultipleOperandInstruction, IEvaluatableToConstant
    {
        public IInterimOperand Condition { get; set; }
        public IInterimOperand TrueValue { get; set; }
        public IInterimOperand FalseValue { get; set; }
        public bool IsInvariant
        {
            get
            {
                bool? condition = EvaluateCondition();
                if (condition == null)
                    return false;
                if (condition == true)
                    return TrueValue.IsInvariant;
                else
                    return FalseValue.IsInvariant;
            }
        }
        public Type Type
        {
            get
            {
                bool? condition = EvaluateCondition();
                if (condition == null)
                    return PhiNode<IInterimOperand>.GetFirstCommonBaseType(TrueValue.Type, FalseValue.Type);
                if (condition == true)
                    return TrueValue.Type;
                else
                    return FalseValue.Type;
            }
        }
        public IEnumerable<IInterimOperand> Operands
        {
            get
            {
                yield return Condition;
                yield return TrueValue;
                yield return FalseValue;
            }
        }
        public int OperandCount => 3;

        public TernaryOperand(IInterimOperand condition, IInterimOperand trueValue, IInterimOperand falseValue)
        {
            Condition = condition;
            TrueValue = trueValue;
            FalseValue = falseValue;
        }

        private bool? EvaluateCondition()
        {
            if ((Condition?.IsInvariant ?? false) &&
                Condition is IEvaluatableToConstant evaluatable)
                return Convert.ToBoolean(evaluatable.Evaluate().Value);
            return null;
        }
        // TODO: Consider interrupting All/Any/Foreach if one value is not executable.
        public bool AllOperands(Func<IInterimOperand, bool> predicate)
            => predicate(Condition) &&
            predicate(TrueValue) &&
            predicate(FalseValue);

        public bool AnyOperand(Func<IInterimOperand, bool> predicate)
            => predicate(Condition) ||
            predicate(TrueValue) ||
            predicate(FalseValue);

        public IEnumerable<Opcode> EmitOpcodes()
        {
            foreach (Opcode op in Condition.EmitOpcodes())
                yield return op;

            List<Opcode> opcodes = FalseValue.EmitOpcodes().ToList();
            yield return new OpcodeBranchIfTrue() { Distance = opcodes.Count + 2 };
            foreach (Opcode op in opcodes)
                yield return op;

            opcodes = TrueValue.EmitOpcodes().ToList();
            yield return new OpcodeBranchJump() { Distance = opcodes.Count + 1 };
            foreach (Opcode op in opcodes)
                yield return op;
            yield break;
        }

        public bool Equals(IInterimOperand other)
        {
            bool? condition = EvaluateCondition();
            if (condition == null)
                return other is TernaryOperand ternary &&
                    (Condition?.Equals(ternary.Condition) ?? false) &&
                    (TrueValue?.Equals(ternary.TrueValue) ?? false) &&
                    (FalseValue?.Equals(ternary.FalseValue) ?? false);
            if (condition == true)
                return TrueValue?.Equals(other) ?? false;
            else
                return FalseValue?.Equals(other) ?? false;
        }

        public override string ToString()
            => $"{Condition} ? {TrueValue} : {FalseValue}";

        public void ForEachOperand(Action<IInterimOperand> action)
        {
            action(Condition);
            action(TrueValue);
            action(FalseValue);
        }

        public void MutateEachOperand(Func<IInterimOperand, IInterimOperand> mutateFunc)
        {
            Condition = mutateFunc(Condition);
            TrueValue = mutateFunc(TrueValue);
            FalseValue = mutateFunc(FalseValue);
        }

        public IInterimOperand Clone(BasicBlock block, bool maintainSSAReferences = false)
            => new TernaryOperand(
                Condition?.Clone(block, maintainSSAReferences),
                TrueValue?.Clone(block, maintainSSAReferences),
                FalseValue?.Clone(block, maintainSSAReferences));

        public InterimConstantValue Evaluate()
        {
            bool condition = EvaluateCondition() ?? throw new InvalidOperationException();
            if (condition)
            {
                if (!((TrueValue?.IsInvariant ?? false) &&
                    TrueValue is IEvaluatableToConstant trueValue))
                    throw new InvalidOperationException();
                return trueValue.Evaluate();
            }
            else
            {
                if (!((FalseValue?.IsInvariant ?? false) &&
                    FalseValue is IEvaluatableToConstant falseValue))
                    throw new InvalidOperationException();
                return falseValue.Evaluate();
            }
        }
    }
}
