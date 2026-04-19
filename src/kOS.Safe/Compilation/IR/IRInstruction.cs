using System;
using System.Collections.Generic;
using System.Linq;

namespace kOS.Safe.Compilation.IR
{
    public abstract class IRInstruction
    {
        public short SourceLine { get; private set; }   // line number in the source code that this was compiled from.
        public short SourceColumn { get; private set; } // column number of the token nearest the cause of this Opcode.

        public abstract bool IsInvariant { get; }
        internal abstract IEnumerable<Opcode> EmitOpcode();
        protected IRInstruction(Opcode originalOpcode)
        {
            SourceLine = originalOpcode.SourceLine;
            SourceColumn = originalOpcode.SourceColumn;
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
        protected IRValue operand;

        protected SingleOperandInstruction(Opcode originalOpcode) : base(originalOpcode) { }

        IRValue ISingleOperandInstruction.Operand { get => operand; set => operand = value; }

        public void ForEachOperand(Action<IRValue> action)
            => action(operand);

        public void MutateEachOperand(Func<IRValue, IRValue> mutateFunc)
            => operand = mutateFunc(operand);
    }
    public abstract class MultipleOperandInstruction : IRInstruction, IMultipleOperandInstruction
    {
        protected MultipleOperandInstruction(Opcode originalOpcode) : base(originalOpcode) { }

        public abstract IEnumerable<IRValue> Operands { get; }
        public abstract int OperandCount { get; }
        /// <summary>
        /// Allows replacing operands from a common function.
        /// The meaning of the index and ordering are irrelevant,
        /// as long as it covers the range [0, <see cref="OperandCount"/>).
        /// </summary>
        protected abstract IRValue this[int index] { get; set; }

        public void ForEachOperand(Action<IRValue> action)
        {
            foreach (IRValue operand in Operands)
                action(operand);
        }

        public void MutateEachOperand(Func<IRValue, IRValue> mutateFunc)
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
        public IRVariableBase Target { get; set; }
        public IRValue Value { get => operand; set => operand = value; }
        public StoreScope Scope { get; set; } = StoreScope.Ambivalent;
        public bool AssertExists { get; set; } = false;

        public IRAssign(OpcodeIdentifierBase opcode, IRVariableBase target, IRValue value) : base(opcode)
        {
            Target = target;
            Value = value;
            if (target is SSAVariable ssaTarget)
                ssaTarget.AssignedAt = this;
        }
        internal override IEnumerable<Opcode> EmitOpcode()
        {
            if (Value != null)
                foreach (Opcode opcode in Value.EmitPush())
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
        public override bool Equals(object obj)
            => obj is IRAssign assignment &&
                Target.Equals(assignment.Target) &&
                Value.Equals(assignment.Value);
        public override int GetHashCode()
            => Target.GetHashCode();
    }
    public class IRBinaryOp : MultipleOperandInstruction, IResultingInstruction
    {
        private static readonly Type[] commutativeTypes =
        {
            typeof(OpcodeCompareEqual),
            typeof(OpcodeCompareNE),
            typeof(OpcodeCompareGT),
            typeof(OpcodeCompareLT),
            typeof(OpcodeCompareGTE),
            typeof(OpcodeCompareLTE),
            typeof(OpcodeMathAdd),
            typeof(OpcodeMathMultiply)
        };

