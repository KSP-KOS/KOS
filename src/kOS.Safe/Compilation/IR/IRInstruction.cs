using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Exceptions;

namespace kOS.Safe.Compilation.IR
{
    public abstract class IRInstruction
    {
        public BasicBlock Block { get; set; }
        public short SourceLine { get; private set; }   // line number in the source code that this was compiled from.
        public short SourceColumn { get; private set; } // column number of the token nearest the cause of this Opcode.

        /// <summary>
        /// Gets a value indicating whether this instance is invariant.
        /// That is, if the effects and result of the operation can
        /// be known at compile time.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is invariant; otherwise, <c>false</c>.
        /// </value>
        public abstract bool IsInvariant { get; }
        public IRInstruction Clone(BasicBlock block, bool maintainSSAReferences = false)
        {
            IRInstruction newInstruction = Clone_Internal(block, maintainSSAReferences);
            if (Block.CodePart.ReachableVariables.TryGetValue(this, out HashSet<IInterimVariableReference> value))
                block.CodePart.ReachableVariables.Add(newInstruction, value);
            return newInstruction;
        }
        protected abstract IRInstruction Clone_Internal(BasicBlock block, bool maintainSSAReferences);

        public abstract IEnumerable<Opcode> EmitOpcodes();
        protected IRInstruction(Opcode originalOpcode, BasicBlock block)
        {
            SourceLine = originalOpcode?.SourceLine ?? -1;
            SourceColumn = originalOpcode?.SourceColumn ?? -1;
            Block = block;
        }
        protected IRInstruction(IRInstruction from, BasicBlock block)
        {
            SourceLine = from.SourceLine;
            SourceColumn = from.SourceColumn;
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
        public static IEqualityComparer<IRInstruction> ReferenceEqualityComparer => InstructionReferenceEqualityComparer.Instance;

        private class InstructionReferenceEqualityComparer : IEqualityComparer<IRInstruction>
        {
            public static InstructionReferenceEqualityComparer Instance { get; } =
                new InstructionReferenceEqualityComparer();
            public bool Equals(IRInstruction x, IRInstruction y)
                => x == y;
            public int GetHashCode(IRInstruction obj)
                => obj.GetHashCode();
        }
    }

    public abstract class SingleOperandInstruction : IRInstruction, ISingleOperandInstruction
    {
        protected IInterimOperand operand;

        protected SingleOperandInstruction(Opcode originalOpcode, BasicBlock block) : base(originalOpcode, block) { }
        protected SingleOperandInstruction(IRInstruction cloneFrom, BasicBlock block) : base(cloneFrom, block) { }

        IInterimOperand ISingleOperandInstruction.Operand { get => operand; set => operand = value; }

        public void ForEachOperand(Action<IInterimOperand> action)
            => action(operand);

        public void MutateEachOperand(Func<IInterimOperand, IInterimOperand> mutateFunc)
            => operand = mutateFunc(operand);

        public bool AnyOperand(Func<IInterimOperand, bool> predicate)
            => predicate(operand);

        public bool AllOperands(Func<IInterimOperand, bool> predicate)
            => predicate(operand);
    }
    public abstract class MultipleOperandInstruction : IRInstruction, IMultipleOperandInstruction
    {
        protected MultipleOperandInstruction(Opcode originalOpcode, BasicBlock block) : base(originalOpcode, block) { }
        protected MultipleOperandInstruction(IRInstruction cloneFrom, BasicBlock block) : base(cloneFrom, block) { }

        public abstract IEnumerable<IInterimOperand> Operands { get; }
        public abstract int OperandCount { get; }
        /// <summary>
        /// Allows replacing operands from a common function.
        /// The ordering should be the same as the order in which
        /// operands are pushed to the stack, beginning at 0.
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

        public bool AnyOperand(Func<IInterimOperand, bool> predicate)
        {
            for (int i = 0; i < OperandCount; i++)
                if (predicate(this[i]))
                    return true;
            return false;
        }

        public bool AllOperands(Func<IInterimOperand, bool> predicate)
        {
            for (int i = 0; i < OperandCount; i++)
                if (!predicate(this[i]))
                    return false;
            return true;
        }
    }

    public class IRAssign : SingleOperandInstruction, IActionInstruction
    {
        public enum StoreScope
        {
            Ambivalent,
            Local,
            Global
        }
        public override bool IsInvariant => Value.IsInvariant;
        public bool IsInert { get; set; }
        public SSASetDefinition Target { get; set; }
        public IInterimOperand Value { get => operand; set => operand = value; }
        public StoreScope Scope { get; set; } = StoreScope.Ambivalent;
        public bool AssertExists { get; set; } = false;

