using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Execution;

namespace kOS.Safe.Compilation
{
    #region Base classes

    public class BinaryOpcode : Opcode
    {
        protected object Argument1 { get; set; }
        protected object Argument2 { get; set; }

        public override void Execute(ICpu cpu)
        {
            Argument2 = cpu.PopValue();
            Argument1 = cpu.PopValue();

            // convert floats to doubles
            if (Argument1 is float) Argument1 = Convert.ToDouble(Argument1);
            if (Argument2 is float) Argument2 = Convert.ToDouble(Argument2);

            Calculator calc = Calculator.GetCalculator(Argument1, Argument2);
            object result = ExecuteCalculation(calc);
            cpu.PushStack(result);
        }

        protected virtual object ExecuteCalculation(Calculator calc)
        {
            return null;
        }
    }

    #endregion

    #region General

    public class OpcodeStore : Opcode
    {
        public override string Name { get { return "store"; } }

        public override void Execute(ICpu cpu)
        {
            object value = cpu.PopValue();
            var identifier = (string)cpu.PopStack();
            cpu.SetValue(identifier, value);
        }
    }

    public class OpcodeUnset : Opcode
    {
        public override string Name { get { return "unset"; } }

        public override void Execute(ICpu cpu)
        {
            object identifier = cpu.PopStack();
            if (identifier != null)
            {
                cpu.RemoveVariable(identifier.ToString());
            }
            else
            {
                cpu.RemoveAllVariables();
            }
        }
    }

    public class OpcodeGetMember : Opcode
    {
        public override string Name { get { return "getmember"; } }

        public override void Execute(ICpu cpu)
        {
            string suffixName = cpu.PopStack().ToString().ToUpper();
            object popValue = cpu.PopValue();

            var specialValue = popValue as Structure;
            if (specialValue == null)
            {
                throw new Exception(string.Format("Values of type {0} cannot have suffixes", popValue.GetType()));
            }

            object value = specialValue.GetSuffix(suffixName);
            if (value != null)
            {
                cpu.PushStack(value);
            }
            else
            {
                throw new Exception(string.Format("Suffix {0} not found on object", suffixName));
            }
        }
    }

    public class OpcodeSetMember : Opcode
    {
        public override string Name { get { return "setmember"; } }

        public override void Execute(ICpu cpu)
        {
            object value = cpu.PopValue();
            string suffixName = cpu.PopStack().ToString().ToUpper();
            object popValue = cpu.PopValue();

            var specialValue = popValue as Structure;
            if (specialValue == null)
            {
                throw new Exception(string.Format("Values of type {0} cannot have suffixes", popValue.GetType()));
            }

            if (!specialValue.SetSuffix(suffixName, value))
            {
                throw new Exception(string.Format("Suffix {0} not found on object", suffixName));
            }
        }
    }

    public class OpcodeGetIndex : Opcode
    {
        public override string Name { get { return "getindex"; } }

        public override void Execute(ICpu cpu)
        {
            object index = cpu.PopValue();
            if (index is double || index is float)
            {
                index = Convert.ToInt32(index);  // allow expressions like (1.0) to be indexes
            }
            object list = cpu.PopValue();

            if (!(list is IIndexable)) throw new Exception(string.Format("Can't iterate on an object of type {0}", list.GetType()));
            if (!(index is int)) throw new Exception("The index must be an integer number");

            object value = ((IIndexable)list).GetIndex((int)index);
            cpu.PushStack(value);
        }
    }

    public class OpcodeSetIndex : Opcode
    {
        public override string Name { get { return "setindex"; } }

        public override void Execute(ICpu cpu)
        {
            object value = cpu.PopValue();
            object index = cpu.PopValue();
            object list = cpu.PopValue();
            if (index is double || index is float)
            {
                index = Convert.ToInt32(index);  // allow expressions like (1.0) to be indexes
            }
            if (!(list is IIndexable)) throw new Exception(string.Format("Can't iterate on an object of type {0}", list.GetType()));
            if (!(index is int)) throw new Exception("The index must be an integer number");

            if (value != null)
            {
                ((IIndexable)list).SetIndex((int)index, value);
            }
        }
    }

    public class OpcodeEOF : Opcode
    {
        public override string Name { get { return "EOF"; } }
    }

    public class OpcodeEOP : Opcode
    {
        public override string Name { get { return "EOP"; } }
    }

    public class OpcodeNOP : Opcode
    {
        public override string Name { get { return "nop"; } }
    }
    
    #endregion

    #region Branch

    public class BranchOpcode : Opcode
    {
        public int Distance { get; set; }

        public override string ToString()
        {
            return string.Format("{0} {1}", Name, Distance);
        }
    }

    public class OpcodeBranchIfFalse : BranchOpcode
    {
        public override string Name { get { return "br.false"; } }

