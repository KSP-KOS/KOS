using System.Collections.Generic;
using System.Linq;

namespace kOS.Safe.Compilation.IR
{
    public abstract class IRInstruction
    {
        // Should-be-static
        public abstract bool SideEffects { get; }
        internal abstract IEnumerable<Opcode> EmitOpcode();
    }
    public abstract class IRInteractsInstruction : IRInstruction
    {
        public override bool SideEffects { get; }
        public IRInteractsInstruction(IRValue interactor)
        {
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
    public class IRAssign : IRInstruction
    {
        public enum StoreScope
        {
            Ambivalent,
            Local,
            Global
        }
        public override bool SideEffects => false;
        public string Target { get; }
        public IRValue Value { get; }
        public StoreScope Scope { get; set; } = StoreScope.Ambivalent;
        public bool AssertExists { get; set; } = false;
        public IRAssign(string target, IRValue value)
        {
            Target = target;
            Value = value;
        }
        public IRAssign(OpcodeIdentifierBase opcode, IRValue value) : this(opcode.Identifier, value) { }
        internal override IEnumerable<Opcode> EmitOpcode()
        {
            foreach (Opcode opcode in Value.EmitPush())
                yield return opcode;
            if (AssertExists)
            {
                yield return new OpcodeStoreExist(Target);
                yield break;
            }
            switch (Scope)
            {
                case StoreScope.Local:
                    yield return new OpcodeStoreLocal(Target);
                    yield break;
                case StoreScope.Global:
                    yield return new OpcodeStoreGlobal(Target);
                    yield break;
                default:
                case StoreScope.Ambivalent:
                    yield return new OpcodeStore(Target);
                    yield break;
            }
        }
        public override string ToString()
            => string.Format("{{store {0}}}", Value.ToString());
    }
    public class IRBinaryOp : IRInstruction
    {
        public override bool SideEffects => false;
        public IRTemp Result { get; }
        public BinaryOpcode Operation { get; protected set; }
        public IRValue Left { get; protected set; }
        public IRValue Right { get; protected set; }
        public bool Commutative { get; }
        public IRBinaryOp(IRTemp result, BinaryOpcode operation, IRValue left, IRValue right)
        {
            Result = result;
            Operation = operation;
            Left = left;
            Right = right;
            Commutative = (operation is OpcodeCompareEqual) ||
                (operation is OpcodeCompareNE) ||
                (operation is OpcodeCompareGT) ||
                (operation is OpcodeCompareLT) ||
                (operation is OpcodeCompareGTE) ||
                (operation is OpcodeCompareLTE) ||
                (operation is OpcodeMathAdd) ||
                (operation is OpcodeMathMultiply);
        }
        public void ReverseOperands()
        {
            if (!Commutative)
                throw new System.InvalidOperationException($"{this} is not commutative.");
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
            yield return Operation;
        }
        public override string ToString()
            => Operation.ToString();
    }
    public class IRUnaryOp : IRInstruction
    {
        public override bool SideEffects => false;
        public IRTemp Result { get; }
        public Opcode Operation { get; }
        public IRValue Operand { get; }
        public IRUnaryOp(IRTemp result, Opcode operation, IRValue operand)
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
        public IRNoStackInstruction(Opcode opcode)
            => Operation = opcode;
        internal override IEnumerable<Opcode> EmitOpcode()
        {
            Operation.Label = string.Empty;
            yield return Operation;
        }
        public override string ToString()
            => Operation.ToString();
    }
    public class IRUnaryConsumer : IRInstruction
    {
        public override bool SideEffects { get; }
        public Opcode Operation { get; }
        public IRValue Operand { get; }
        public IRUnaryConsumer(Opcode opcode, IRValue operand, bool sideEffects = false)
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
    public class IRPop : IRInstruction
    {
        public override bool SideEffects => false;
        internal override IEnumerable<Opcode> EmitOpcode()
        {
            yield return new OpcodePop();
        }
        public override string ToString()
            => "{pop}";
    }
    public class IRNonVarPush : IRInstruction
    {
        public override bool SideEffects => false;
        public Opcode Operation { get; }
        public IRNonVarPush(Opcode opcode)
            => Operation = opcode;
        internal override IEnumerable<Opcode> EmitOpcode()
        {
            Operation.Label = string.Empty;
            yield return Operation;
        }
        public override string ToString()
            => Operation.ToString();
    }
    public class IRSuffixGet : IRInteractsInstruction
    {
        public IRTemp Result { get; }
        public IRValue Object {  get; }
        public string Suffix { get; }
        public IRSuffixGet(IRTemp result, IRValue obj, string suffix) : base(obj)
        {
            Result = result;
            Object = obj;
            Suffix = suffix;
        }
        public IRSuffixGet(IRTemp result, IRValue obj, OpcodeGetMember opcode) : this(result, obj, opcode.Identifier) { }
        internal override IEnumerable<Opcode> EmitOpcode()
        {
            foreach (Opcode opcode in Object.EmitPush())
                yield return opcode;
            yield return new OpcodeGetMember(Suffix);
        }
        public override string ToString()
            => string.Format("{{gmb \"{0}\"}}", Suffix);
    }
    public class IRSuffixGetMethod : IRSuffixGet
    {
        public override bool SideEffects => false;
        public IRSuffixGetMethod(IRTemp result, IRValue obj, string suffix) : base(result, obj, suffix) { }
        public IRSuffixGetMethod(IRTemp result, IRValue obj, OpcodeGetMethod opcode) : this(result, obj, opcode.Identifier) { }
        internal override IEnumerable<Opcode> EmitOpcode()
        {
            foreach (Opcode opcode in Object.EmitPush())
                yield return opcode;
            yield return new OpcodeGetMethod(Suffix);
        }
        public override string ToString()
            => string.Format("{{gmet \"{0}\"}}", Suffix);
    }
    public class IRSuffixSet : IRInteractsInstruction
    {
        public override bool SideEffects { get; }
        public IRValue Object { get; }
        public IRValue Value { get; }
        public string Suffix { get; }
        public IRSuffixSet(IRValue obj, IRValue value, string suffix) : base(obj)
        {
            Object = obj;
            Value = value;
            Suffix = suffix;
        }
        public IRSuffixSet(IRValue obj, IRValue value, OpcodeSetMember opcode) : this(obj, value, opcode.Identifier) { }
        internal override IEnumerable<Opcode> EmitOpcode()
        {
            foreach (Opcode opcode in Object.EmitPush())
                yield return opcode;
            foreach (Opcode opcode in Value.EmitPush())
                yield return opcode;
            yield return new OpcodeSetMember(Suffix);
        }
        public override string ToString()
            => string.Format("{{smb \"{0}\"}}", Suffix);
    }
    public class IRIndexGet : IRInstruction
    {
        public override bool SideEffects => false;
        public IRValue Result { get; }
        public IRValue Object { get; }
        public IRValue Index { get; }
        public IRIndexGet(IRValue result, IRValue obj, IRValue index)
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
            yield return new OpcodeGetIndex();
        }
        public override string ToString()
            => "{gidx}";
    }
    public class IRIndexSet : IRInstruction
    {
        public override bool SideEffects => false;
        public IRValue Object { get; }
        public IRValue Index { get; }
        public IRValue Value { get; }
        public IRIndexSet(IRValue obj, IRValue index, IRValue value)
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
            yield return new OpcodeSetIndex();
        }
        public override string ToString()
            => "{sidx}";
    }
    public class IRJump : IRInstruction
    {
        public override bool SideEffects => false;
        public BasicBlock Target { get; }
        public IRJump(BasicBlock target)
        {
            Target = target;
        }
        internal override IEnumerable<Opcode> EmitOpcode()
        {
            yield return new OpcodeBranchJump() { DestinationLabel = Target.Label };
        }
        public override string ToString()
            => string.Format("{{jump {0}}}", Target.Label);
    }
    public class IRJumpStack : IRInstruction
    {
        public override bool SideEffects => false;
        public IRValue Distance { get; }
        public List<BasicBlock> Targets { get; } = new List<BasicBlock>();
        public IRJumpStack(IRValue distance, IEnumerable<BasicBlock> targets)
        {
            Distance = distance;
            Targets.AddRange(targets);
        }
        internal override IEnumerable<Opcode> EmitOpcode()
        {
            foreach (Opcode opcode in Distance.EmitPush())
                yield return opcode;
            yield return new OpcodeJumpStack();
        }
    }
    public class IRBranch : IRInstruction
    {
        public override bool SideEffects => false;
        public IRValue Condition { get; }
        public BasicBlock True { get; }
        public BasicBlock False { get; }
        public bool PreferFalse { get; set; } = false;
        public IRBranch(IRValue condition, BasicBlock onTrue, BasicBlock onFalse)
        {
            Condition = condition;
            True = onTrue;
            False = onFalse;
        }
        internal override IEnumerable<Opcode> EmitOpcode()
        {
            foreach (Opcode opcode in Condition.EmitPush())
                yield return opcode;
            if (PreferFalse)
            {
                yield return new OpcodeBranchIfFalse() { DestinationLabel = False.Label };
                yield return new OpcodeBranchJump() { DestinationLabel = True.Label };
            }
            else
            {
                yield return new OpcodeBranchIfTrue() { DestinationLabel = True.Label };
                yield return new OpcodeBranchJump() { DestinationLabel = False.Label };
            }
        }
        public override string ToString()
            => string.Format("{{br.? {0}/{1}}}", True.Label, False.Label);
    }
    public class IRCall : IRInstruction
    {
        protected static readonly Function.FunctionManager functionManager = new Function.FunctionManager(null);
        public override bool SideEffects { get; }
        public IRTemp Target { get; }
        public string Function { get; }
        public List<IRValue> Arguments { get; } = new List<IRValue>();
        public IRValue IndirectMethod { get; internal set; }
        public bool Direct { get; }
        public bool EmitArgMarker;
        private IRCall(IRTemp target, OpcodeCall opcode, bool emitArgMarker)
        {
            Target = target;
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
                    case "addAlarm":
                    case "listAlarms":
                    case "deleteAlarm":
                    case "buildlist":
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
                    case "positionat":
                    case "velocityat":
                    case "orbitat":
                    case "career":
                    case "allwaypoints":
                    case "waypoint":
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
                    case "print":
                    case "printat":
                    case "logfile":
                    case "debugdump":
                    case "debugfreezegame":
                    case "profileresult":
                    case "makebuiltindelegate":
                    case "droppriority":
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
            yield return new OpcodeCall(Function);
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
    public class IRReturn : IRInstruction
    {
        public override bool SideEffects => false;
        public IRValue Value { get; set; }
        public short Depth { get; internal set; }
        public IRReturn(short depth)
            => Depth = depth;
        internal override IEnumerable<Opcode> EmitOpcode()
        {
            if (Value != null)
                foreach (Opcode opcode in Value.EmitPush())
                    yield return opcode;
            else
                yield return new OpcodePush(null);
            yield return new OpcodeReturn(Depth);
        }
        public override string ToString()
            => string.Format("{{ret {0}}}", Depth);
    }
}