        public IRAssign(BasicBlock block, OpcodeIdentifierBase opcode, IInterimOperand value) : base(opcode, block)
        {
            Value = value;
            Target = new SSASetDefinition(opcode.Identifier, this);
        }
        protected IRAssign(BasicBlock block, IRAssign cloneFrom, bool maintainSSAReferences) : base(cloneFrom, block)
        {
            Value = cloneFrom.Value.Clone(block, maintainSSAReferences);
            Target = new SSASetDefinition(cloneFrom.Target.Name, this);
            IsInert = cloneFrom.IsInert;
            Scope = cloneFrom.Scope;
            AssertExists = cloneFrom.AssertExists;
        }
        protected override IRInstruction Clone_Internal(BasicBlock block, bool maintainSSAReferences = false)
            => new IRAssign(block, this, maintainSSAReferences);
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

    public class IREval : IRUnaryOp, IResultingInstruction, IEvaluatableToConstant
    {
        public bool BarewordOkay { get; } = false;
        public override bool IsInvariant => false;
        public override Type Type => Operand is InterimResolvedReference resolvedReference ? resolvedReference.Type : typeof(Encapsulation.Structure);
        public IREval(BasicBlock block, OpcodeEval operation, IInterimOperand operand) : base(block, operation, operand)
        {
            BarewordOkay = operation.BarewordOkay;
        }

        protected IREval(BasicBlock block, IREval cloneFrom, bool maintainSSAReferences) : base(block, cloneFrom, maintainSSAReferences)
        {
            BarewordOkay = cloneFrom.BarewordOkay;
        }

        protected override IRInstruction Clone_Internal(BasicBlock block, bool maintainSSAReferences = false)
            => new IREval(block ?? Block, this, maintainSSAReferences);
        IInterimOperand IInterimOperand.Clone(BasicBlock block, bool maintainSSAReferences)
            => (IInterimOperand)Clone(block, maintainSSAReferences);

        public override bool Equals(IInterimOperand other)
            => other == this;
        public override bool Equals(object obj)
            => obj == this;
        public override int GetHashCode()
            => base.GetHashCode();
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
                return MetaCalculator.GetResultType(Left.Type, Right.Type, Operation);
            }
        }
        public ushort OpcodeCount
        {
            get
            {
                ushort result = 1;
                if (Left is IResultingInstruction left)
                    result += left.OpcodeCount;
                else
                    result += 1;
                if (Right is IResultingInstruction right)
                    result += right.OpcodeCount;
                else
                    result += 1;
                return result;
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
                switch (Operation)
                {
                    case OpcodeMathSubtract _:
                    case OpcodeMathDivide _:
                    case OpcodeMathPower _:
                    case OpcodeCompareGT _:
                    case OpcodeCompareGTE _:
                    case OpcodeCompareLT _:
                    case OpcodeCompareLTE _:
                        if (IRParameter.IsOrContainsParameter(Left) || IRParameter.IsOrContainsParameter(Right))
                            return false;
                        break;
                }
                return MetaCalculator.IsCommutative(Left.Type, Right.Type, Operation);
            }
        }
        public bool IsReversible
        {
            get
            {
                if (Left.Type == null || Right.Type == null)
                    return false;
                if (IRParameter.IsOrContainsParameter(Left) || IRParameter.IsOrContainsParameter(Right))
                    return false;
                return MetaCalculator.IsReversible(Left.Type, Right.Type, Operation);
            }
        }

