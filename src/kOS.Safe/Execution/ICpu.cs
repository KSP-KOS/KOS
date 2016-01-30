using System.Collections.Generic;
using kOS.Safe.Compilation;
using kOS.Safe.Encapsulation;

namespace kOS.Safe.Execution
{
    public interface ICpu : IFixedUpdateObserver
    {
        void PushStack(object item);
        object PopStack();
        void MoveStackPointer(int delta);
        void PushAboveStack(object thing);
        object PopAboveStack(int howMany);
        List<VariableScope> GetCurrentClosure();
        IUserDelegate MakeUserDelegate(int entryPoint, bool withClosure);
        void AssertValidDelegateCall(IUserDelegate userDelegate);
        object GetValue(object testValue, bool barewordOkay = false);
        object PopValue(bool barewordOkay = false);
        object PeekValue(int digDepth, bool barewordOkay = false);
        object PeekRaw(int digDepth, out bool checkOkay);
        Structure GetValueEncapsulated(Structure testValue, bool barewordOkay = false);
        Structure PopValueEncapsulated(bool barewordOkay = false);
        Structure PeekValueEncapsulated(int digDepth, bool barewordOkay = false);
        int GetStackSize();
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
        void AddTrigger(int triggerFunctionPointer);
        void RemoveTrigger(int triggerFunctionPointer);
        void StartWait(double waitTime);
        void EndWait();
        void CallBuiltinFunction(string functionName);
        bool BuiltInExists(string functionName);
        void BreakExecution(bool manual);
        void AddVariable(Variable variable, string identifier, bool local, bool overwrite = false);
        Opcode GetOpcodeAt(int instructionPtr);
        void Boot();

        /// <summary>
        /// Return the subroutine call trace of how the code got to where it is right now.
        /// </summary>
        /// <returns>The first item in the list is the current instruction pointer.
        /// The rest of the items in the list after that are the instruction pointers of the Opcodecall instructions
        /// that got us to here.</returns>
        List<int> GetCallTrace();

        List<string> GetCodeFragment(int contextLines);
        void RunProgram(List<Opcode> program);
    }
}