using System;
using System.Collections.Generic;
using System.Text;
using kOS.Safe.Execution;

namespace kOS.Safe.Execution
{
    public interface IStack
    {
        void Push(object item);
        object Pop();
        object Peek(int digDepth);
        bool PeekCheck(int digDepth, out object item);
        int GetLogicalSize();
        void MoveStackPointer(int delta);
        void Clear();
        string Dump();
        List<int> GetCallTrace();
    }
}
