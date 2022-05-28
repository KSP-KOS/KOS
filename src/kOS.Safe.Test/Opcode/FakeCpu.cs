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

        public int NextTriggerInstanceId { get { return -99;} }

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

        public void PushArgumentStack(object item)
        {
            fakeStack.Push(item);
        }

        public object PopArgumentStack()
        {
            return fakeStack.Pop();
        }

        public void PushScopeStack(object thing)
        {
            throw new NotImplementedException();
        }

        public object PopScopeStack(int howMany)
        {
            throw new NotImplementedException();
        }

        public void PushNewScope(Int16 scopeId, Int16 parentScopeId)
        {
            throw new NotImplementedException();
        }

        public void DropBackPriority()
        {
            throw new NotImplementedException();
        }

        public List<VariableScope> GetCurrentClosure()
        {
            throw new NotImplementedException();
        }

        public SubroutineContext GetCurrentSubroutineContext()
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

        public object PopValueArgument(bool barewordOkay = false)
        {
            return PopArgumentStack();
        }

        public object PeekValueArgument(int digDepth, bool barewordOkay = false)
        {
            throw new NotImplementedException();
        }

        public object PeekRawArgument(int digDepth, out bool checkOkay)
        {
            throw new NotImplementedException();
        }

        public object PeekRawScope(int digDepth, out bool checkOkay)
        {
            throw new NotImplementedException();
        }

        public Encapsulation.Structure GetStructureEncapsulatedArgument(Encapsulation.Structure testValue, bool barewordOkay = false)
        {
            throw new NotImplementedException();
        }

        public Encapsulation.Structure PopStructureEncapsulatedArgument(bool barewordOkay = false)
        {
            return kOS.Safe.Encapsulation.Structure.FromPrimitiveWithAssert(PopValueArgument(barewordOkay));
        }

        public Encapsulation.Structure PeekStructureEncapsulatedArgument(int digDepth, bool barewordOkay = false)
        {
            return kOS.Safe.Encapsulation.Structure.FromPrimitiveWithAssert(PeekValueArgument(digDepth, barewordOkay));
        }
        
        public object PopValueEncapsulatedArgument(bool barewordOkay = false)
        {
            return kOS.Safe.Encapsulation.Structure.FromPrimitive(PopValueArgument(barewordOkay));
        }

        public object PeekValueEncapsulatedArgument(int digDepth, bool barewordOkay = false)
        {
            return kOS.Safe.Encapsulation.Structure.FromPrimitive(PeekValueArgument(digDepth, barewordOkay));
        }

        public int GetArgumentStackSize()
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
        public TriggerInfo AddTrigger(int triggerFunctionPointer, InterruptPriority priority, int instanceId, bool immediate, List<VariableScope> closure)
        {
            throw new NotImplementedException();
        }

        public TriggerInfo AddTrigger(UserDelegate del, InterruptPriority priority, int instanceId,bool immediate, List<kOS.Safe.Encapsulation.Structure> args)
        {
            throw new NotImplementedException();
        }

        public TriggerInfo AddTrigger(UserDelegate del, InterruptPriority priority, int instanceId, bool immediate, params kOS.Safe.Encapsulation.Structure[] args)
        {
            throw new NotImplementedException();
        }

        public TriggerInfo AddTrigger(TriggerInfo trigger, bool immediate)
        {
            throw new NotImplementedException();
        }

        public void RemoveTrigger(int triggerFunctionPointer, int instanceId)
        {
            throw new NotImplementedException();
        }

        public void RemoveTrigger(TriggerInfo trigger)
        {
            throw new NotImplementedException();
        }

        public void CancelCalledTriggers(int triggerFunctionPointer, int instanceId)
        {
            throw new NotImplementedException();
        }

        public void CancelCalledTriggers(TriggerInfo trigger)
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

        public void AddPopContextNotifyee(IPopContextNotifyee notifyee)
        {
            throw new NotImplementedException();
        }

        public void RemovePopContextNotifyee(IPopContextNotifyee notifyee)
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
