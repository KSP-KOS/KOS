using kOS.Safe.Compilation;
using kOS.Safe.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kOS.Safe.Function
{
    /// <summary>
    /// this exists to make functions made for kerboscript compatible with lua 
    /// </summary>
    public class LuaStackOperator : IStackOperator
    {
        public Stack<object> stack = new Stack<object>();

        public void AssertArgBottomAndConsume(SafeSharedObjects shared, string functionName)
        {
            if (stack.Count==0) return;
            throw new KOSArgumentMismatchException("Too many arguments were passed to " + functionName);
        }

        public int CountRemainingArgs(SafeSharedObjects shared) => stack.Count;

        public object PopValueAssert(SafeSharedObjects shared, string functionName, bool barewordOkay = false)
        {   // if barewordOkay it would transform an identifier into a string if there are no variables with this identifier and return it
            // basically impossible to do with lua without changing lua itself
            if (stack.TryPop(out var res))
                return res;
            else
                throw new KOSArgumentMismatchException("Too few arguments were passed to " + functionName);
        }

        public object PopStackAssert(SafeSharedObjects shared, string functionName)
        {
            if (stack.TryPop(out var res))
                return res;
            else
                throw new KOSArgumentMismatchException("Too few arguments were passed to " + functionName);
        }
    }
}
