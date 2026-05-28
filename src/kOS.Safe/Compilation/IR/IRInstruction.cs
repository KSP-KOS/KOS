using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Exceptions;

namespace kOS.Safe.Compilation.IR
{
    public abstract class IRInstruction
    {
        public BasicBlock Block { get; }
        public short SourceLine { get; private set; }   // line number in the source code that this was compiled from.
        public short SourceColumn { get; private set; } // column number of the token nearest the cause of this Opcode.

        public abstract bool IsInvariant { get; }
        public abstract IEnumerable<Opcode> EmitOpcodes();
        protected IRInstruction(Opcode originalOpcode, BasicBlock block)
        {
            SourceLine = originalOpcode.SourceLine;
            SourceColumn = originalOpcode.SourceColumn;
            Block = block;
        }
        protected Opcode SetSourceLocation(Opcode opcode)
        {
            if (opcode == null)
                return opcode;
            opcode.SourceLine = SourceLine;
            opcode.SourceColumn = SourceColumn;
            return opcode;
        }
        public void OverwriteSourceLocation(short sourceLine, short sourceColumn)
        {
            SourceLine = sourceLine;
            SourceColumn = sourceColumn;
        }
    }

    public abstract class SingleOperandInstruction : IRInstruction, ISingleOperandInstruction
    {
        protected IInterimOperand operand;

        protected SingleOperandInstruction(Opcode originalOpcode, BasicBlock block) : base(originalOpcode, block) { }

        IInterimOperand ISingleOperandInstruction.Operand { get => operand; set => operand = value; }

        public void ForEachOperand(Action<IInterimOperand> action)
            => action(operand);

        public void MutateEachOperand(Func<IInterimOperand, IInterimOperand> mutateFunc)
            => operand = mutateFunc(operand);
    }
    public abstract class MultipleOperandInstruction : IRInstruction, IMultipleOperandInstruction
    {
        protected MultipleOperandInstruction(Opcode originalOpcode, BasicBlock block) : base(originalOpcode, block) { }

        public abstract IEnumerable<IInterimOperand> Operands { get; }
        public abstract int OperandCount { get; }
        /// <summary>
        /// Allows replacing operands from a common function.
        /// The meaning of the index and ordering are irrelevant,
        /// as long as it covers the range [0, <see cref="OperandCount"/>).
        /// </summary>
        protected abstract IInterimOperand this[int index] { get; set; }

        public void ForEachOperand(Action<IInterimOperand> action)
        {
            foreach (IInterimOperand operand in Operands)
                action(operand);
        }

        public void MutateEachOperand(Func<IInterimOperand, IInterimOperand> mutateFunc)
        {
            for (int i = 0; i < OperandCount; i++)
                this[i] = mutateFunc(this[i]);
        }
    }

    public class IRAssign : SingleOperandInstruction
    {
        public enum StoreScope
        {
            Ambivalent,
            Local,
            Global
        }
        public override bool IsInvariant => Value.IsInvariant;
        public SSASetDefinition Target { get; set; }
        public IInterimOperand Value { get => operand; set => operand = value; }
        public StoreScope Scope { get; set; } = StoreScope.Ambivalent;
        public bool AssertExists { get; set; } = false;