        public override void Execute(ICpu cpu)
        {
            bool condition = Convert.ToBoolean(cpu.PopValue());
            DeltaInstructionPointer = !condition ? Distance : 1;
        }
    }

    public class OpcodeBranchJump : BranchOpcode
    {
        public override string Name { get { return "jump"; } }

        public override void Execute(ICpu cpu)
        {
            DeltaInstructionPointer = Distance;
        }
    }

    #endregion

    #region Compare

    public class OpcodeCompareGT : BinaryOpcode
    {
        public override string Name { get { return "gt"; } }

        protected override object ExecuteCalculation(Calculator calc)
        {
            return calc.GreaterThan(Argument1, Argument2);
        }
    }

    public class OpcodeCompareLT : BinaryOpcode
    {
        public override string Name { get { return "lt"; } }

        protected override object ExecuteCalculation(Calculator calc)
        {
            return calc.LessThan(Argument1, Argument2);
        }
    }

    public class OpcodeCompareGTE : BinaryOpcode
    {
        public override string Name { get { return "gte"; } }

        protected override object ExecuteCalculation(Calculator calc)
        {
            return calc.GreaterThanEqual(Argument1, Argument2);
        }
    }

    public class OpcodeCompareLTE : BinaryOpcode
    {
        public override string Name { get { return "lte"; } }

        protected override object ExecuteCalculation(Calculator calc)
        {
            return calc.LessThanEqual(Argument1, Argument2);
        }
    }

    public class OpcodeCompareNE : BinaryOpcode
    {
        public override string Name { get { return "ne"; } }

        protected override object ExecuteCalculation(Calculator calc)
        {
            return calc.NotEqual(Argument1, Argument2);
        }
    }
    
    public class OpcodeCompareEqual : BinaryOpcode
    {
        public override string Name { get { return "eq"; } }

        protected override object ExecuteCalculation(Calculator calc)
        {
            return calc.Equal(Argument1, Argument2);
        }
    }

    #endregion

    #region Math
        
    public class OpcodeMathNegate : Opcode
    {
        public override string Name { get { return "negate"; } }

        public override void Execute(ICpu cpu)
        {
            object value = cpu.PopValue();
            object result;

            if (value is int)
                result = -((int)value);
            else if (value is float)
                result = -(Convert.ToDouble(value));
            else if (value is double)
                result = -((double)value);
            else
                throw new ArgumentException(string.Format("Can't negate object {0} of type {1}", value, value.GetType()));

            cpu.PushStack(result);
        }
    }

    public class OpcodeMathAdd : BinaryOpcode
    {
        public override string Name { get { return "add"; } }

        protected override object ExecuteCalculation(Calculator calc)
        {
            object result = calc.Add(Argument1, Argument2);
            // TODO: complete message
            if (result == null) throw new ArgumentException("Can't add ....");
            return result;
        }
    }

    public class OpcodeMathSubtract : BinaryOpcode
    {
        public override string Name { get { return "sub"; } }

        protected override object ExecuteCalculation(Calculator calc)
        {
            return calc.Subtract(Argument1, Argument2);
        }
    }

    public class OpcodeMathMultiply : BinaryOpcode
    {
        public override string Name { get { return "mult"; } }

        protected override object ExecuteCalculation(Calculator calc)
        {
            return calc.Multiply(Argument1, Argument2);
        }
    }

    public class OpcodeMathDivide : BinaryOpcode
    {
        public override string Name { get { return "div"; } }

        protected override object ExecuteCalculation(Calculator calc)
        {
            return calc.Divide(Argument1, Argument2);
        }
    }

    public class OpcodeMathPower : BinaryOpcode
    {
        public override string Name { get { return "pow"; } }

        protected override object ExecuteCalculation(Calculator calc)
        {
            return calc.Power(Argument1, Argument2);
        }
    }

    #endregion

    #region Logic

    public class OpcodeLogicToBool : Opcode
    {
        public override string Name { get { return "bool"; } }

        public override void Execute(ICpu cpu)
        {
            object value = cpu.PopValue();
            bool result = Convert.ToBoolean(value);
            cpu.PushStack(result);
        }
    }

    public class OpcodeLogicNot : Opcode
    {
        public override string Name { get { return "not"; } }

        public override void Execute(ICpu cpu)
        {
            object value = cpu.PopValue();
            object result;

            if (value is bool)
                result = !((bool)value);
            else if (value is int)
                result = Convert.ToBoolean(value) ? 0 : 1;
            else if ((value is double) || (value is float))
                result = Convert.ToBoolean(value) ? 0.0 : 1.0;
            else
                throw new ArgumentException(string.Format("Can't negate object {0} of type {1}", value, value.GetType()));

            cpu.PushStack(result);
        }
    }

    public class OpcodeLogicAnd : Opcode
    {
        public override string Name { get { return "and"; } }

        public override void Execute(ICpu cpu)
        {
            bool argument2 = Convert.ToBoolean(cpu.PopValue());
            bool argument1 = Convert.ToBoolean(cpu.PopValue());
            object result = argument1 & argument2;
            cpu.PushStack(result);
        }
    }

