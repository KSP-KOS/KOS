using kOS.Safe.Compilation;
using kOS.Safe.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kOS.Safe.Function
{
    internal class KSStackOperator : IStackOperator
    {
        public void AssertArgBottomAndConsume(SafeSharedObjects shared, string functionName)
        {
            object shouldBeBottom = shared.Cpu.PopArgumentStack();
            if (shouldBeBottom != null && shouldBeBottom.GetType() == OpcodeCall.ArgMarkerType)
                return; // Assert passed.

            throw new KOSArgumentMismatchException("Too many arguments were passed to " + functionName);
        }

        public int CountRemainingArgs(SafeSharedObjects shared)
        {
            int depth = 0;
            bool found = false;
            bool stillInStack = true;
            while (stillInStack && !found)
            {
                object peekItem = shared.Cpu.PeekRawArgument(depth, out stillInStack);
                if (stillInStack && peekItem != null && peekItem.GetType() == OpcodeCall.ArgMarkerType)
                    found = true;
                else
                    ++depth;
            }
            if (found)
                return depth;
            else
                return -1;
        }

        public object PopValueAssert(SafeSharedObjects shared, string functionName, bool barewordOkay = false)
        {
            object returnValue = shared.Cpu.PopValueArgument(barewordOkay);
            if (returnValue != null && returnValue.GetType() == OpcodeCall.ArgMarkerType)
                throw new KOSArgumentMismatchException("Too few arguments were passed to " + functionName);
            return returnValue;
        }

        public object PopStackAssert(SafeSharedObjects shared, string functionName)
        {
            object returnValue = shared.Cpu.PopArgumentStack();
            if (returnValue != null && returnValue.GetType() == OpcodeCall.ArgMarkerType)
                throw new KOSArgumentMismatchException("Too few arguments were passed to " + functionName);
            return returnValue;
        }
    }
}