        public IRAssign(BasicBlock block, OpcodeIdentifierBase opcode, IInterimOperand value) : base(opcode, block)
        {
            Value = value;
            Target = new SSASetDefinition(opcode.Identifier, this);
        }
        public override IEnumerable<Opcode> EmitOpcodes()
        {
            if (Value != null)
                foreach (Opcode opcode in Value.EmitOpcodes())
                    yield return opcode;
            if (AssertExists)
            {
                yield return SetSourceLocation(new OpcodeStoreExist(Target.Name));
                yield break;
            }
            switch (Scope)
            {
                case StoreScope.Local:
                    yield return SetSourceLocation(new OpcodeStoreLocal(Target.Name));
                    yield break;
                case StoreScope.Global:
                    yield return SetSourceLocation(new OpcodeStoreGlobal(Target.Name));
                    yield break;
                default:
                case StoreScope.Ambivalent:
                    yield return SetSourceLocation(new OpcodeStore(Target.Name));
                    yield break;
            }
        }
        public override string ToString()
            => string.Format("{{store {0} -> {1}}}", Value.ToString(), Target.ToString());
    }
    public class IRBinaryOp : MultipleOperandInstruction, IResultingInstruction
    {
        public override bool IsInvariant => Left.IsInvariant && Right.IsInvariant;
        public BinaryOpcode Operation { get; set; }
        public IInterimOperand Left { get; set; }
        public IInterimOperand Right { get; set; }
        public override IEnumerable<IInterimOperand> Operands { get { yield return Left; yield return Right; } }
        public override int OperandCount => 2;
        public Type Type
        {
            get
            {
                if (Left.Type == null || Right.Type == null)
                    return null;
                Calculator calculator = Calculator.GetCalculator(Left.Type, Right.Type);
                switch (Operation)
                {
                    case OpcodeMathAdd _:
                        return calculator.GetAddResultType(Left.Type, Right.Type);
                    case OpcodeMathSubtract _:
                        return calculator.GetSubtractResultType(Left.Type, Right.Type);
                    case OpcodeMathMultiply _:
                        return calculator.GetMultiplyResultType(Left.Type, Right.Type);
                    case OpcodeMathDivide _:
                        return calculator.GetDivideResultType(Left.Type, Right.Type);
                    case OpcodeMathPower _:
                        return calculator.GetPowerResultType(Left.Type, Right.Type);
                    case OpcodeCompareEqual _:
                        return calculator.GetEqualResultType(Left.Type, Right.Type);
                    case OpcodeCompareNE _:
                        return calculator.GetNotEqualResultType(Left.Type, Right.Type);
                    case OpcodeCompareGT _:
                        return calculator.GetGreaterThanResultType(Left.Type, Right.Type);
                    case OpcodeCompareLT _:
                        return calculator.GetLessThanResultType(Left.Type, Right.Type);
                    case OpcodeCompareGTE _:
                        return calculator.GetGreaterThanEqualResultType(Left.Type, Right.Type);
                    case OpcodeCompareLTE _:
                        return calculator.GetLessThanEqualResultType(Left.Type, Right.Type);
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        protected override IInterimOperand this[int index]
        {
            get => index == 0 ? Left : index == 1 ? Right : throw new ArgumentOutOfRangeException();
            set
            {
                if (index == 0)
                    Left = value;
                else if (index == 1)
                    Right = value;
                else
                    throw new ArgumentOutOfRangeException();
            }
        }
        public bool IsCommutative
        {
            get
            {
                if (Left.Type == null || Right.Type == null)
                    return false;
                Calculator calculator = Calculator.GetCalculator(Left.Type, Right.Type);
                switch (Operation)
                {
                    case OpcodeMathAdd _:
                        return calculator.IsAdditionCommutative(Left.Type, Right.Type);
                    case OpcodeMathSubtract _:
                        return calculator.IsSubtractionCommutativeWithNegation(Left.Type, Right.Type);
                    case OpcodeMathMultiply _:
                        return calculator.IsMultiplicationCommmutative(Left.Type, Right.Type);
                    case OpcodeMathDivide _:
                        return calculator.IsDivisionCommutative(Left.Type, Right.Type);
                    case OpcodeMathPower _:
                        return false;
                    case OpcodeCompareEqual _:
                    case OpcodeCompareNE _:
                        return true;
                    case OpcodeCompareGT _:
                    case OpcodeCompareLT _:
                    case OpcodeCompareGTE _:
                    case OpcodeCompareLTE _:
                        return true;
                    default:
#pragma warning disable CS0162 // Unreachable code detected
#if DEBUG
                        throw new NotImplementedException();
#endif
                        return false;
#pragma warning restore CS0162 // Unreachable code detected
                }
            }
        }

        public IRBinaryOp(BasicBlock block, BinaryOpcode operation, IInterimOperand left, IInterimOperand right) : base(operation, block)
        {
            Operation = operation;
            Left = left;
            Right = right;
        }
        public bool SwapOperands()
        {
            if (!IsCommutative)
                return false;
            if (Operation is OpcodeMathSubtract)
            {
                Right = new IRUnaryOp(Block, new OpcodeMathNegate(), Right);
                Operation = new OpcodeMathAdd();
            }
            else if (Operation is OpcodeMathAdd &&
                Left is IRUnaryOp negation)
            {
                Left = negation.Operand;
                Operation = new OpcodeMathSubtract();
            }
            (Right, Left) = (Left, Right);
            switch (Operation)
            {
                case OpcodeCompareGT _:
                    Operation = new OpcodeCompareLTE();
                    break;
                case OpcodeCompareLT _:
                    Operation = new OpcodeCompareGTE();
                    break;
                case OpcodeCompareGTE _:
                    Operation = new OpcodeCompareLT();
                    break;
                case OpcodeCompareLTE _:
                    Operation = new OpcodeCompareGT();
                    break;
            }
            return true;
        }

        public IInterimOperand Clone(BasicBlock block)
            => new IRBinaryOp(block, (BinaryOpcode)SetSourceLocation(Operation), Left.Clone(block), Right.Clone(block));

        public override IEnumerable<Opcode> EmitOpcodes()
        {
            foreach (Opcode opcode in Left.EmitOpcodes())
                yield return opcode;
            foreach (Opcode opcode in Right.EmitOpcodes())
                yield return opcode;
            Operation.Label = string.Empty;
            yield return SetSourceLocation(Operation);
        }

        public override string ToString()
            => Operation.ToString();
        public bool Equals(IInterimOperand other)
        {
            if (other is IRBinaryOp binaryOp &&
                Operation.GetType() == binaryOp.Operation.GetType())
            {
                return (Left.Equals(binaryOp.Left) && Right.Equals(binaryOp.Right)) ||
                    (IsCommutative && Left.Equals(binaryOp.Right) && Right.Equals(binaryOp.Left));
            }
            if (IsInvariant &&
                other is IEvaluatableToConstant evaluatableToConstant &&
                evaluatableToConstant.IsInvariant)
                return Evaluate().Equals(evaluatableToConstant.Evaluate());
            return false;
        }
        public override bool Equals(object obj)
        {
            if (obj is IRBinaryOp binaryOp &&
                Operation.GetType() == binaryOp.Operation.GetType())
            {
                return (Left.Equals(binaryOp.Left) && Right.Equals(binaryOp.Right)) ||
                    (IsCommutative && Left.Equals(binaryOp.Right) && Right.Equals(binaryOp.Left));
            }
            if (IsInvariant &&
                obj is IEvaluatableToConstant evaluatableToConstant &&
                evaluatableToConstant.IsInvariant)
                return Evaluate().Equals(evaluatableToConstant.Evaluate());
            return false;
        }
        public override int GetHashCode()
            => Operation.GetHashCode();

        public InterimConstantValue Evaluate()
        {
            if (!IsInvariant)
                throw new InvalidOperationException();
            object left = (Left as IEvaluatableToConstant)?.Evaluate().Value;
            object right = (Right as IEvaluatableToConstant)?.Evaluate().Value;
            try
            {
                return new InterimConstantValue(Operation.ExecuteCalculation(left, right), this);
            }
            catch (KOSBinaryOperandTypeException binaryTypeException)
            {
                throw new KOSCompileException(this, binaryTypeException);
            }
        }
    }
    public class IRUnaryOp : SingleOperandInstruction, IResultingInstruction
    {
        public override bool IsInvariant => Operand.IsInvariant && !(Operation is OpcodeExists);
        public Opcode Operation { get; }
        public IInterimOperand Operand { get => operand; set => operand = value; }
        public Type Type
        {
            get
            {
                switch (Operation)
                {
                    case OpcodeExists _:
                    case OpcodeLogicNot _:
                    case OpcodeLogicToBool _:
                        return typeof(Encapsulation.BooleanValue);
                    case OpcodeMathNegate _:
                        return Operand.Type;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
        public IRUnaryOp(BasicBlock block, Opcode operation, IInterimOperand operand) : base(operation, block)
        {
            Operation = operation;
            Operand = operand;
        }

        public IInterimOperand Clone(BasicBlock block)
            => new IRUnaryOp(block, Operation, Operand.Clone(block));
        public override IEnumerable<Opcode> EmitOpcodes()
        {
            foreach (Opcode opcode in Operand.EmitOpcodes())
                yield return opcode;
            Operation.Label = string.Empty;
            yield return Operation;
        }
        public override string ToString()
            => Operation.ToString();
        public bool Equals(IInterimOperand other)
            => (other is IRUnaryOp unaryOp &&
                Operation.GetType() == unaryOp.Operation.GetType() &&
                Operand.Equals(unaryOp.Operand)) ||
                (IsInvariant &&
                other is IEvaluatableToConstant evaluatableToConstant &&
                evaluatableToConstant.IsInvariant &&
                Evaluate().Equals(evaluatableToConstant.Evaluate()));
        public override bool Equals(object obj)
            => obj is IRUnaryOp unaryOp &&
                Operation.GetType() == unaryOp.Operation.GetType() &&
                Operand.Equals(unaryOp.Operand);
        public override int GetHashCode()
            => (Operation, Operand).GetHashCode();

        public InterimConstantValue Evaluate()
        {
            if (!IsInvariant)
                throw new InvalidOperationException();
            object input = (Operand as IEvaluatableToConstant)?.Evaluate().Value;
            try
            {
                switch (Operation)
                {
                    case OpcodeMathNegate _:
                        return new InterimConstantValue(OpcodeMathNegate.StaticOperation(input), this);
                    case OpcodeLogicNot _:
                        return new InterimConstantValue(OpcodeLogicNot.StaticOperation(input), this);
                    case OpcodeLogicToBool _:
                        return new InterimConstantValue(OpcodeLogicToBool.StaticOperation(input), this);
                    default:
                        throw new NotImplementedException();
                }
            }
            catch (KOSUnaryOperandTypeException unaryTypeException)
            {
                throw new KOSCompileException(this, unaryTypeException);
            }
        }
    }
    public class IRNoStackInstruction : IRInstruction
    {
        public override bool IsInvariant { get; } = false;
        public Opcode Operation { get; }
        public IRNoStackInstruction(BasicBlock block, Opcode opcode) : base(opcode, block)
            => Operation = opcode;
        public IRNoStackInstruction(BasicBlock block, Opcode opcode, bool isInvariant) : this(block, opcode)
            => IsInvariant = isInvariant;
        public override IEnumerable<Opcode> EmitOpcodes()
        {
            Operation.Label = string.Empty;
            yield return Operation;
        }
        public override string ToString()
            => Operation.ToString();
    }
    public class IRUnaryConsumer : SingleOperandInstruction
    {
        private readonly bool operationHasSideEffects;
        public override bool IsInvariant => !operationHasSideEffects && Operand.IsInvariant;
        public Opcode Operation { get; }
        public IInterimOperand Operand { get => operand; set => operand = value; }
        public IRUnaryConsumer(BasicBlock block, Opcode opcode, IInterimOperand operand, bool sideEffects = false) : base(opcode, block)
        {
            Operation = opcode;
            Operand = operand;
            operationHasSideEffects = sideEffects;
        }
        public override IEnumerable<Opcode> EmitOpcodes()
        {
            foreach (Opcode opcode in Operand.EmitOpcodes())
                yield return opcode;
            Operation.Label = string.Empty;
            yield return Operation;
        }
        public override string ToString()
            => Operation.ToString();
    }
    public class IRUnset : IRUnaryConsumer
    {
        public override bool IsInvariant => Target != null;
        public SSASetDefinition Target { get; set; }
        public bool IsExecutable => Block.IsExecutable;
        public IRUnset(BasicBlock block, OpcodeUnset opcode, IInterimOperand operand) : base(block, opcode, operand, false)
        {
            if (operand.IsInvariant)
            {
                string name = (string)(operand as IEvaluatableToConstant).Evaluate().Value;
                Target = new SSASetDefinition(name, this);
            }
        }
    }
    public class IRPop : SingleOperandInstruction
    {
        public override bool IsInvariant => Value.IsInvariant;
        public IInterimOperand Value { get => operand; set => operand = value; }
        public IRPop(BasicBlock block, IInterimOperand value, OpcodePop opcode) : base(opcode, block)
            => Value = value;

        public override IEnumerable<Opcode> EmitOpcodes()
        {
            foreach (Opcode opcode in Value.EmitOpcodes())
                yield return opcode;
            yield return SetSourceLocation(new OpcodePop());
        }
        public override string ToString()
            => $"{{pop {Value}}}";
    }
    public class IRNonVarPush : IRInstruction, IResultingInstruction
    {
        public override bool IsInvariant => false;
        public Opcode Operation { get; }
        public Type Type
        {
            get
            {
                switch (Operation)
                {
                    case OpcodeTestArgBottom _:
                        return typeof(Encapsulation.BooleanValue);
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        public IRNonVarPush(BasicBlock block, Opcode opcode) : base(opcode, block)
        {
            Operation = opcode;
        }
        public IInterimOperand Clone(BasicBlock block)
            => new IRNonVarPush(block, Operation);
        public override IEnumerable<Opcode> EmitOpcodes()
        {
            Operation.Label = string.Empty;
            yield return Operation;
        }
        public override string ToString()
            => Operation.ToString();
        public bool Equals(IInterimOperand other)
            => other == this;
        public InterimConstantValue Evaluate()
            => throw new InvalidOperationException();
    }
    public class IRSuffixGet : SingleOperandInstruction, IResultingInstruction
    {
        // TODO: Consider implementing this.
        public override bool IsInvariant => false && Object.IsInvariant;
        public IInterimOperand Object { get => operand; set => operand = value; }
        public string Suffix { get; set; }
        public Type Type => TypeInferencer.GetTypeForSuffix(Object.Type, Suffix);
        public IRSuffixGet(BasicBlock block, IInterimOperand obj, OpcodeGetMember opcodeGetMember) : base(opcodeGetMember, block)
        {
            Object = obj;
            Suffix = opcodeGetMember.Identifier;
        }
        public IInterimOperand Clone(BasicBlock block)
            => new IRSuffixGet(block, Object.Clone(block), new OpcodeGetMember(Suffix) { SourceLine = SourceLine, SourceColumn = SourceColumn });
        public override IEnumerable<Opcode> EmitOpcodes()
        {
            foreach (Opcode opcode in Object.EmitOpcodes())
                yield return opcode;
            yield return SetSourceLocation(new OpcodeGetMember(Suffix));
        }
        public override string ToString()
            => string.Format("{{gmb \"{0}\"}}", Suffix);
        public bool Equals(IInterimOperand other)
            => other == this ||
            (IsInvariant &&
            other is IRSuffixGet suffixGet &&
            suffixGet.IsInvariant &&
            !(suffixGet is IRSuffixGetMethod) &&
            string.Equals(Suffix, suffixGet.Suffix, StringComparison.OrdinalIgnoreCase) &&
            Object == suffixGet.Object);
        public override bool Equals(object obj)
            => obj == this ||
            (IsInvariant &&
            obj is IRSuffixGet suffixGet &&
            suffixGet.IsInvariant &&
            !(suffixGet is IRSuffixGetMethod) &&
            string.Equals(Suffix, suffixGet.Suffix, StringComparison.OrdinalIgnoreCase) &&
            Object == suffixGet.Object);
        public override int GetHashCode()
            => (Object, Suffix).GetHashCode();

        public InterimConstantValue Evaluate()
        {
            if (!IsInvariant)
                throw new InvalidOperationException();
            throw new NotImplementedException();
#pragma warning disable CS0162 // Unreachable code detected
            Encapsulation.Structure obj = (Encapsulation.Structure)(Object as IEvaluatableToConstant).Evaluate()?.Value;
#pragma warning restore CS0162 // Unreachable code detected
            object result = obj.GetSuffix(Suffix);
            return new InterimConstantValue(result, this);
        }
    }
    public class IRSuffixGetMethod : IRSuffixGet
    {
        public IRSuffixGetMethod(BasicBlock block, IInterimOperand obj, OpcodeGetMethod opcode) : base(block, obj, opcode) { }
        public override IEnumerable<Opcode> EmitOpcodes()
        {
            foreach (Opcode opcode in Object.EmitOpcodes())
                yield return opcode;
            yield return SetSourceLocation(new OpcodeGetMethod(Suffix));
        }
        public override string ToString()
            => string.Format("{{gmet \"{0}\"}}", Suffix);
        public override bool Equals(object obj)
            => obj is IRSuffixGetMethod &&
            base.Equals(obj);
        public override int GetHashCode()
            => base.GetHashCode();
    }
    public class IRSuffixSet : MultipleOperandInstruction
    {
        public override bool IsInvariant => false;
        public IInterimOperand Object { get; set; }
        public IInterimOperand Value { get; set; }
        public override IEnumerable<IInterimOperand> Operands { get { yield return Object; yield return Value; } }
        public override int OperandCount => 2;
        protected override IInterimOperand this[int index]
        {
            get => index == 0 ? Object : index == 1 ? Value : throw new ArgumentOutOfRangeException();
            set
            {
                if (index == 0)
                    Object = value;
                else if (index == 1)
                    Value = value;
                else
                    throw new ArgumentOutOfRangeException();
            }
        }
        public string Suffix { get; }
        public IRSuffixSet(BasicBlock block, IInterimOperand obj, IInterimOperand value, OpcodeSetMember opcodeSetMember) : base(opcodeSetMember, block)
        {
            Object = obj;
            Value = value;
            Suffix = opcodeSetMember.Identifier;
        }
        public override IEnumerable<Opcode> EmitOpcodes()
        {
            foreach (Opcode opcode in Object.EmitOpcodes())
                yield return opcode;
            foreach (Opcode opcode in Value.EmitOpcodes())
                yield return opcode;
            yield return SetSourceLocation(new OpcodeSetMember(Suffix));
        }
        public override string ToString()
            => string.Format("{{smb \"{0}\"}}", Suffix);
        public override bool Equals(object obj)
            => obj == this ||
                (IsInvariant &&
                obj is IRSuffixSet suffixSet &&
                suffixSet.IsInvariant &&
                string.Equals(Suffix, suffixSet.Suffix, StringComparison.OrdinalIgnoreCase) &&
                Object.Equals(suffixSet.Object) &&
                Value.Equals(suffixSet.Value));
        public override int GetHashCode()
            => (Object, Suffix, Value).GetHashCode();
    }
    public class IRIndexGet : MultipleOperandInstruction, IResultingInstruction
    {
        // TODO: Consider implementing this.
        public override bool IsInvariant => false && Object.IsInvariant && Index.IsInvariant;
        public IInterimOperand Object { get; set; }
        public IInterimOperand Index { get; set; }
        public override IEnumerable<IInterimOperand> Operands { get { yield return Object; yield return Index; } }
        public override int OperandCount => 2;
        public Type Type => TypeInferencer.GetTypeForIndex(Object.Type);
        protected override IInterimOperand this[int index]
        {
            get => index == 0 ? Object : index == 1 ? Index : throw new ArgumentOutOfRangeException();
            set
            {
                if (index == 0)
                    Object = value;
                else if (index == 1)
                    Index = value;
                else
                    throw new ArgumentOutOfRangeException();
            }
        }
        public IRIndexGet(BasicBlock block, IInterimOperand obj, IInterimOperand index, OpcodeGetIndex opcode) : base(opcode, block)
        {
            Object = obj;
            Index = index;
        }
        public IInterimOperand Clone(BasicBlock block)
            => new IRIndexGet(block, Object.Clone(block), Index.Clone(block), new OpcodeGetIndex() { SourceLine = SourceLine, SourceColumn = SourceColumn });
        public override IEnumerable<Opcode> EmitOpcodes()
        {
            foreach (Opcode opcode in Object.EmitOpcodes())
                yield return opcode;
            foreach (Opcode opcode in Index.EmitOpcodes())
                yield return opcode;
            yield return SetSourceLocation(new OpcodeGetIndex());
        }
        public override string ToString()
            => "{gidx}";
        public bool Equals(IInterimOperand other)
            => other == this ||
            (IsInvariant &&
            other is IRIndexGet indexGet &&
            indexGet.IsInvariant &&
            Object.Equals(indexGet.Object) &&
            Index.Equals(indexGet.Index));
        public override bool Equals(object obj)
            => obj == this ||
            (IsInvariant &&
            obj is IRIndexGet indexGet &&
            indexGet.IsInvariant &&
            Object.Equals(indexGet.Object) &&
            Index.Equals(indexGet.Index));
        public override int GetHashCode()
            => (Object, Index).GetHashCode();

        public InterimConstantValue Evaluate()
        {
            if (!IsInvariant)
                throw new InvalidOperationException();
            throw new NotImplementedException();
            //((Encapsulation.IIndexable)Object).GetIndex();
        }
    }
    public class IRIndexSet : MultipleOperandInstruction
    {
        public override bool IsInvariant => false;
        public IInterimOperand Object { get; set; }
        public IInterimOperand Index { get; set; }
        public IInterimOperand Value { get; set; }
        public override IEnumerable<IInterimOperand> Operands { get { yield return Object; yield return Index; yield return Value; } }
        public override int OperandCount => 3;
        protected override IInterimOperand this[int index]
        {
            get => index == 0 ? Object : index == 1 ? Index : index == 2 ? Value : throw new ArgumentOutOfRangeException();
            set
            {
                if (index == 0)
                    Object = value;
                else if (index == 1)
                    Index = value;
                else if (index == 2)
                    Value = value;
                else
                    throw new ArgumentOutOfRangeException();
            }
        }
        public IRIndexSet(BasicBlock block, IInterimOperand obj, IInterimOperand index, IInterimOperand value, OpcodeSetIndex opcode) : base(opcode, block)
        {
            Object = obj;
            Index = index;
            Value = value;
        }
        public override IEnumerable<Opcode> EmitOpcodes()
        {
            foreach (Opcode opcode in Object.EmitOpcodes())
                yield return opcode;
            foreach (Opcode opcode in Index.EmitOpcodes())
                yield return opcode;
            foreach (Opcode opcode in Value.EmitOpcodes())
                yield return opcode;
            yield return SetSourceLocation(new OpcodeSetIndex());
        }
        public override string ToString()
            => "{sidx}";
        public override bool Equals(object obj)
            => obj == this ||
            (IsInvariant && 
            obj is IRIndexSet indexGet &&
            indexGet.IsInvariant &&
            Object.Equals(indexGet.Object) &&
            Index.Equals(indexGet.Index) &&
            Value.Equals(indexGet.Value));
        public override int GetHashCode()
            => Object.GetHashCode();
    }
    public class IRJump : IRInstruction
    {
        public override bool IsInvariant => true;
        public BasicBlock Target { get; set; }
        public IRJump(BasicBlock block, BasicBlock target, OpcodeBranchJump opcode) : base(opcode, block)
        {
            Target = target;
        }
        public IRJump(BasicBlock block, BasicBlock target, short sourceLine, short sourceColumn)
            : this(block, target, new OpcodeBranchJump() { SourceLine = sourceLine, SourceColumn = sourceColumn }) { }
        public override IEnumerable<Opcode> EmitOpcodes()
        {
            yield return SetSourceLocation(new OpcodeBranchJump() { DestinationLabel = Target.Label });
        }
        public override string ToString()
            => string.Format("{{jump {0}}}", Target.Label);
        public override bool Equals(object obj)
            => obj is IRJump jump &&
                Target == jump.Target;
        public override int GetHashCode()
            => Target.GetHashCode();
    }
    public class IRJumpStack : SingleOperandInstruction
    {
        public override bool IsInvariant => Distance.IsInvariant;
        public IInterimOperand Distance { get => operand; set => operand = value; }
        public List<BasicBlock> Targets { get; } = new List<BasicBlock>();
        public IRJumpStack(BasicBlock block, IInterimOperand distance, IEnumerable<BasicBlock> targets, OpcodeJumpStack jumpStack) : base(jumpStack, block)
        {
            Distance = distance;
            Targets.AddRange(targets);
        }
        public override IEnumerable<Opcode> EmitOpcodes()
        {
            foreach (Opcode opcode in Distance.EmitOpcodes())
                yield return opcode;
            yield return SetSourceLocation(new OpcodeJumpStack());
        }
        public override bool Equals(object obj)
            => obj is IRJumpStack jumpStack &&
                Distance == jumpStack.Distance &&
                Targets.SequenceEqual(jumpStack.Targets);
        public override int GetHashCode()
            => Targets.GetHashCode();
    }
    public class IRBranch : SingleOperandInstruction
    {
        public override bool IsInvariant => Condition.IsInvariant;
        public IInterimOperand Condition { get => operand; set => operand = value; }
        public BasicBlock True { get; set; }
        public BasicBlock False { get; set; }
        public bool PreferFalse { get; set; } = false;
        public IRBranch(BasicBlock block, IInterimOperand condition, BasicBlock onTrue, BasicBlock onFalse, BranchOpcode opcodeBranch) : base(opcodeBranch, block)
        {
            Condition = condition;
            True = onTrue;
            False = onFalse;
            PreferFalse = opcodeBranch is OpcodeBranchIfFalse;
        }
        public override IEnumerable<Opcode> EmitOpcodes()
        {
            foreach (Opcode opcode in Condition.EmitOpcodes())
                yield return opcode;
            if (PreferFalse)
            {
                yield return SetSourceLocation(new OpcodeBranchIfFalse() { DestinationLabel = False.Label });
                yield return SetSourceLocation(new OpcodeBranchJump() { DestinationLabel = True.Label });
            }
            else
            {
                yield return SetSourceLocation(new OpcodeBranchIfTrue() { DestinationLabel = True.Label });
                yield return SetSourceLocation(new OpcodeBranchJump() { DestinationLabel = False.Label });
            }
        }
        public IRBranch Clone(BasicBlock block)
        {
            BranchOpcode opcode;
            if (PreferFalse)
                opcode = new OpcodeBranchIfFalse();
            else
                opcode = new OpcodeBranchIfTrue();
            opcode.SourceLine = SourceLine;
            opcode.SourceColumn = SourceColumn;
            return new IRBranch(block, Condition.Clone(block), True, False, opcode);
        }
        public override string ToString()
            => string.Format("{{br.? {0}/{1}}}", True.Label, False.Label);
        public override bool Equals(object obj)
            => obj is IRBranch branch &&
                Condition.Equals(branch.Condition) &&
                True == branch.True &&
                False == branch.False;
        public override int GetHashCode()
            => True.GetHashCode() ^ False.GetHashCode();
    }
    public class IRCall : MultipleOperandInstruction, IResultingInstruction
    {
        public override bool IsInvariant => IsCallInvariant() && Arguments.All(a => a.IsInvariant);
        public string Function { get; }
        public List<IInterimOperand> Arguments { get; } = new List<IInterimOperand>();
        public override IEnumerable<IInterimOperand> Operands => Enumerable.Reverse(Arguments);
        public override int OperandCount => Arguments.Count;
        public Type Type => GetDefaultReturnType();
        protected override IInterimOperand this[int index]
        {
            get => Arguments[index];
            set => Arguments[index] = value;
        }
        public IInterimOperand IndirectMethod { get; internal set; }
        public bool Direct { get; }
        public bool EmitArgMarker { get; set; }
        private IRCall(BasicBlock block, OpcodeCall opcode, bool emitArgMarker) : base(opcode, block)
        {
            Function = (string)opcode.Destination;
            Direct = opcode.Direct;
            EmitArgMarker = emitArgMarker;
        }
        private bool IsCallInvariant()
        {
            // TODO: Consider that some suffix methods may actually be known at compile time.
            IRCodePart.IRFunction function = Block?.CodePart?.GetFunction(this);
            if (function != null)
                return function.IsInvariant;
            if (!Direct)
                return false;
            if (Optimization.Optimizer.FunctionManager.Exists(Function.Replace("()", "")))
                return Optimization.Optimizer.FunctionManager.IsFunctionInvariant(Function.Replace("()", ""));
            return false;
        }
        private Type GetDefaultReturnType()
        {
            IRCodePart.IRFunction function = Block?.CodePart?.GetFunction(this);
            if (function != null)
                return function.Returns.Type;
            if (Optimization.Optimizer.FunctionManager.Exists(Function.Replace("()", "")))
                return Optimization.Optimizer.FunctionManager.FunctionReturnType(Function.Replace("()", ""));
            if (!Direct && IndirectMethod is IRSuffixGetMethod suffixGetMethod)
                return suffixGetMethod.Type;
            return typeof(Encapsulation.Structure);
        }
        public IInterimOperand Clone(BasicBlock block)
            => new IRCall(block, new OpcodeCall(Function)
            {
                Direct = Direct,
                SourceLine = SourceLine,
                SourceColumn = SourceColumn
            }, EmitArgMarker, Arguments);
        public override IEnumerable<Opcode> EmitOpcodes()
        {
            if (EmitArgMarker)
            {
                if (IndirectMethod != null)
                    foreach (Opcode opcode in IndirectMethod.EmitOpcodes())
                        yield return opcode;
                yield return new OpcodePush(new Execution.KOSArgMarkerType());
            }
            foreach (IInterimOperand argument in Arguments)
            {
                foreach (Opcode opcode in argument.EmitOpcodes())
                    yield return opcode;
            }
            yield return SetSourceLocation(new OpcodeCall(Function));
        }

        public IRCall(BasicBlock block, OpcodeCall opcode, bool emitArgMarker, IInterimOperand argument) : this(block, opcode, emitArgMarker)
        {
            Arguments.Add(argument);
        }
        public IRCall(BasicBlock block, OpcodeCall opcode, bool emitArgMarker, IEnumerable<IInterimOperand> arguments) : this(block, opcode, emitArgMarker)
        {
            Arguments.AddRange(arguments);
        }
        public IRCall(BasicBlock block, OpcodeCall opcode, bool emitArgMarker, params IInterimOperand[] arguments) : this(block, opcode, emitArgMarker)
        {
            Arguments.AddRange(arguments);
        }
        public override string ToString()
            => string.Format("{{call {0}({1})}}", Function.Trim('(', ')'), string.Join(",", Arguments.Select(a => a.ToString())));
        public bool Equals(IInterimOperand other)
            => (other is IRCall call &&
                string.Equals(Function.Replace("()", ""), call.Function.Replace("()", ""), StringComparison.OrdinalIgnoreCase) &&
                Arguments.SequenceEqual(call.Arguments)) ||
                (IsInvariant &&
                other is IEvaluatableToConstant evaluatableToConstant &&
                evaluatableToConstant.IsInvariant &&
                Evaluate().Equals(evaluatableToConstant.Evaluate()));
        public override bool Equals(object obj)
            => obj is IRCall call &&
                string.Equals(Function.Replace("()", ""), call.Function.Replace("()", ""), StringComparison.OrdinalIgnoreCase) &&
                Arguments.SequenceEqual(call.Arguments);
        public override int GetHashCode()
            => Function.ToLower().GetHashCode();

        public InterimConstantValue Evaluate()
        {
            if (!IsInvariant)
                throw new InvalidOperationException();

            IRCodePart.IRFunction function = Block?.CodePart?.GetFunction(this);
            if (function != null)
                return function.Returns.Evaluate();

            string functionName = Function.Replace("()", "");
            Optimization.InterimCPU interimCPU = Optimization.Optimizer.InterimCPU;
            interimCPU.Boot();  // Clear the stack out of caution.
            interimCPU.PushArgumentStack(new Execution.KOSArgMarkerType());
            foreach (IInterimOperand arg in Arguments)
            {
                object argValue = (arg as IEvaluatableToConstant)?.Evaluate().Value
                    ?? throw new ArgumentNullException(arg.ToString());
                interimCPU.PushArgumentStack(argValue);
            }
            Optimization.Optimizer.FunctionManager.CallFunction(functionName);
            return new InterimConstantValue(interimCPU.PopValueArgument(), this);
        }
    }
    public class IRReturn : SingleOperandInstruction
    {
        public override bool IsInvariant => Value.IsInvariant;
        public IInterimOperand Value { get => operand; set => operand = value; }
        public short Depth { get; internal set; }
        public IRReturn(BasicBlock block, short depth, OpcodeReturn opcode) : base(opcode, block)
            => Depth = depth;
        public override IEnumerable<Opcode> EmitOpcodes()
        {
            if (Value != null)
                foreach (Opcode opcode in Value.EmitOpcodes())
                    yield return opcode;
            else
                yield return SetSourceLocation(new OpcodePush(null));
            yield return SetSourceLocation(new OpcodeReturn(Depth));
        }
        public override string ToString()
            => string.Format("{{return {0} deep}}", Depth);
        public override bool Equals(object obj)
            => obj is IRReturn ret &&
                Value.Equals(ret.Value);
        public override int GetHashCode()
            => Value.GetHashCode();
    }
}
