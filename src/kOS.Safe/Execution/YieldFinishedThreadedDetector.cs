using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace kOS.Safe.Execution
{
    public abstract class YieldFinishedThreadedDetector : YieldFinishedDetector
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

            TryStartThread();
        }

        private void TryStartThread()
        {
            if (ThreadInitialize())
            {
                childThread = new Thread(DoThread);
                childThread.IsBackground = true;
                childThread.Start();
            }
        }

        public override bool IsFinished()
        {
            // Thread may not be started yet, but this distinguishes between finished and not-started
            // in case IsFinished() gets called multiple times even after it returns true
            if (childThreadEvent.WaitOne(0))
            {
                childThread.Join();
                if (childException == null)
                {
                    try
                    {
                        ThreadFinish();
                    }
                    catch (Exception ex)
                    {
                        childException = ex;
                    }
                }

                // Remove the reference now, before we potentially (re)throw the exception
                childThread = null;

                // Note this is *deliberately* NOT an "else" of the above "if" even though
                // it looks like it should be.  That is because the above IF clause can actually
                // alter this flag and if it does so it needs to fall through to here and do this.
                if (childException != null)
                {
                    // If there was an error in the child thread, break execution and then
                    // throw the exception.  Because we're still executing the same opcode
                    // throwing will be caught and logged exactly the same as any other error
                    // in normal code.  It's important to break execution to ensure the
                    // instruction pointer skips the current opcode that throws the exception.
                    shared.Cpu.BreakExecution(false);
                    throw childException;
                }
                return true;
            }
            // Try starting the thread now, if not started yet
            if (childThread == null)
                TryStartThread();

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
        /// <returns>True if ready to start the thread (false if waiting for some condition, e.g. Processor.CheckCanBoot)</returns>
        protected abstract bool ThreadInitialize();

        /// <summary>
        /// <para>
        /// This method is what actually executes in the background.  It should do the majority of the work.
        /// Remember that the KSP and Unity APIs are not thread safe, so calls to either API should be avoided in this
        /// method.
        /// </para>
        /// <para>
        /// WARNING: THIS METHOD EXECUTES ON A SEPARATE THREAD.  TAKE CARE TO ENSURE THAT IMPLEMENTATIONS ARE THREAD SAFE
        /// </para>
        /// </summary>
        /// <param name="shared"></param>
        protected abstract void ThreadExecute();

        /// <summary>
        /// This method is executed after the child thread is finished, when the CPU checks IsFinished.  It is called from
        /// the main thread and is not required to be thread safe with respect to KSP.
        /// </summary>
        protected abstract void ThreadFinish();
    }
}
