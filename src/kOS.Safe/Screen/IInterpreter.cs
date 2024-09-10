using kOS.Safe.Utilities;

namespace kOS.Safe.Screen
{
    public interface IInterpreter : IScreenBuffer
    {
        void Type(char ch);
        bool SpecialKey(char key);
        string GetCommandHistoryAbsolute(int absoluteIndex);
        int GetCommandHistoryIndex();
        void SetInputLock(bool isLocked);
        bool IsAtStartOfCommand();
        bool IsWaitingForCommand();
        void Reset();
    }
}