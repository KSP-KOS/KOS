using kOS.Safe.Encapsulation;
using System.Collections.Generic;
using System;

namespace kOS.Safe.Execution
{
    /// <summary>
    /// Description of YieldFinishedGetChar.
    /// </summary>
    public class YieldFinishedGetChar : YieldFinishedDetector
    {
        private SafeSharedObjects shared;

        public override void Begin(SafeSharedObjects shared)
        {
            this.shared = shared;
        }

        public override bool IsFinished()
        {
            Queue<char> q = shared.Screen.CharInputQueue;

            if (q.Count > 0)
            {
                char ch = q.Dequeue();

                // Replace the dummy return value the suffix
                // left atop the stack with the real return value.
                // Now that we're done waiting, the next Opcode
                // (that expects the expression atop the stack to be
                // the char that was read) is going to execute:
                shared.Cpu.PopStack();
                shared.Cpu.PushStack(new StringValue(ch));

                return true;
            }
            else
                return false;
        }
    }
}
