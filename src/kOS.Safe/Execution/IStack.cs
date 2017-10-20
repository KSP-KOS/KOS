using System.Collections.Generic;

namespace kOS.Safe.Execution
{
    public interface IStack
    {
        void Push(object item);
        object Pop();
        void PushAbove(object item);
        object PopAbove();
        object Peek(int digDepth);
        bool PeekCheck(int digDepth, out object item);
        int GetLogicalSize();
        void Clear();
        string Dump();
        List<int> GetCallTrace();
        bool HasTriggerContexts();
    }
}
