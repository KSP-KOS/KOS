using kOS.Safe.Utilities;

namespace kOS.Safe.Screen
{
    public interface IInterpreter : IScreenBuffer
    {
        void Type(char ch);
        void SpecialKey(char key);
        string GetCommandHistoryAbsolute(int absoluteIndex);
        void SetInputLock(bool isLocked);
        bool IsAtStartOfCommand();
        void Reset();
    }
}