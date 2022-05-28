using System;

namespace kOS.Safe.Communication
{
    /// <summary>
    /// A class implementing this will provide the current time when asked, using whatever
    /// type of clock it chooses to (i.e. in-game time, wall clock time, etc.)  Instances
    /// of this class MUST also implement a default constructor, although a C# compiler cannot
    /// enforce that rule.
    /// </summary>
    public interface CurrentTimeProvider
    {
        //ClassName() constructor (default constructor) must also exist in any
        //class implementing this, but C# compilers cannot enforce that rule
        //for an interface.  But trust us, it has to or it won't work.

        /// <summary>Returns the current time in seconds, in whatever time scheme this class uses.</summary>
        double CurrentTime();

        /// <summary>
        /// If allowed, this will reset the clock to a new value.  Note, not all
        /// CurrentTimeProviders have to allow this.  They can just ignore this call
        /// and do nothing when it is invoked.
        /// </summary>
        void SetTime(double newTime);
    }
}