    public class OpcodeLogicOr : Opcode
    {
        public override string Name { get { return "or"; } }

        public override void Execute(ICpu cpu)
        {
            bool argument2 = Convert.ToBoolean(cpu.PopValue());
            bool argument1 = Convert.ToBoolean(cpu.PopValue());
            object result = argument1 | argument2;
            cpu.PushStack(result);
        }
    }

    #endregion

    #region Call

    public class OpcodeCall : Opcode
    {
        public object Destination { get; set; }

        public override string Name { get { return "call"; } }

        public OpcodeCall(object destination)
        {
            Destination = destination;
        }

        public override void Execute(ICpu cpu)
        {
            object functionPointer = cpu.GetValue(Destination);
            if (functionPointer is int)
            {
                int currentPointer = cpu.InstructionPointer;
                DeltaInstructionPointer = (int)functionPointer - currentPointer;
                var contextRecord = new SubroutineContext(currentPointer+1);
                cpu.PushStack(contextRecord);
                cpu.MoveStackPointer(-1);
            }
            else
            {
                var name = functionPointer as string;
                if (name != null)
                {
                    string functionName = name;
                    functionName = functionName.Substring(0, functionName.Length - 2);
                    cpu.CallBuiltinFunction(functionName);
                }
            }
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", Name, Destination);
        }
    }

    public class OpcodeReturn : Opcode
    {
        public override string Name { get { return "return"; } }

        public override void Execute(ICpu cpu)
        {
            cpu.MoveStackPointer(1);
            object shouldBeContextRecord = cpu.PopValue();
            if ( !(shouldBeContextRecord is SubroutineContext) )
            {
                // This should never happen with any user code:
                throw new Exception( "kOS internal error: Stack misalignment detected when returning from routine.");
            }
            var contextRecord = shouldBeContextRecord as SubroutineContext;
            
            int destinationPointer = contextRecord.CameFromInstPtr;
            int currentPointer = cpu.InstructionPointer;
            DeltaInstructionPointer = destinationPointer - currentPointer;
        }
    }

    #endregion

    #region Stack

    public class OpcodePush : Opcode
    {
        public object Argument { get; set; }

        public override string Name { get { return "push"; } }

        public OpcodePush(object argument)
        {
            Argument = argument;
        }

        public override void Execute(ICpu cpu)
        {
            cpu.PushStack(Argument);
        }

        public override string ToString()
        {
            string argumentString = Argument != null ? Argument.ToString() : "null";
            return Name + " " + argumentString;
        }
    }

    public class OpcodePop : Opcode
    {
        public override string Name { get { return "pop"; } }

        public override void Execute(ICpu cpu)
        {
            cpu.PopStack();
        }
    }

    public class OpcodeDup : Opcode
    {
        public override string Name { get { return "dup"; } }

        public override void Execute(ICpu cpu)
        {
            object value = cpu.PopStack();
            cpu.PushStack(value);
            cpu.PushStack(value);
        }
    }

    public class OpcodeSwap : Opcode
    {
        public override string Name { get { return "swap"; } }

        public override void Execute(ICpu cpu)
        {
            object value1 = cpu.PopStack();
            object value2 = cpu.PopStack();
            cpu.PushStack(value1);
            cpu.PushStack(value2);
        }
    }

    #endregion

    #region Wait / Trigger

    public class OpcodeAddTrigger : Opcode
    {
        public bool ShouldWait { get; set; }
        
        public override string Name { get { return "addtrigger"; } }

        public OpcodeAddTrigger(bool shouldWait)
        {
            ShouldWait = shouldWait;
        }

        public override void Execute(ICpu cpu)
        {
            var functionPointer = (int)cpu.PopValue();
            cpu.AddTrigger(functionPointer);
            if (ShouldWait)
                cpu.StartWait(0);
        }

        public override string ToString()
        {
            return Name + " " + ShouldWait.ToString().ToLower();
        }
    }

    public class OpcodeRemoveTrigger : Opcode
    {
        public override string Name { get { return "removetrigger"; } }

        public override void Execute(ICpu cpu)
        {
            var functionPointer = (int)cpu.PopValue();
            cpu.RemoveTrigger(functionPointer);
        }
    }

    public class OpcodeWait : Opcode
    {
        public override string Name { get { return "wait"; } }

        public override void Execute(ICpu cpu)
        {
            object waitTime = cpu.PopValue();
            if (waitTime is double)
                cpu.StartWait((double)waitTime);
            else if (waitTime is int)
                cpu.StartWait((int)waitTime);
        }
    }

    public class OpcodeEndWait : Opcode
    {  
        public override string Name { get { return "endwait"; } }

        public override void Execute(ICpu cpu)
        {
            cpu.EndWait();
        }
    }

    #endregion

}

