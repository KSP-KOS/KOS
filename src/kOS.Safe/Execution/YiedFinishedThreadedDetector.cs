﻿using System;
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

            if (RunOnCaller())
            {
                DoThread();
            }
            else
            {
                childThread = new Thread(DoThread);
                childThread.IsBackground = true;
                childThread.Start();
            }
        }

        public override bool IsFinished()
        {
            if (childThreadEvent.WaitOne(0))
            {
                if (childThread != null)
                {
                    childThread.Join();
                }
                if (childException == null)
                {
                    ThreadFinish();
                }
                else
                {
                    // If there was an error in the child thread, break execution and then
                    // throw the exception.  Because we're still executing the same opcode
                    // throwing will be caught and logged exactly the same as any other error
                    // in normal code.  It's important to break execution to ensure the
                    // instruction pointer skips the current opcode that throws the exception.
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
        public abstract void ThreadExecute();

        /// <summary>
        /// This method is executed after the child thread is finished, when the CPU checks IsFinished.  It is called from
        /// the main thread and is not required to be thread safe with respect to KSP.
        /// </summary>
        public abstract void ThreadFinish();

        /// <summary>
        /// Determines if it must execute DoThread() when Begin() is invoked using caller thread.
        /// <br/>
        /// This method method is used by descendants to tell whether they want to freeze a game.
        /// </summary>
        /// <returns>false to run in separate thread, true to freeze a game</returns>
        protected virtual bool RunOnCaller()
        {
            return true;
        }
    }
}
