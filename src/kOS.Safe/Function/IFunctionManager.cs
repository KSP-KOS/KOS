namespace kOS.Safe.Function
{
    public interface IFunctionManager
    {
        void Load();
        void CallFunction(string functionName);
        bool Exists(string functionName);
        bool IsFunctionInvariant(string functionName);
        System.Type FunctionReturnType(string functionName);
    }
}