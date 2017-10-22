using System.Collections.Generic;

namespace kOS.Safe.Execution
{
    public interface IStack
    {
        void Push(object item);
        object Pop();
        object Peek(int digDepth);
        bool PeekCheck(int digDepth, out object item);
        void PushScope(object item);
        object PopScope();
        int GetLogicalSize();
        void Clear();
        string Dump();
        List<int> GetCallTrace();
        bool HasTriggerContexts();
    }
}
