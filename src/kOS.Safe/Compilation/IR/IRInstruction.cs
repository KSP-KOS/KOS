using System.Collections.Generic;
using System.Linq;

namespace kOS.Safe.Compilation.IR
{
    public abstract class IRInstruction
    {
        public short SourceLine { get; } // line number in the source code that this was compiled from.
        public short SourceColumn { get; }  // column number of the token nearest the cause of this Opcode.

        // Should-be-static
        public abstract bool SideEffects { get; }
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
    }
    public abstract class IRInteractsInstruction : IRInstruction
    {
        public override bool SideEffects { get; }
        protected IRInteractsInstruction(IRValue interactor, Opcode originalOpcode) : base(originalOpcode)
        {
            if (interactor is IRConstant)
            {
                SideEffects = false;
                return;
            }
            switch (interactor.Type)
            {
                case IRValue.ValueType.Value:
                    SideEffects = false;
                    break;
                default:
                    SideEffects = true;
                    break;
            }
        }
    }
    public class IRAssign : IRInstruction, ISingleOperandInstruction
    {
        public enum StoreScope
        {
            Ambivalent,
            Local,
            Global
        }
        public override bool SideEffects => false;
        public string Target { get; set; }
        public IRValue Value { get; set; }
        public StoreScope Scope { get; set; } = StoreScope.Ambivalent;
        public bool AssertExists { get; set; } = false;
        IRValue ISingleOperandInstruction.Operand => Value;

        public IRAssign(OpcodeIdentifierBase opcode, IRValue value) : base(opcode)
        {
            Target = opcode.Identifier;
            Value = value;
        }
        internal override IEnumerable<Opcode> EmitOpcode()
        {
            foreach (Opcode opcode in Value.EmitPush())
                yield return opcode;
            if (AssertExists)
            {
                yield return SetSourceLocation(new OpcodeStoreExist(Target));
                yield break;
            }
            switch (Scope)
            {
                case StoreScope.Local:
                    yield return SetSourceLocation(new OpcodeStoreLocal(Target));
                    yield break;
                case StoreScope.Global:
                    yield return SetSourceLocation(new OpcodeStoreGlobal(Target));
                    yield break;
                default:
                case StoreScope.Ambivalent:
                    yield return SetSourceLocation(new OpcodeStore(Target));
                    yield break;
            }
        }
        public override string ToString()
            => string.Format("{{store {0}}}", Value.ToString());
    }
    public class IRBinaryOp : IRInstruction, IResultingInstruction, IMultipleOperandInstruction
    {
        public override bool SideEffects => false;
        public IRValue Result { get; set; }
        public BinaryOpcode Operation { get; protected set; }
        public IRValue Left { get; set; }
        public IRValue Right { get; set; }
        public IEnumerable<IRValue> Operands { get { yield return Left; yield return Right; } }
        public bool IsCommutative { get; }

