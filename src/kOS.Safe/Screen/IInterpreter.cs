using kOS.Safe.Utilities;

namespace kOS.Safe.Screen
{
    public interface IInterpreter : IScreenBuffer
    {
        void Type(char ch);
        void SpecialKey(kOSKeys key);
        string GetCommandHistoryAbsolute(int absoluteIndex);
        void SetInputLock(bool isLocked);
        void Reset();
    }
}