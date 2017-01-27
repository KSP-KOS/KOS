using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace kOS.Safe.Execution
{
    public abstract class YiedFinishedThreadedDetector : YieldFinishedDetector
    {
        private ManualResetEvent childThreadEvent;
        private Thread childThread;
        private Exception childException;

        protected SafeSharedObjects shared;

        public override void Begin(SafeSharedObjects sharedObj)
        {
            if (childThread != null)
                throw new Exceptions.KOSException("Error calling Begin, childThread is not null.");

            shared = sharedObj;

            childThreadEvent = new ManualResetEvent(false);

            ThreadInitialize(sharedObj);

            childThread = new Thread(DoThread);
            childThread.IsBackground = true;
            childThread.Start();
        }

        public override bool IsFinished()
        {
            if (childThreadEvent.WaitOne(0))
            {
                childThread.Join();
                if (childException == null)
                {
                    ThreadFinsh();
                }
                else
                {
                    // If it died due to a compile error, then we won't really be able to switch to program context
                    // as was implied by calling Cpu.SwitchToProgramContext() up above.  The CPU needs to be
                    // told that it's still in interpreter context, or else it fails to advance the interpreter's
                    // instruction pointer and it will just try the "call run()" instruction again:
                    shared.Cpu.BreakExecution(false);
                    throw childException;
                }
                childThread = null;
                return true;
            }
            return false;
        }

        private void DoThread()
        {
            try
            {
                ThreadExecute();
            }
            catch (Exception ex)
            {
                childException = ex;
            }
            childThreadEvent.Set();
        }

        /// <summary>
        /// This method is executed before starting the child thread.  It is called from the main thread and is not required
        /// to be thread safe with respect to KSP.
        /// </summary>
        /// <param name="shared"></param>
        public abstract void ThreadInitialize(SafeSharedObjects shared);

        /// <summary>
        /// WARNING: THIS METHOD EXECUTES ON A SEPARATE THREAD.  TAKE CARE TO ENSURE THAT IMPLEMENTATIONS ARE THREAD SAFE
        /// </summary>
        /// <param name="shared"></param>
        public abstract void ThreadExecute();

        /// <summary>
        /// This method is executed after the child thread is finished, when the CPU checks IsFinished.  It is called from
        /// the main thread and is not required to be thread safe with respect to KSP.
        /// </summary>
        public abstract void ThreadFinsh();
    }
}