        public IRBinaryOp(IRTemp result, BinaryOpcode operation, IRValue left, IRValue right) : base(operation)
        {
            Result = result;
            Operation = operation;
            Left = left;
            Right = right;
            IsCommutative = (operation is OpcodeCompareEqual) ||
                (operation is OpcodeCompareNE) ||
                (operation is OpcodeCompareGT) ||
                (operation is OpcodeCompareLT) ||
                (operation is OpcodeCompareGTE) ||
                (operation is OpcodeCompareLTE) ||
                (operation is OpcodeMathAdd) ||
                (operation is OpcodeMathMultiply);
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
    }
    public class IRUnaryOp : IRInstruction, IResultingInstruction, ISingleOperandInstruction
    {
        public override bool SideEffects => false;
        public IRValue Result { get; set; }
        public Opcode Operation { get; }
        public IRValue Operand { get; set; }
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
    }
    public class IRNoStackInstruction : IRInstruction
    {
        public override bool SideEffects => false;
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
    }
    public class IRUnaryConsumer : IRInstruction, ISingleOperandInstruction
    {
        public override bool SideEffects { get; }
        public Opcode Operation { get; }
        public IRValue Operand { get; }
        public IRUnaryConsumer(Opcode opcode, IRValue operand, bool sideEffects = false) : base(opcode)
        {
            Operation = opcode;
            Operand = operand;
            SideEffects = sideEffects;
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
    }
    public class IRPop : IRInstruction, ISingleOperandInstruction
    {
        public override bool SideEffects => false;
        public IRValue Value { get; set; }
        IRValue ISingleOperandInstruction.Operand => Value;
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
            => "{pop}";
    }
    public class IRNonVarPush : IRInstruction
    {
        public override bool SideEffects => false;
        public Opcode Operation { get; }
        public IRNonVarPush(Opcode opcode) : base(opcode)
            => Operation = opcode;
        internal override IEnumerable<Opcode> EmitOpcode()
        {
            Operation.Label = string.Empty;
            yield return Operation;
        }
        public override string ToString()
            => Operation.ToString();
    }
    public class IRSuffixGet : IRInteractsInstruction, IResultingInstruction, ISingleOperandInstruction
    {
        public IRValue Result { get; set; }
        public IRValue Object { get; set; }
        public string Suffix { get; set; }
        IRValue ISingleOperandInstruction.Operand => Object;
        public IRSuffixGet(IRTemp result, IRValue obj, OpcodeGetMember opcodeGetMember) : base(obj, opcodeGetMember)
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
    }
    public class IRSuffixGetMethod : IRSuffixGet
    {
        public override bool SideEffects => false;
        public IRSuffixGetMethod(IRTemp result, IRValue obj, OpcodeGetMethod opcode) : base(result, obj, opcode) { }
        internal override IEnumerable<Opcode> EmitOpcode()
        {
            foreach (Opcode opcode in Object.EmitPush())
                yield return opcode;
            yield return SetSourceLocation(new OpcodeGetMethod(Suffix));
        }
        public override string ToString()
            => string.Format("{{gmet \"{0}\"}}", Suffix);
    }
    public class IRSuffixSet : IRInteractsInstruction, IMultipleOperandInstruction
    {
        public override bool SideEffects { get; }
        public IRValue Object { get; set; }
        public IRValue Value { get; set; }
        public IEnumerable<IRValue> Operands { get { yield return Object; yield return Value; } }
        public string Suffix { get; }
        public IRSuffixSet(IRValue obj, IRValue value, OpcodeSetMember opcodeSetMember) : base(obj, opcodeSetMember)
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
    }
    public class IRIndexGet : IRInstruction, IResultingInstruction, IMultipleOperandInstruction
    {
        public override bool SideEffects => false;
        public IRValue Result { get; }
        public IRValue Object { get; set; }
        public IRValue Index { get; set; }
        public IEnumerable<IRValue> Operands { get { yield return Object; yield return Index; } }
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
    }
    public class IRIndexSet : IRInstruction, IMultipleOperandInstruction
    {
        public override bool SideEffects => false;
        public IRValue Object { get; set; }
        public IRValue Index { get; set; }
        public IRValue Value { get; set; }
        public IEnumerable<IRValue> Operands { get { yield return Object; yield return Index; yield return Value; } }
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
    }
    public class IRJump : IRInstruction
    {
        public override bool SideEffects => false;
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
    }
    public class IRJumpStack : IRInstruction, ISingleOperandInstruction
    {
        public override bool SideEffects => false;
        public IRValue Distance { get; set; }
        IRValue ISingleOperandInstruction.Operand => Distance;
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
    }
    public class IRBranch : IRInstruction, ISingleOperandInstruction
    {
        public override bool SideEffects => false;
        public IRValue Condition { get; set; }
        IRValue ISingleOperandInstruction.Operand => Condition;
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
    }
    public class IRCall : IRInstruction, IResultingInstruction, IMultipleOperandInstruction
    {
        protected static readonly Function.FunctionManager functionManager = new Function.FunctionManager(null);
        public override bool SideEffects { get; }
        public IRValue Result { get; set; }
        public string Function { get; }
        public List<IRValue> Arguments { get; } = new List<IRValue>();
        public IEnumerable<IRValue> Operands => Enumerable.Reverse(Arguments);
        public IRValue IndirectMethod { get; internal set; }
        public bool Direct { get; }
        public bool EmitArgMarker { get; set; }
        private IRCall(IRTemp target, OpcodeCall opcode, bool emitArgMarker) : base(opcode)
        {
            Result = target;
            Function = (string)opcode.Destination;
            Direct = opcode.Direct;
            SideEffects = CheckIfFunctionHasSideEffects(opcode);
            EmitArgMarker = emitArgMarker;
        }
        private bool CheckIfFunctionHasSideEffects(OpcodeCall opcode)
        {
            if (!Direct)
                return true;
            if (opcode.Destination is string functionName && functionManager.Exists(functionName))
            {
                functionName = functionName.ToLower().Replace("()", "");
                switch (functionName)
                {
                    case "vcrs":
                    case "vectorcrossproduct":
                    case "vdot":
                    case "vectordotproduct":
                    case "vxcl":
                    case "vectorexclude":
                    case "vang":
                    case "vectorangle":
                    case "clearscreen":
                    case "hudtext":
                    case "add":
                    case "remove":
                    case "processor":
                    case "edit":
                    case "printlist":
                    case "node":
                    case "v":
                    case "r":
                    case "q":
                    case "createorbit":
                    case "rotatefromto":
                    case "lookdirup":
                    case "angleaxis":
                    case "latlng":
                    case "vessel":
                    case "body":
                    case "bodyexists":
                    case "bodyatmosphere":
                    case "bounds":
                    case "heading":
                    case "slidenote":
                    case "note":
                    case "getvoice":
                    case "stopallvoices":
                    case "time":
                    case "timestamp":
                    case "timespan":
                    case "hsv":
                    case "hsva":
                    case "vecdraw":
                    case "vecdrawargs":
                    case "clearvecdraws":
                    case "clearguis":
                    case "gui":
                    case "career":
                    case "lex":
                    case "lexicon":
                    case "list":
                    case "pidloop":
                    case "queue":
                    case "stack":
                    case "uniqueset":
                    case "abs":
                    case "mod":
                    case "floor":
                    case "ceiling":
                    case "round":
                    case "sqrt":
                    case "ln":
                    case "log10":
                    case "min":
                    case "max":
                    case "random":
                    case "randomseed":
                    case "char":
                    case "unchar":
                    case "range":
                    case "constant":
                    case "sin":
                    case "cos":
                    case "tan":
                    case "arcsin":
                    case "arccos":
                    case "arctan":
                    case "arctan2":
                    case "anglediff":
                        return false;
                    case "addAlarm":
                    case "listAlarms":
                    case "deleteAlarm":
                    case "buildlist":
                    case "positionat":
                    case "velocityat":
                    case "orbitat":
                    case "allwaypoints":
                    case "waypoint":
                    case "print":
                    case "printat":
                    case "logfile":
                    case "debugdump":
                    case "debugfreezegame":
                    case "profileresult":
                    case "makebuiltindelegate":
                    case "droppriority":
                    case "stage":
                    case "warpto":
                    case "transfer":
                    case "transferall":
                    case "run":
                    case "load":
                    case "toggleflybywire":
                    case "selectautopilotmode":
                    case "reboot":
                    case "shutdown":
                    case "scriptpath":
                    case "switch":
                    case "cd":
                    case "chdir":
                    case "copy_deprecated":
                    case "rename_file_deprecated":
                    case "rename_volume_deprecated":
                    case "delete_deprecated":
                    case "copypath":
                    case "movepath":
                    case "deletepath":
                    case "writejson":
                    case "readjson":
                    case "exists":
                    case "open":
                    case "create":
                    case "createdir":
                    case "path":
                    case "volume":
                    default:
                        return true;
                }
            }
            else
                return true;
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
    }
    public class IRReturn : IRInstruction, ISingleOperandInstruction
    {
        public override bool SideEffects => false;
        public IRValue Value { get; set; }
        IRValue ISingleOperandInstruction.Operand => Value;
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
            => string.Format("{{ret {0}}}", Depth);
    }
}
