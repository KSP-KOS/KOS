namespace kOS.Safe.Function
{
    public interface IFunctionManager
    {
        void Load();
        void CallFunction(string functionName);
        bool Exists(string functionName);
        bool IsFunctionInvariant(string functionName);
        bool IsFunctionInert(string functionName);
        System.Type FunctionReturnType(string functionName);
    }
}