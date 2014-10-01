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
        void RemoveVariable(string identifier);
        void RemoveAllVariables();
        int InstructionPointer { get; set; }
        void AddTrigger(int triggerFunctionPointer);
        void RemoveTrigger(int triggerFunctionPointer);
        void StartWait(double waitTime);
        void EndWait();
        void CallBuiltinFunction(string functionName);
    }
}