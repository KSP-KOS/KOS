using System;

namespace kOS.Safe.Execution
{
    /// <summary>
    /// When telling kOS's CPU that it should yield, using the CPU.YieldProgram() method,
    /// you make a new instance of a derivative of this class to manage the decision of
    /// when it should resume again.  kOS's CPU will repeatedly re-check the instance of
    /// this class whenever it wants to execute Opcodes, and only when this class says
    /// so will it resume execution of the Opcodes.<br/>
    /// <br/>
    /// When you make a new instance of this class you should immediately "forget" it
    /// after it is passed to YieldProgram() (i.e. don't hold a reference to it.)  If you
    /// call YieldProgram() again, it should always be a with a new instance of this class.<br/>
    /// <br/>
    /// When you inherit from this class, you should store any data values that are part of
    /// the decision "am I done waiting" as members of this class, such that each new instance
    /// gets its own set of such fields and all instances can decide "am I done" indepentantly
    /// of each other.<br/>
    /// <br/>
    /// The reason all the above instructions are relevant is that they allow the same Opcode,
    /// or Built-in Function to cause more than one YieldProgram to exist similtaneously from
    /// them.<br/>
    /// (i.e. a Wait Opcode inside a function, and that function gets called from both the
    /// mainline code and a trigger.  You want two different wait timers going for them
    /// even though they're coming from the exact same OpcodeWait instance in the program.)<br/>
    /// </summary>
    public abstract class YieldFinishedDetector
    {
        // used for tracking by the CPU - don't mess with it.  It ensures that
        // All yields must at least wait for the next tick, even the ones that
        // return true immediately.
        public double creationTimeStamp {get; set;}
        
        /// <summary>
        /// When the CPU starts the yield, it will call Begin to tell you the shared
        /// objects handle in case you need some information from it, and to let you
        /// get anything you like started up, like a timer for example.<br/>
        /// <br/>
        /// You can be guaranteed that the CPU will call Begin() before it ever calls IsFinished(),
        /// so it's safe for IsFinished() to use information that was obtained from Begin().
        /// </summary>
        /// <param name="shared"></param>
        public abstract void Begin(SafeSharedObjects shared);

        /// <summary>
        /// Track whatever you feel like in this class to decide when the wait is over, but
        /// this is how you tell the CPU the wait is over.  The CPU will call IsFinished()
        /// over and over to find out when it's time to start executing opcodes again.<br/>
        /// <br/>
        /// You can be guaranteed that the CPU will call Begin() before it ever calls IsFinished(),
        /// so it's safe for IsFinished() to use information that was obtained from Begin().
        /// </summary>
        /// <returns>false to keep waiting, true to resume (and stop checking this instance thereafter)</returns>
        public abstract bool IsFinished();
    }
}
