using System.Collections.Generic;
using kOS.Safe.Compilation;
using kOS.Safe.Encapsulation;

namespace kOS.Safe.Execution
{
    public interface ICpu : IFixedUpdateObserver
    {
        void PushArgumentStack(object item);
        object PopArgumentStack();
        void PushScopeStack(object thing);
        object PopScopeStack(int howMany);
        List<VariableScope> GetCurrentClosure();
        IUserDelegate MakeUserDelegate(int entryPoint, bool withClosure);
        void AssertValidDelegateCall(IUserDelegate userDelegate);
        object GetValue(object testValue, bool barewordOkay = false);
        object PopValueArgument(bool barewordOkay = false);
        object PeekValueArgument(int digDepth, bool barewordOkay = false);
        object PeekRawArgument(int digDepth, out bool checkOkay);
        object PeekRawScope(int digDepth, out bool checkOkay);
        object PopValueEncapsulatedArgument(bool barewordOkay = false);
        object PeekValueEncapsulatedArgument(int digDepth, bool barewordOkay = false);
        Structure GetStructureEncapsulatedArgument(Structure testValue, bool barewordOkay = false);
        Structure PopStructureEncapsulatedArgument(bool barewordOkay = false);
        Structure PeekStructureEncapsulatedArgument(int digDepth, bool barewordOkay = false);
        int GetArgumentStackSize();
        void SetValue(string identifier, object value);
        void SetValueExists(string identifier, object value);
        void SetNewLocal(string identifier, object value);
        void SetGlobal(string identifier, object value);
        bool IdentifierExistsInScope(string identifier);
        string DumpVariables();
        string DumpStack();
        void RemoveVariable(string identifier);
        int InstructionPointer { get; set; }
        double SessionTime { get; }
        List<string> ProfileResult { get; }
        TriggerInfo AddTrigger(int triggerFunctionPointer, List<VariableScope> closure);
        TriggerInfo AddTrigger(TriggerInfo trigger);
        TriggerInfo AddTrigger(UserDelegate del, List<Structure> args);
        TriggerInfo AddTrigger(UserDelegate del, params Structure[] args);
        void RemoveTrigger(int triggerFunctionPointer);
        void RemoveTrigger(TriggerInfo trigger);
        void CallBuiltinFunction(string functionName);
        bool BuiltInExists(string functionName);
        void BreakExecution(bool manual);
        void YieldProgram(YieldFinishedDetector yieldTracker);
        void AddVariable(Variable variable, string identifier, bool local, bool overwrite = false);
        IProgramContext GetCurrentContext();
        void AddPopContextNotifyee(IPopContextNotifyee notifyee);
        void RemovePopContextNotifyee(IPopContextNotifyee notifyee);
        Opcode GetCurrentOpcode();
        Opcode GetOpcodeAt(int instructionPtr);
        void Boot();
        int InstructionsThisUpdate { get; }
        void StartCompileStopwatch();
        void StopCompileStopwatch();
        IProgramContext SwitchToProgramContext();

        /// <summary>
        /// Return the subroutine call trace of how the code got to where it is right now.
        /// </summary>
        /// <returns>The first item in the list is the current instruction pointer.
        /// The rest of the items in the list after that are the instruction pointers of the Opcodecall instructions
        /// that got us to here.</returns>
        List<int> GetCallTrace();

        List<string> GetCodeFragment(int contextLines);
        void RunProgram(List<Opcode> program);

        bool IsPoppingContext { get; }
    }
}
