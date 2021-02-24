namespace kOS.Safe.Function
{
    public interface IFunctionManager
    {
        void Load(string[] contexts);
        void CallFunction(string functionName);
        bool Exists(string functionName);
    }
}