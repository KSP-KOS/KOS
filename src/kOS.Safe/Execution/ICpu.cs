using System.Collections.Generic;
using kOS.Safe.Compilation;

namespace kOS.Safe.Execution
{
    public interface ICpu
    {
        void PushStack(object item);
        object PopStack();
        void MoveStackPointer(int delta);
        object GetValue(object testValue);
        object PopValue();
        object PeekValue(int digDepth);        
        int GetStackSize();
        void SetValue(string identifier, object value);
        void DumpVariables();
        void RemoveVariable(string identifier);
        void RemoveAllVariables();
        int InstructionPointer { get; set; }
        double SessionTime { get; }
        void AddTrigger(int triggerFunctionPointer);
        void RemoveTrigger(int triggerFunctionPointer);
        void StartWait(double waitTime);
        void EndWait();
        void CallBuiltinFunction(string functionName);
        void BreakExecution(bool manual);
        void AddVariable(Variable variable, string identifier);
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