        public IRBinaryOp(BasicBlock block, BinaryOpcode operation, IInterimOperand left, IInterimOperand right) : base(operation, block)
        {
            Operation = operation;
            Left = left;
            Right = right;
        }
        protected IRBinaryOp(BasicBlock block, IRBinaryOp cloneFrom, bool maintainSSAReferences) : base(cloneFrom, block)
        {
            Operation = cloneFrom.CloneOperation();
            Left = cloneFrom.Left.Clone(block, maintainSSAReferences);
            Right = cloneFrom.Right.Clone(block, maintainSSAReferences);
        }
        public bool SwapOperands()
        {
            if (!IsCommutative)
                return false;

            if (Operation is OpcodeMathSubtract)
            {
                if (!MetaCalculator.IsNegatable(Left.Type, Right.Type))
                    return false;
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

        protected override IRInstruction Clone_Internal(BasicBlock block, bool maintainSSAReferences = false)
            => new IRBinaryOp(block ?? Block, this, maintainSSAReferences);
        IInterimOperand IInterimOperand.Clone(BasicBlock block, bool maintainSSAReferences)
            => (IInterimOperand)Clone(block, maintainSSAReferences);
        private BinaryOpcode CloneOperation()
        {
            switch (Operation)
            {
                case OpcodeMathAdd _:
                    return new OpcodeMathAdd();
                case OpcodeMathSubtract _:
                    return new OpcodeMathSubtract();
                case OpcodeMathMultiply _:
                    return new OpcodeMathMultiply();
                case OpcodeMathDivide _:
                    return new OpcodeMathDivide();
                case OpcodeMathPower _:
                    return new OpcodeMathPower();
                case OpcodeCompareEqual _:
                    return new OpcodeCompareEqual();
                case OpcodeCompareNE _:
                    return new OpcodeCompareNE();
                case OpcodeCompareGT _:
                    return new OpcodeCompareGT();
                case OpcodeCompareLT _:
                    return new OpcodeCompareLT();
                case OpcodeCompareGTE _:
                    return new OpcodeCompareGTE();
                case OpcodeCompareLTE _:
                    return new OpcodeCompareLTE();
                default:
                    throw new NotImplementedException();
            }
        }

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
            => Operation.GetType().GetHashCode();

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

        public IRBinaryOp Reverse(IInterimOperand left, IInterimOperand right)
        {
            if (!IsReversible)
                throw new InvalidOperationException();

            switch (Operation)
            {
                case OpcodeMathAdd _:
                    return new IRBinaryOp(Block, new OpcodeMathSubtract() { SourceLine = SourceLine, SourceColumn = SourceColumn }, left, right);
                case OpcodeMathSubtract _:
                    return new IRBinaryOp(Block, new OpcodeMathAdd() { SourceLine = SourceLine, SourceColumn = SourceColumn }, left, right);
                case OpcodeMathMultiply _:
                    return new IRBinaryOp(Block, new OpcodeMathDivide() { SourceLine = SourceLine, SourceColumn = SourceColumn }, left, right);
                case OpcodeMathDivide _:
                    return new IRBinaryOp(Block, new OpcodeMathMultiply() { SourceLine = SourceLine, SourceColumn = SourceColumn }, left, right);
                case OpcodeMathPower _:
                    throw new InvalidOperationException();
                case OpcodeCompareEqual _:
                case OpcodeCompareNE _:
                case OpcodeCompareGT _:
                case OpcodeCompareGTE _:
                case OpcodeCompareLT _:
                case OpcodeCompareLTE _:
                    throw new InvalidOperationException();
                default:
                    throw new NotImplementedException();
            }
        }
    }
    public class IRUnaryOp : SingleOperandInstruction, IResultingInstruction
    {
        public override bool IsInvariant => Operand.IsInvariant && !(Operation is OpcodeExists);
        public Opcode Operation { get; }
        public IInterimOperand Operand { get => operand; set => operand = value; }
        public virtual Type Type
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
        public ushort OpcodeCount
        {
            get
            {
                ushort result = 1;
                if (Operand is IResultingInstruction resulting)
                    result += resulting.OpcodeCount;
                else
                    result += 1;
                return result;
            }
        }

        public IRUnaryOp(BasicBlock block, Opcode operation, IInterimOperand operand) : base(operation, block)
        {
            Operation = operation;
            Operand = operand;
        }
        protected IRUnaryOp(BasicBlock block, IRUnaryOp cloneFrom, bool maintainSSAReferences) : base(cloneFrom, block)
        {
            Operation = SetSourceLocation(cloneFrom.CloneOperation());
            Operand = cloneFrom.Operand.Clone(block, maintainSSAReferences);
        }

        protected override IRInstruction Clone_Internal(BasicBlock block, bool maintainSSAReferences = false)
            => new IRUnaryOp(block ?? Block, this, maintainSSAReferences);
        IInterimOperand IInterimOperand.Clone(BasicBlock block, bool maintainSSAReferences)
            => (IInterimOperand)Clone(block, maintainSSAReferences);
        private Opcode CloneOperation()
        {
            switch (Operation)
            {
                case OpcodeExists _:
                    return new OpcodeExists();
                case OpcodeLogicNot _:
                    return new OpcodeLogicNot(); 
                case OpcodeLogicToBool _:
                    return new OpcodeLogicToBool();
                case OpcodeMathNegate _:
                    return new OpcodeMathNegate();
                case OpcodeEval _:
                    return new OpcodeEval();
                default:
                    throw new NotImplementedException();
            }
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
        public virtual bool Equals(IInterimOperand other)
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
            => (Operation.GetType(), Operand).GetHashCode();

        public virtual InterimConstantValue Evaluate()
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
    public class IRNoStackInstruction : IRInstruction, IActionInstruction
    {
        public override bool IsInvariant => true;
        public bool IsInert { get; } = false;
        public Opcode Operation { get; }
        public IRNoStackInstruction(BasicBlock block, Opcode opcode) : base(opcode, block)
            => Operation = opcode;
        public IRNoStackInstruction(BasicBlock block, Opcode opcode, bool isInert) : this(block, opcode)
            => IsInert = isInert;
        protected IRNoStackInstruction(BasicBlock block, IRNoStackInstruction cloneFrom) : base(cloneFrom, block)
        {
            IsInert = cloneFrom.IsInert;
            switch (cloneFrom.Operation)
            {
                case OpcodeEOF _:
                    Operation = new OpcodeEOF();
                    break;
                case OpcodeEOP _:
                    Operation = new OpcodeEOP();
                    break;
                case OpcodeNOP _:
                    Operation = new OpcodeNOP();
                    break;
                case OpcodeBogus _:
                    Operation = new OpcodeBogus();
                    break;
                case OpcodeArgBottom _:
                    Operation = new OpcodeArgBottom();
                    break;
                case OpcodePushScope pushScope:
                    Operation = new OpcodePushScope(pushScope.ScopeId, pushScope.ParentScopeId);
                    break;
                case OpcodePopScope popScope:
                    Operation = new OpcodePopScope(popScope.NumLevels);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        protected override IRInstruction Clone_Internal(BasicBlock block, bool maintainSSAReferences = false)
            => new IRNoStackInstruction(block, this);
        public override IEnumerable<Opcode> EmitOpcodes()
        {
            Operation.Label = string.Empty;
            yield return Operation;
        }
        public override string ToString()
            => Operation.ToString();
    }
    public class IRUnaryConsumer : SingleOperandInstruction, IActionInstruction
    {
        private readonly bool operationHasSideEffects;
        public override bool IsInvariant => Operand.IsInvariant;
        public bool IsInert => !operationHasSideEffects;
        public Opcode Operation { get; }
        public IInterimOperand Operand { get => operand; set => operand = value; }
        public IRUnaryConsumer(BasicBlock block, Opcode opcode, IInterimOperand operand, bool sideEffects = false) : base(opcode, block)
        {
            Operation = opcode;
            Operand = operand;
            operationHasSideEffects = sideEffects;
        }
        protected IRUnaryConsumer(BasicBlock block, IRUnaryConsumer cloneFrom, bool maintainSSAReferences) : base(cloneFrom, block)
        {
            Operand = cloneFrom.Operand.Clone(block, maintainSSAReferences);
            operationHasSideEffects = cloneFrom.operationHasSideEffects;
            switch (cloneFrom.Operation)
            {
                case OpcodeAddTrigger addTrigger:
                    Operation = new OpcodeAddTrigger(addTrigger.Unique, (Execution.InterruptPriority)addTrigger.Priority);
                    break;
                case OpcodeRemoveTrigger _:
                    Operation = new OpcodeRemoveTrigger();
                    break;
                case OpcodeWait _:
                    Operation = new OpcodeWait();
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        protected override IRInstruction Clone_Internal(BasicBlock block, bool maintainSSAReferences = false)
            => new IRUnaryConsumer(block, this, maintainSSAReferences);
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
    public class IRUnset : IRUnaryConsumer, IActionInstruction
    {
        public override bool IsInvariant => Target != null;
        public SSASetDefinition Target { get; set; }
        public bool IsExecutable => Block.IsExecutable;
        public IRUnset(BasicBlock block, OpcodeUnset opcode, IInterimOperand operand) : base(block, opcode, operand, true)
        {
            if (operand.IsInvariant)
            {
                string name = (string)(operand as IEvaluatableToConstant).Evaluate().Value;
                Target = new SSASetDefinition(name, this);
            }
        }
    }
    public class IRPop : SingleOperandInstruction, IActionInstruction
    {
        public override bool IsInvariant => Value.IsInvariant;
        public bool IsInert => true;
        public IInterimOperand Value { get => operand; set => operand = value; }
        public IRPop(BasicBlock block, IInterimOperand value, OpcodePop opcode) : base(opcode, block)
            => Value = value;
        protected IRPop(BasicBlock block, IRPop cloneFrom, bool maintainSSAReferences = false) : base(cloneFrom, block)
        {
            Value = cloneFrom.Value.Clone(block, maintainSSAReferences);
        }
        protected override IRInstruction Clone_Internal(BasicBlock block, bool maintainSSAReferences = false)
            => new IRPop(block, this, maintainSSAReferences);
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
        public ushort OpcodeCount => 1;

        public IRNonVarPush(BasicBlock block, Opcode opcode) : base(opcode, block)
        {
            Operation = opcode;
        }
        protected IRNonVarPush(BasicBlock block, IRNonVarPush cloneFrom) : base(cloneFrom, block)
        {
            switch (cloneFrom.Operation)
            {
                case OpcodeTestArgBottom _:
                    Operation = new OpcodeTestArgBottom();
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        protected override IRInstruction Clone_Internal(BasicBlock block, bool _)
            => new IRNonVarPush(block ?? Block, CloneOperation());
        IInterimOperand IInterimOperand.Clone(BasicBlock block, bool maintainSSAReferences)
            => (IInterimOperand)Clone(block, maintainSSAReferences);
        private Opcode CloneOperation()
        {
            switch (Operation)
            {
                case OpcodeTestArgBottom _:
                    return new OpcodeTestArgBottom()
                    {
                        SourceLine = SourceLine,
                        SourceColumn = SourceColumn
                    };
                default:
                    throw new NotImplementedException();
            }
        }
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
        public ushort OpcodeCount
        {
            get
            {
                ushort result = 1;
                if (Object is IResultingInstruction obj)
                    result += obj.OpcodeCount;
                else
                    result += 1;
                return result;
            }
        }
        public IRSuffixGet(BasicBlock block, IInterimOperand obj, OpcodeGetMember opcodeGetMember) : base(opcodeGetMember, block)
        {
            Object = obj;
            Suffix = opcodeGetMember.Identifier;
        }
        protected IRSuffixGet(BasicBlock block, IRSuffixGet cloneFrom, bool maintainSSAReferences) : base(cloneFrom, block)
        {
            Object = cloneFrom.Object.Clone(block, maintainSSAReferences);
            Suffix = cloneFrom.Suffix;
        }
        protected override IRInstruction Clone_Internal(BasicBlock block, bool maintainSSAReferences = false)
            => new IRSuffixGet(block ?? Block, this, maintainSSAReferences);
        IInterimOperand IInterimOperand.Clone(BasicBlock block, bool maintainSSAReferences)
            => (IInterimOperand)Clone(block, maintainSSAReferences);
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
    public class IRSuffixGetMethod : IRSuffixGet, IActionInstruction
    {
        // TODO: Consider implementing this.
        public bool IsInert => false;
        public IRSuffixGetMethod(BasicBlock block, IInterimOperand obj, OpcodeGetMethod opcode) : base(block, obj, opcode) { }
        protected IRSuffixGetMethod(BasicBlock block, IRSuffixGet cloneFrom, bool maintainSSAReferences) : base(block, cloneFrom, maintainSSAReferences) { }
        protected override IRInstruction Clone_Internal(BasicBlock block, bool maintainSSAReferences = false)
            => new IRSuffixGetMethod(block, this, maintainSSAReferences);
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
    public class IRSuffixSet : MultipleOperandInstruction, IActionInstruction
    {
        public override bool IsInvariant => IsInert && Object.IsInvariant && Value.IsInvariant;
        public bool IsInert => false;
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
        protected IRSuffixSet(BasicBlock block, IRSuffixSet cloneFrom, bool maintainSSAReferences) : base(cloneFrom, block)
        {
            Object = cloneFrom.Object.Clone(block, maintainSSAReferences);
            Value = cloneFrom.Value.Clone(block, maintainSSAReferences);
            Suffix = cloneFrom.Suffix;
        }
        protected override IRInstruction Clone_Internal(BasicBlock block, bool maintainSSAReferences = false)
            => new IRSuffixSet(block, this, maintainSSAReferences);
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
        public ushort OpcodeCount
        {
            get
            {
                ushort result = 1;
                if (Object is IResultingInstruction obj)
                    result += obj.OpcodeCount;
                else
                    result += 1;
                if (Index is IResultingInstruction index)
                    result += index.OpcodeCount;
                else
                    result += 1;
                return result;
            }
        }
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
        protected IRIndexGet(BasicBlock block, IRIndexGet cloneFrom, bool maintainSSAReferences) : base(cloneFrom, block)
        {
            Object = cloneFrom.Object.Clone(block, maintainSSAReferences);
            Index = cloneFrom.Index.Clone(block, maintainSSAReferences);
        }
        protected override IRInstruction Clone_Internal(BasicBlock block, bool maintainSSAReferences = false)
            => new IRIndexGet(block ?? Block, this, maintainSSAReferences);
        IInterimOperand IInterimOperand.Clone(BasicBlock block, bool maintainSSAReferences)
            => (IInterimOperand)Clone(block, maintainSSAReferences);
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
    public class IRIndexSet : MultipleOperandInstruction, IActionInstruction
    {
        public override bool IsInvariant => IsInert && Object.IsInvariant && Index.IsInvariant && Value.IsInvariant;
        public bool IsInert => false;
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
        protected IRIndexSet(BasicBlock block, IRIndexSet cloneFrom, bool maintainSSAReferences) : base(cloneFrom, block)
        {
            Object = cloneFrom.Object.Clone(block, maintainSSAReferences);
            Index = cloneFrom.Index.Clone(block, maintainSSAReferences);
            Value = cloneFrom.Value.Clone(block, maintainSSAReferences);
        }
        protected override IRInstruction Clone_Internal(BasicBlock block, bool maintainSSAReferences = false)
            => new IRIndexSet(block, this, maintainSSAReferences);
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
    
    public class IRCall : MultipleOperandInstruction, IResultingInstruction, IActionInstruction
    {
        private IInterimOperand targetFunction;

        public override bool IsInvariant => IsSelfInvariant && IsInert && Arguments.All(a => a.IsInvariant);
        public string Function { get; set; }
        public List<IInterimOperand> Arguments { get; } = new List<IInterimOperand>();
        public override IEnumerable<IInterimOperand> Operands => Arguments;
        public override int OperandCount => Arguments.Count;
        public Type Type
        {
            get
            {
                switch (targetFunction)
                {
                    case IRSuffixGet _:
                    case InterimUserFunction _:
                    case InterimBuiltInFunction _:
                    case IRParameter _:
                        return targetFunction.Type;
                    case null:
                        string function = Function.Replace("()", "");
                        if (Optimization.Optimizer.FunctionManager.Exists(function))
                            return Optimization.Optimizer.FunctionManager.FunctionReturnType(function);
                        return typeof(Encapsulation.Structure);
                    default:
                        throw new NotImplementedException();
                }
            }
        }
        public ushort OpcodeCount
        {
            get
            {
                ushort result = 2;
                if (TargetMethod is IResultingInstruction method)
                    result += method.OpcodeCount;
                else
                    result += 1;
                foreach (IInterimOperand argument in Arguments)
                    if (argument is IResultingInstruction arg)
                        result += arg.OpcodeCount;
                    else result += 1;
                if (targetFunction is InterimUserFunction userFunc && !userFunc.IsRecursive)
                    result += (ushort)Optimization.Passes.FunctionInlining.CalculateFunctionLength(userFunc.Function);
                return result;
            }
        }
        protected override IInterimOperand this[int index]
        {
            get => Arguments[index];
            set => Arguments[index] = value;
        }
        public IInterimOperand TargetMethod
        {
            get => targetFunction;
            internal set
            {
                if (Direct)
                {
                    if (!(value is InterimBuiltInFunction) &&
                        value is InterimUserFunction userFunc_ &&
                        userFunc_.VariableReference == null &&
                        userFunc_.Function == null)
                        throw new InvalidOperationException("Cannot use an indirect method on a direct call.");
                }

                if (targetFunction is InterimUserFunction targetUserFunc)
                    targetUserFunc.Function?.CallSites.Remove(this);
                if (value is InterimUserFunction userFunc)
                {
                    if (userFunc.Function == null)
                    {
                        Block.CodeComponent.UnresolvedCallSites.Add(this);
                        Block.CodePart.UnresolvedCallSites.Add(this);
                    }
                    else
                    {
                        userFunc.Function.CallSites.Add(this);
                        Block.CodeComponent.UnresolvedCallSites.Remove(this);
                        Block.CodePart.UnresolvedCallSites.Remove(this);
                    }
                }
                else
                {
                    Block.CodeComponent.UnresolvedCallSites.Remove(this);
                    Block.CodePart.UnresolvedCallSites.Remove(this);
                }
                targetFunction = value;
            }
        }
        public bool Direct { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="IRCall"/>
        /// has been provided with all arguments and an argument marker.
        /// </summary>
        public bool Closed { get; set; }
        public bool EmitArgMarker => Closed || Arguments.Where(arg => arg is IRParameter).Cast<IRParameter>().All(IRParameter.IsSetResolvable);
        private IRCall(BasicBlock block, OpcodeCall opcode, bool argMarkerProvided) : base(opcode, block)
        {
            Direct = opcode.Direct;
            Closed = argMarkerProvided;
            Function = (string)opcode.Destination;
        }
        protected IRCall(BasicBlock block, IRCall cloneFrom, bool maintainSSAReferences) : base(cloneFrom, block)
        {
            Direct = cloneFrom.Direct;
            Closed = cloneFrom.Closed;
            Function = cloneFrom.Function;
            TargetMethod = cloneFrom.TargetMethod;
            Arguments.AddRange(cloneFrom.Arguments.Select(arg => arg.Clone(block, maintainSSAReferences)));
        }
        protected bool IsSelfInvariant
        {
            get
            {
                switch (targetFunction)
                {
                    // TODO: Consider that some suffix methods may actually be known at compile time.
                    case IRSuffixGet _:
                        return false;
                    case InterimBuiltInFunction _:
                    case InterimUserFunction _:
                    case IRParameter _:
                        return targetFunction.IsInvariant;
                    default:
                        return false;
                }
            }
        }
        public virtual bool IsInert
        {
            get
            {
                switch (targetFunction)
                {
                    // TODO: Consider that some suffix methods may actually be inert.
                    case IRSuffixGet _:
                        return false;
                    case InterimBuiltInFunction builtIn:
                        return builtIn.IsInert;
                    case InterimUserFunction userFunc:
                        return userFunc.IsInert;
                    case IRParameter _:
                        return false;
                    default:
                        return false;
                }
            }
        }
        protected override IRInstruction Clone_Internal(BasicBlock block, bool maintainSSAReferences = false)
            => new IRCall(block ?? Block, this, maintainSSAReferences);
        IInterimOperand IInterimOperand.Clone(BasicBlock block, bool maintainSSAReferences)
            => (IInterimOperand)Clone(block, maintainSSAReferences);
        public override IEnumerable<Opcode> EmitOpcodes()
        {
            if (EmitArgMarker)
            {
                if (TargetMethod != null)
                    foreach (Opcode opcode in TargetMethod.EmitOpcodes())
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

        public IRCall(BasicBlock block, OpcodeCall opcode, bool argMarkerProvided, IInterimOperand argument) : this(block, opcode, argMarkerProvided)
        {
            Arguments.Add(argument);
        }
        public IRCall(BasicBlock block, OpcodeCall opcode, bool argMarkerProvided, IEnumerable<IInterimOperand> arguments) : this(block, opcode, argMarkerProvided)
        {
            Arguments.AddRange(arguments);
        }
        public IRCall(BasicBlock block, OpcodeCall opcode, params IInterimOperand[] arguments) : this(block, opcode, true)
        {
            Arguments.AddRange(arguments);
        }
        public override string ToString()
            => string.Format("{{call {0}({1})}}", Function.Trim('(', ')'), string.Join(",", Arguments.Select(a => a.ToString())));
        public bool Equals(IInterimOperand other)
            => other == this ||
                (IsInert &&  // If the call is not inert, the underlying state is affected, which makes any similar call non-equal.
                other is IRCall call &&
                string.Equals(Function.Replace("()", ""), call.Function.Replace("()", ""), StringComparison.OrdinalIgnoreCase) &&
                Arguments.SequenceEqual(call.Arguments)) ||
                (IsInvariant && // If the call is invariant, it may be equal to a constant.
                other is IEvaluatableToConstant evaluatableToConstant &&
                evaluatableToConstant.IsInvariant &&
                (Evaluate()?.Equals(evaluatableToConstant.Evaluate()) ?? false));
        public override bool Equals(object obj)
            => obj is IInterimOperand operand &&
            Equals(operand);
        public override int GetHashCode()
            => Function.ToLower().GetHashCode();

        public InterimConstantValue Evaluate()
        {
            if (!IsInvariant)
                throw new InvalidOperationException();

            switch (targetFunction)
            {
                case InterimUserFunction userFunction:
                    return userFunction.Function.Returns.Evaluate();
                case InterimBuiltInFunction builtIn:
                    return builtIn.Evaluate(Arguments);
                default:
                    throw new NotImplementedException();
            }
        }
    }

    public class IRRun : IRCall, IInterimOperand
    {
        public override bool IsInert => false;
        public override bool IsInvariant => false;
        public string DestinationLabel { get; }

        public IRRun(BasicBlock block, OpcodeCall opcode, IEnumerable<IInterimOperand> arguments) : base(block, opcode, true, arguments)
        {
            DestinationLabel = opcode.DestinationLabel;
        }

        protected IRRun(BasicBlock block, IRRun cloneFrom, bool maintainSSAReferences) : base(block, cloneFrom, maintainSSAReferences)
        {
            DestinationLabel = cloneFrom.DestinationLabel;
        }

        protected override IRInstruction Clone_Internal(BasicBlock block, bool maintainSSAReferences = false)
            => new IRRun(block, this, maintainSSAReferences);
        IInterimOperand IInterimOperand.Clone(BasicBlock block, bool maintainSSAReferences)
            => (IInterimOperand)Clone(block, maintainSSAReferences);

        public override IEnumerable<Opcode> EmitOpcodes()
        {
            yield return new OpcodePush(new Execution.KOSArgMarkerType());
            foreach (Opcode opcode in base.EmitOpcodes())
                yield return opcode;
        }
        public override int GetHashCode()
            => Arguments.First(a => !(a is InterimConstantValue constant && constant.Value is Encapsulation.BooleanValue)).GetHashCode();
        public override string ToString()
            => string.Format("{{run {0}}}", string.Join(",", Arguments.Reverse<IInterimOperand>().Select(a => a.ToString())));
    }

    public class IRReturn : SingleOperandInstruction
    {
        public override bool IsInvariant => Value.IsInvariant;
        public IInterimOperand Value { get => operand; set => operand = value; }
        public short Depth { get; internal set; }
        public IRReturn(BasicBlock block, short depth, OpcodeReturn opcode) : base(opcode, block)
            => Depth = depth;
        protected IRReturn(BasicBlock block, IRReturn cloneFrom, bool maintainSSAReferences) : base(cloneFrom, block)
        {
            Depth = cloneFrom.Depth;
            Value = cloneFrom.Value.Clone(block, maintainSSAReferences);
        }
        protected override IRInstruction Clone_Internal(BasicBlock block, bool maintainSSAReferences = false)
            => new IRReturn(block, this, maintainSSAReferences);
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
            => string.Format("{{return {0} deep: {1}}}", Depth, Value);
        public override bool Equals(object obj)
            => obj is IRReturn ret &&
                Value.Equals(ret.Value);
        public override int GetHashCode()
            => Value.GetHashCode();
    }
    public class IRPushStack : SingleOperandInstruction, IActionInstruction, IStackTransferObject
    {
        private readonly HashSet<StackTransferPhi> controllers = new HashSet<StackTransferPhi>();
        private readonly HashSet<IRParameter> references = new HashSet<IRParameter>();
        public IReadOnlyCollection<StackTransferPhi> Controllers => controllers;
        public IReadOnlyCollection<IRParameter> References => references;
        IEnumerable<IStackTransferObject> IStackTransferObject.StackTransferObjects => new IRPushStack[] { this };
        public IInterimOperand Value { get => operand; set => operand = value; }
        public Type Type => Value?.Type ?? typeof(Encapsulation.Structure);
        public override bool IsInvariant => operand.IsInvariant;
        public bool IsInert => false;
        public bool IsResolvable => IRParameter.IsSetResolvable(this);
        public virtual bool IsSelfResolvable => Block != null;

        public IRPushStack(BasicBlock block, IInterimOperand operand) : base((Opcode)null, block)
        {
            Value = operand;
        }
        private IRPushStack(BasicBlock block, IRPushStack cloneFrom, bool maintainSSAReferences) : base(cloneFrom, block)
        {
            Value = cloneFrom.Value?.Clone(block, maintainSSAReferences);
        }
        public static IRPushStack ExternalPush()
            => new IRPushStack(null, (IInterimOperand)null);

        public void AddController(StackTransferPhi phi)
            => controllers.Add(phi);
        public void AddReference(IRParameter reference)
            => references.Add(reference);
        public void RemoveReference(IRParameter reference)
            => references.Remove(reference);

        protected override IRInstruction Clone_Internal(BasicBlock block, bool maintainSSAReferences = false)
            => new IRPushStack(block, this, maintainSSAReferences);
        public override IEnumerable<Opcode> EmitOpcodes()
            => !IsResolvable ? Value.EmitOpcodes() : Enumerable.Empty<Opcode>();
        public override string ToString()
            => !IsResolvable ? $"{{ push {Value} }}" : "{ push nop }";
    }

    public class IRPushStackArgMarker : IRPushStack
    {
        public IRCall Call { get; set; }
        public override bool IsSelfResolvable => Call?.EmitArgMarker ?? false;
        public IRPushStackArgMarker(BasicBlock block, IInterimOperand operand) : base(block, operand)
        {
        }
    }
}
