using System;
using System.Collections.Generic;
using kOS.Safe.Execution;
using kOS.Safe.Encapsulation;

namespace kOS.Safe.Test.Opcode
{
    public class FakeCpu : ICpu
    {
        private readonly Stack<object> fakeStack;
        public bool IsPoppingContext { get { return false; } }

        public FakeCpu()
        {
            fakeStack = new Stack<object>();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void KOSFixedUpdate(double deltaTime)
        {
            throw new NotImplementedException();
        }

        public void PushStack(object item)
        {
            fakeStack.Push(item);
        }

        public object PopStack()
        {
            return fakeStack.Pop();
        }

        public void MoveStackPointer(int delta)
        {
            throw new NotImplementedException();
        }

        public void PushAboveStack(object thing)
        {
            throw new NotImplementedException();
        }

        public object PopAboveStack(int howMany)
        {
            throw new NotImplementedException();
        }

        public List<VariableScope> GetCurrentClosure()
        {
            throw new NotImplementedException();
        }

        public IUserDelegate MakeUserDelegate(int entryPoint, bool withClosure)
        {
            throw new NotImplementedException();
        }

        public void AssertValidDelegateCall(IUserDelegate userDelegate)
        {
            throw new NotImplementedException();
        }

        public object GetValue(object testValue, bool barewordOkay = false)
        {
            throw new NotImplementedException();
        }

        public object PopValue(bool barewordOkay = false)
        {
            return PopStack();
        }

        public object PeekValue(int digDepth, bool barewordOkay = false)
        {
            throw new NotImplementedException();
        }

        public object PeekRaw(int digDepth, out bool checkOkay)
        {
            throw new NotImplementedException();
        }

        public Encapsulation.Structure GetStructureEncapsulated(Encapsulation.Structure testValue, bool barewordOkay = false)
        {
            throw new NotImplementedException();
        }

        public Encapsulation.Structure PopStructureEncapsulated(bool barewordOkay = false)
        {
            return kOS.Safe.Encapsulation.Structure.FromPrimitiveWithAssert(PopValue(barewordOkay));
        }

        public Encapsulation.Structure PeekStructureEncapsulated(int digDepth, bool barewordOkay = false)
        {
            return kOS.Safe.Encapsulation.Structure.FromPrimitiveWithAssert(PeekValue(digDepth, barewordOkay));
        }
        
        public object PopValueEncapsulated(bool barewordOkay = false)
        {
            return kOS.Safe.Encapsulation.Structure.FromPrimitive(PopValue(barewordOkay));
        }

        public object PeekValueEncapsulated(int digDepth, bool barewordOkay = false)
        {
            return kOS.Safe.Encapsulation.Structure.FromPrimitive(PeekValue(digDepth, barewordOkay));
        }

        public int GetStackSize()
        {
            return fakeStack.Count;
        }

        public void SetValue(string identifier, object value)
        {
            throw new NotImplementedException();
        }

        public void SetValueExists(string identifier, object value)
        {
            throw new NotImplementedException();
        }

        public void SetNewLocal(string identifier, object value)
        {
            throw new NotImplementedException();
        }

        public void SetGlobal(string identifier, object value)
        {
            throw new NotImplementedException();
        }

        public bool IdentifierExistsInScope(string identifier)
        {
            throw new NotImplementedException();
        }

        public string DumpVariables()
        {
            throw new NotImplementedException();
        }

        public string DumpStack()
        {
            throw new NotImplementedException();
        }

        public void RemoveVariable(string identifier)
        {
            throw new NotImplementedException();
        }

        public int InstructionPointer
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public double SessionTime
        {
            get { throw new NotImplementedException(); }
        }

        public List<string> ProfileResult
        {
            get { throw new NotImplementedException(); }
        }
        public TriggerInfo AddTrigger(int triggerFunctionPointer, List<VariableScope> closure)
        {
            throw new NotImplementedException();
        }

        public TriggerInfo AddTrigger(UserDelegate del, List<kOS.Safe.Encapsulation.Structure> args)
        {
            throw new NotImplementedException();
        }

        public TriggerInfo AddTrigger(UserDelegate del, params kOS.Safe.Encapsulation.Structure[] args)
        {
            throw new NotImplementedException();
        }

        public TriggerInfo AddTrigger(TriggerInfo trigger)
        {
            throw new NotImplementedException();
        }

        public void RemoveTrigger(int triggerFunctionPointer)
        {
            throw new NotImplementedException();
        }

        public void RemoveTrigger(TriggerInfo trigger)
        {
            throw new NotImplementedException();
        }

        public double StartWait(double waitTime)
        {
            throw new NotImplementedException();
        }

        public void EndWait()
        {
            throw new NotImplementedException();
        }

        public void CallBuiltinFunction(string functionName)
        {
            throw new NotImplementedException();
        }

        public bool BuiltInExists(string functionName)
        {
            throw new NotImplementedException();
        }
        
        public void YieldProgram(YieldFinishedDetector yieldTracker)
        {
            throw new NotImplementedException();            
        }

        public void BreakExecution(bool manual)
        {
            throw new NotImplementedException();
        }

        public void AddVariable(Variable variable, string identifier, bool local, bool overwrite = false)
        {
            throw new NotImplementedException();
        }
        
        public IProgramContext GetCurrentContext()
        {
            throw new NotImplementedException();
        }

        public Compilation.Opcode GetOpcodeAt(int instructionPtr)
        {
            throw new NotImplementedException();
        }

        public void Boot()
        {
            throw new NotImplementedException();
        }

        public List<int> GetCallTrace()
        {
            throw new NotImplementedException();
        }

        public List<string> GetCodeFragment(int contextLines)
        {
            throw new NotImplementedException();
        }

        public void RunProgram(List<Compilation.Opcode> program)
        {
            throw new NotImplementedException();
        }


        public int InstructionsThisUpdate
        {
            get { throw new NotImplementedException(); }
        }

        public void StartCompileStopwatch()
        {
            throw new NotImplementedException();
        }

        public void StopCompileStopwatch()
        {
            throw new NotImplementedException();
        }

        public Compilation.Opcode GetCurrentOpcode()
        {
            throw new NotImplementedException();
        }

        public IProgramContext SwitchToProgramContext()
        {
            throw new NotImplementedException();
        }
    }
}
