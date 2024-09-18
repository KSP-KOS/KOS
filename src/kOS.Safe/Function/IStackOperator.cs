using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kOS.Safe.Function
{
    public interface IStackOperator
    {
        /// <summary>
        /// A utility function that a function's Execute() must use after it has popped all the
        /// arguments it was expecting from the stack.  It will assert that all the arguments
        /// have been consumed exactly, and the next item on the stack is the arg bottom mark.
        /// It will consume the arg bottom mark as well.
        /// <br/>
        /// If the assert fails, an exception is thrown.
        /// </summary>
        /// <param name="shared"></param>
        void AssertArgBottomAndConsume(SafeSharedObjects shared, string functionName);

        /// <summary>
        /// A utility function that a function's Execute() may use if it wishes to, to get a count of
        /// how many args passed to it that it has not yet consumed still remain on the stack.
        /// </summary>
        /// <param name="shared"></param>
        /// <returns>Number of args as yet unpopped.  returns zero if there are no args, or -1 if there's a bug and the argstart marker is missing.</returns>
        int CountRemainingArgs(SafeSharedObjects shared);

        /// <summary>
        /// A utility function that a function's Execute() should use in place of cpu.PopValue(),
        /// because it will assert that the value being popped is NOT an ARG_MARKER_STRING, and if it
        /// is, it will throw the appropriate error.
        /// </summary>
        /// <returns></returns>
        object PopValueAssert(SafeSharedObjects shared, string functionName, bool barewordOkay = false);

        /// <summary>
        /// A utility function that a function's Execute() should use in place of cpu.PopArgumentStack(),
        /// because it will assert that the value being popped is NOT an ARG_MARKER_STRING, and if it
        /// is, it will throw the appropriate error.
        /// </summary>
        /// <returns></returns>
        object PopStackAssert(SafeSharedObjects shared, string functionName);
    }
}