        public override bool IsInvariant => Left.IsInvariant && Right.IsInvariant;
        public IRValue Result { get; set; }
        public BinaryOpcode Operation { get; set; }
        public IRValue Left { get; set; }
        public IRValue Right { get; set; }
        public override IEnumerable<IRValue> Operands { get { yield return Left; yield return Right; } }
        public override int OperandCount => 2;
        public Type ResultType
        {
            get
            {
                Calculator calculator = Calculator.GetCalculator(Left.ValueType, Right.ValueType);
                switch (Operation)
                {
                    case OpcodeMathAdd _:
                        return calculator.GetAddResultType(Left.ValueType, Right.ValueType);
                    case OpcodeMathSubtract _:
                        return calculator.GetSubtractResultType(Left.ValueType, Right.ValueType);
                    case OpcodeMathMultiply _:
                        return calculator.GetMultiplyResultType(Left.ValueType, Right.ValueType);
                    case OpcodeMathDivide _:
                        return calculator.GetDivideResultType(Left.ValueType, Right.ValueType);
                    case OpcodeMathPower _:
                        return calculator.GetPowerResultType(Left.ValueType, Right.ValueType);
                    case OpcodeCompareEqual _:
                        return calculator.GetEqualResultType(Left.ValueType, Right.ValueType);
                    case OpcodeCompareNE _:
                        return calculator.GetNotEqualResultType(Left.ValueType, Right.ValueType);
                    case OpcodeCompareGT _:
                        return calculator.GetGreaterThanResultType(Left.ValueType, Right.ValueType);
                    case OpcodeCompareLT _:
                        return calculator.GetLessThanResultType(Left.ValueType, Right.ValueType);
                    case OpcodeCompareGTE _:
                        return calculator.GetGreaterThanEqualResultType(Left.ValueType, Right.ValueType);
                    case OpcodeCompareLTE _:
                        return calculator.GetLessThanEqualResultType(Left.ValueType, Right.ValueType);
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        protected override IRValue this[int index]
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
        public bool IsCommutative => commutativeTypes.Contains(Operation.GetType());

        public IRBinaryOp(IRTemp result, BinaryOpcode operation, IRValue left, IRValue right) : base(operation)
        {
            Result = result;
            Operation = operation;
            Left = left;
            Right = right;
        }
        public void SwapOperands()
        {
            if (!IsCommutative)
                return;
                //throw new System.InvalidOperationException($"{this} is not commutative.");
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
        }
        internal override IEnumerable<Opcode> EmitOpcode()
        {
            foreach (Opcode opcode in Left.EmitPush())
                yield return opcode;
            foreach (Opcode opcode in Right.EmitPush())
                yield return opcode;
            Operation.Label = string.Empty;
            yield return SetSourceLocation(Operation);
        }
        public override string ToString()
            => Operation.ToString();
        public override bool Equals(object obj)
        {
            if (obj is IRBinaryOp binaryOp &&
                Operation.GetType() == binaryOp.Operation.GetType())
            {
                return (Left.Equals(binaryOp.Left) && Right.Equals(binaryOp.Right)) ||
                    (IsCommutative && Left.Equals(binaryOp.Right) && Right.Equals(binaryOp.Left));
            }
            return false;
        }
        public override int GetHashCode()
            => Operation.GetHashCode();
    }
    public class IRUnaryOp : SingleOperandInstruction, IResultingInstruction
    {
        public override bool IsInvariant => Operand.IsInvariant;
        public IRValue Result { get; set; }
        public Opcode Operation { get; }
        public IRValue Operand { get => operand; set => operand = value; }
        public Type ResultType
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
                        return Operand.ValueType;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
        public IRUnaryOp(IRTemp result, Opcode operation, IRValue operand) : base(operation)
        {
            Result = result;
            Operation = operation;
            Operand = operand;
        }
        internal override IEnumerable<Opcode> EmitOpcode()
        {
            foreach (Opcode opcode in Operand.EmitPush())
                yield return opcode;
            Operation.Label = string.Empty;
            yield return Operation;
        }
        public override string ToString()
            => Operation.ToString();
        public override bool Equals(object obj)
            => obj is IRUnaryOp unaryOp &&
                Operation.GetType() == unaryOp.Operation.GetType() &&
                Operand.Equals(unaryOp.Operand);
        public override int GetHashCode()
            => Operation.GetHashCode();
    }
    public class IRNoStackInstruction : IRInstruction
    {
        public override bool IsInvariant => false;
        public Opcode Operation { get; }
        public IRNoStackInstruction(Opcode opcode) : base(opcode)
            => Operation = opcode;
        internal override IEnumerable<Opcode> EmitOpcode()
        {
            Operation.Label = string.Empty;
            yield return Operation;
        }
        public override string ToString()
            => Operation.ToString();
        public override bool Equals(object obj)
            => obj is IRNoStackInstruction instruction && Operation.GetType() == instruction.Operation.GetType();
        public override int GetHashCode()
            => Operation.GetHashCode();
    }
    public class IRUnaryConsumer : SingleOperandInstruction
    {
        private readonly bool operationHasSideEffects;
        public override bool IsInvariant => !operationHasSideEffects && Operand.IsInvariant;
        public Opcode Operation { get; }
        public IRValue Operand { get => operand; set => operand = value; }
        public IRUnaryConsumer(Opcode opcode, IRValue operand, bool sideEffects = false) : base(opcode)
        {
            Operation = opcode;
            Operand = operand;
            operationHasSideEffects = sideEffects;
        }
        internal override IEnumerable<Opcode> EmitOpcode()
        {
            foreach (Opcode opcode in Operand.EmitPush())
                yield return opcode;
            Operation.Label = string.Empty;
            yield return Operation;
        }
        public override string ToString()
            => Operation.ToString();
        public override bool Equals(object obj)
            => obj is IRUnaryConsumer unaryConsumer &&
                Operation.GetType() == unaryConsumer.Operation.GetType() &&
                Operand.Equals(unaryConsumer.Operand);
        public override int GetHashCode()
            => Operation.GetHashCode();
    }
    public class IRPop : SingleOperandInstruction
    {
        public override bool IsInvariant => Value.IsInvariant;
        public IRValue Value { get => operand; set => operand = value; }
        public IRPop(IRValue value, OpcodePop opcode) : base(opcode)
            => Value = value;

        internal override IEnumerable<Opcode> EmitOpcode()
        {
            if (Value is IRConstant || (Value is IRVariable && !(Value is IRTemp)))
                yield break;
            foreach (Opcode opcode in Value.EmitPush())
                yield return opcode;
            yield return SetSourceLocation(new OpcodePop());
        }
        public override string ToString()
            => $"{{pop {Value}}}";
        public override bool Equals(object obj)
            => obj is IRPop pop && Value.Equals(pop.Value);
        public override int GetHashCode()
            => Value.GetHashCode();
    }
    public class IRNonVarPush : IRInstruction, IResultingInstruction
    {
        public override bool IsInvariant => false;
        public Opcode Operation { get; }
        public IRValue Result { get; }
        public Type ResultType
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

        public IRNonVarPush(IRValue result, Opcode opcode) : base(opcode)
        {
            Operation = opcode;
            Result = result;
        }
        internal override IEnumerable<Opcode> EmitOpcode()
        {
            Operation.Label = string.Empty;
            yield return Operation;
        }
        public override string ToString()
            => Operation.ToString();
        public override bool Equals(object obj)
            => obj is IRNonVarPush instruction &&
            Operation.GetType() == instruction.Operation.GetType();
        public override int GetHashCode()
            => Operation.GetHashCode();
    }
    public class IRSuffixGet : SingleOperandInstruction, IResultingInstruction
    {
        public override bool IsInvariant => Object.IsInvariant;
        public IRValue Result { get; set; }
        public IRValue Object { get => operand; set => operand = value; }
        public string Suffix { get; set; }
        public Type ResultType => TypeInferencer.GetTypeForSuffix(Object.ValueType, Suffix);
        public IRSuffixGet(IRTemp result, IRValue obj, OpcodeGetMember opcodeGetMember) : base(opcodeGetMember)
        {
            Result = result;
            Object = obj;
            Suffix = opcodeGetMember.Identifier;
        }
        internal override IEnumerable<Opcode> EmitOpcode()
        {
            foreach (Opcode opcode in Object.EmitPush())
                yield return opcode;
            yield return SetSourceLocation(new OpcodeGetMember(Suffix));
        }
        public override string ToString()
            => string.Format("{{gmb \"{0}\"}}", Suffix);
        public override bool Equals(object obj)
            => obj is IRSuffixGet suffixGet &&
            !(suffixGet is IRSuffixGetMethod) &&
            string.Equals(Suffix, suffixGet.Suffix, StringComparison.OrdinalIgnoreCase) &&
            Object == suffixGet.Object;
        public override int GetHashCode()
            => (Object, Suffix).GetHashCode();
    }
    public class IRSuffixGetMethod : IRSuffixGet
    {
        public IRSuffixGetMethod(IRTemp result, IRValue obj, OpcodeGetMethod opcode) : base(result, obj, opcode) { }
        internal override IEnumerable<Opcode> EmitOpcode()
        {
            foreach (Opcode opcode in Object.EmitPush())
                yield return opcode;
            yield return SetSourceLocation(new OpcodeGetMethod(Suffix));
        }
        public override string ToString()
            => string.Format("{{gmet \"{0}\"}}", Suffix);
        public override bool Equals(object obj)
            => obj is IRSuffixGetMethod suffixGet &&
            string.Equals(Suffix, suffixGet.Suffix, StringComparison.OrdinalIgnoreCase) &&
            Object == suffixGet.Object;
        public override int GetHashCode()
            => (Object, Suffix).GetHashCode();
    }
    public class IRSuffixSet : MultipleOperandInstruction
    {
        public override bool IsInvariant => false;
        public IRValue Object { get; set; }
        public IRValue Value { get; set; }
        public override IEnumerable<IRValue> Operands { get { yield return Object; yield return Value; } }
        public override int OperandCount => 2;
        protected override IRValue this[int index]
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
        public IRSuffixSet(IRValue obj, IRValue value, OpcodeSetMember opcodeSetMember) : base(opcodeSetMember)
        {
            Object = obj;
            Value = value;
            Suffix = opcodeSetMember.Identifier;
        }
        internal override IEnumerable<Opcode> EmitOpcode()
        {
            foreach (Opcode opcode in Object.EmitPush())
                yield return opcode;
            foreach (Opcode opcode in Value.EmitPush())
                yield return opcode;
            yield return SetSourceLocation(new OpcodeSetMember(Suffix));
        }
        public override string ToString()
            => string.Format("{{smb \"{0}\"}}", Suffix);
        public override bool Equals(object obj)
            => obj is IRSuffixSet suffixSet &&
                string.Equals(Suffix, suffixSet.Suffix, StringComparison.OrdinalIgnoreCase) &&
                Object.Equals(suffixSet.Object) &&
                Value.Equals(suffixSet.Value);
        public override int GetHashCode()
            => (Object, Suffix).GetHashCode();
    }
    public class IRIndexGet : MultipleOperandInstruction, IResultingInstruction
    {
        public override bool IsInvariant => Object.IsInvariant && Index.IsInvariant;
        public IRValue Result { get; }
        public IRValue Object { get; set; }
        public IRValue Index { get; set; }
        public override IEnumerable<IRValue> Operands { get { yield return Object; yield return Index; } }
        public override int OperandCount => 2;
        public Type ResultType => TypeInferencer.GetTypeForIndex(Object.ValueType);
        protected override IRValue this[int index]
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
        public IRIndexGet(IRTemp result, IRValue obj, IRValue index, OpcodeGetIndex opcode) : base(opcode)
        {
            Result = result;
            Object = obj;
            Index = index;
        }
        internal override IEnumerable<Opcode> EmitOpcode()
        {
            foreach (Opcode opcode in Object.EmitPush())
                yield return opcode;
            foreach (Opcode opcode in Index.EmitPush())
                yield return opcode;
            yield return SetSourceLocation(new OpcodeGetIndex());
        }
        public override string ToString()
            => "{gidx}";
        public override bool Equals(object obj)
            => obj is IRIndexGet indexGet &&
            Object.Equals(indexGet.Object) &&
            Index.Equals(indexGet.Index);
        public override int GetHashCode()
            => Object.GetHashCode();
    }
    public class IRIndexSet : MultipleOperandInstruction
    {
        public override bool IsInvariant => false;
        public IRValue Object { get; set; }
        public IRValue Index { get; set; }
        public IRValue Value { get; set; }
        public override IEnumerable<IRValue> Operands { get { yield return Object; yield return Index; yield return Value; } }
        public override int OperandCount => 3;
        protected override IRValue this[int index]
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
        public IRIndexSet(IRValue obj, IRValue index, IRValue value, OpcodeSetIndex opcode) : base(opcode)
        {
            Object = obj;
            Index = index;
            Value = value;
        }
        internal override IEnumerable<Opcode> EmitOpcode()
        {
            foreach (Opcode opcode in Object.EmitPush())
                yield return opcode;
            foreach (Opcode opcode in Index.EmitPush())
                yield return opcode;
            foreach (Opcode opcode in Value.EmitPush())
                yield return opcode;
            yield return SetSourceLocation(new OpcodeSetIndex());
        }
        public override string ToString()
            => "{sidx}";
        public override bool Equals(object obj)
            => obj is IRIndexSet indexGet &&
            Object.Equals(indexGet.Object) &&
            Index.Equals(indexGet.Index) &&
            Value.Equals(indexGet.Value);
        public override int GetHashCode()
            => Object.GetHashCode();
    }
    public class IRJump : IRInstruction
    {
        public override bool IsInvariant => true;
        public BasicBlock Target { get; set; }
        public IRJump(BasicBlock target, OpcodeBranchJump opcode) : base(opcode)
        {
            Target = target;
        }
        public IRJump(BasicBlock target, short sourceLine, short sourceColumn)
            : this(target, new OpcodeBranchJump() { SourceLine = sourceLine, SourceColumn = sourceColumn }) { }
        internal override IEnumerable<Opcode> EmitOpcode()
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
        public IRValue Distance { get => operand; set => operand = value; }
        public List<BasicBlock> Targets { get; } = new List<BasicBlock>();
        public IRJumpStack(IRValue distance, IEnumerable<BasicBlock> targets, OpcodeJumpStack jumpStack) : base(jumpStack)
        {
            Distance = distance;
            Targets.AddRange(targets);
        }
        internal override IEnumerable<Opcode> EmitOpcode()
        {
            foreach (Opcode opcode in Distance.EmitPush())
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
        public IRValue Condition { get => operand; set => operand = value; }
        public BasicBlock True { get; set; }
        public BasicBlock False { get; set; }
        public bool PreferFalse { get; set; } = false;
        public IRBranch(IRValue condition, BasicBlock onTrue, BasicBlock onFalse, BranchOpcode opcodeBranch) : base(opcodeBranch)
        {
            Condition = condition;
            True = onTrue;
            False = onFalse;
            PreferFalse = opcodeBranch is OpcodeBranchIfFalse;
        }
        internal override IEnumerable<Opcode> EmitOpcode()
        {
            foreach (Opcode opcode in Condition.EmitPush())
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
        private bool isResultTypeInformed = false;
        private Type resultType = null;
        private bool? isFunctionInvariant = null;

        protected static readonly Function.FunctionManager functionManager = new Function.FunctionManager(null);
        public bool IsFunctionInvariant
        {
            get => isFunctionInvariant ?? IsCallInvariant(Function);
            set => isFunctionInvariant = value;
        }
        public override bool IsInvariant => IsFunctionInvariant && Arguments.All(a => a.IsInvariant);
        public IRValue Result { get; set; }
        public string Function { get; }
        public List<IRValue> Arguments { get; } = new List<IRValue>();
        public override IEnumerable<IRValue> Operands => Enumerable.Reverse(Arguments);
        public override int OperandCount => Arguments.Count;
        public Type ResultType
        {
            get => isResultTypeInformed ? resultType : GetDefaultReturnType();
            set
            {
                resultType = value;
                isResultTypeInformed = true;
            }
        }
        protected override IRValue this[int index]
        {
            get => Arguments[index];
            set => Arguments[index] = value;
        }
        public IRValue IndirectMethod { get; internal set; }
        public bool Direct { get; }
        public bool EmitArgMarker { get; set; }
        private IRCall(IRTemp target, OpcodeCall opcode, bool emitArgMarker) : base(opcode)
        {
            Result = target;
            Function = (string)opcode.Destination;
            Direct = opcode.Direct;
            EmitArgMarker = emitArgMarker;
        }
        private bool IsCallInvariant(string functionName)
        {
            if (!Direct)
                return false;
            if (functionManager.Exists(functionName))
            {
                return functionManager.IsFunctionInvariant(functionName);
            }
            return false;

        }
        private Type GetDefaultReturnType()
        {
            if (functionManager.Exists(Function))
                return functionManager.FunctionReturnType(Function);
            if (IndirectMethod is IRTemp tempSuffixCall &&
                tempSuffixCall.Parent is IRSuffixGetMethod suffixGetMethod)
                return suffixGetMethod.ResultType;
            return typeof(Encapsulation.Structure);
        }
        internal override IEnumerable<Opcode> EmitOpcode()
        {
            if (EmitArgMarker)
            {
                if (IndirectMethod != null)
                    foreach (Opcode opcode in IndirectMethod.EmitPush())
                        yield return opcode;
                yield return new OpcodePush(new Execution.KOSArgMarkerType());
            }
            foreach (IRValue argument in Arguments)
            {
                foreach (Opcode opcode in argument.EmitPush())
                    yield return opcode;
            }
            yield return SetSourceLocation(new OpcodeCall(Function));
        }

        public IRCall(IRTemp target, OpcodeCall opcode, bool emitArgMarker, IRValue argument) : this(target, opcode, emitArgMarker)
        {
            Arguments.Add(argument);
        }
        public IRCall(IRTemp target, OpcodeCall opcode, bool emitArgMarker, IEnumerable<IRValue> arguments) : this(target, opcode, emitArgMarker)
        {
            Arguments.AddRange(arguments);
        }
        public IRCall(IRTemp target, OpcodeCall opcode, bool emitArgMarker, params IRValue[] arguments) : this(target, opcode, emitArgMarker)
        {
            Arguments.AddRange(arguments);
        }
        public override string ToString()
            => string.Format("{{call {0}({1})}}", Function.Trim('(', ')'), string.Join(",", Arguments.Select(a => a.ToString())));
        public override bool Equals(object obj)
            => obj is IRCall call &&
                string.Equals(Function.Replace("()", ""), call.Function.Replace("()", ""), StringComparison.OrdinalIgnoreCase) &&
                Arguments.SequenceEqual(call.Arguments);
        public override int GetHashCode()
            => Function.ToLower().GetHashCode();
    }
    public class IRReturn : SingleOperandInstruction
    {
        public override bool IsInvariant => Value.IsInvariant;
        public IRValue Value { get => operand; set => operand = value; }
        public short Depth { get; internal set; }
        public IRReturn(short depth, OpcodeReturn opcode) : base(opcode)
            => Depth = depth;
        internal override IEnumerable<Opcode> EmitOpcode()
        {
            if (Value != null)
                foreach (Opcode opcode in Value.EmitPush())
                    yield return opcode;
            else
                yield return SetSourceLocation(new OpcodePush(null));
            yield return SetSourceLocation(new OpcodeReturn(Depth));
        }
        public override string ToString()
            => string.Format("{return {0} deep}", Depth);
        public override bool Equals(object obj)
            => obj is IRReturn ret &&
                Depth == ret.Depth &&
                Value.Equals(ret.Value);
        public override int GetHashCode()
            => base.GetHashCode();
    }
}
