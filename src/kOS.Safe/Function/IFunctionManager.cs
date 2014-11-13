namespace kOS.Safe.Function
{
    public interface IFunctionManager
    {
        void Load();
        void CallFunction(string functionName);
    }
}