using System;

namespace kOS.Safe.Utilities
{
    /// <summary>
    /// The purpose of this class is to always return a new, bigger number each time you ask it
    /// for a number.  That's all.  Its meant to be used to timestamp the relative age of things.
    /// Bigger numbers were returned later in the life of the program than smaller ones.
    /// It could potentially overflow, but only if the program ran nonstop without crashing for
    /// a ludicrious amount of time.  (Uint64 can store a number more than two orders of magnitude
    /// larger than the number of NANOseconds in a year.)
    /// </summary>
    public class TickGen
    {
        private static UInt64 tick = 0;

        /// <summary>Get the next tick higher number every time this property is queried.</summary>
        ///
        public static UInt64 Next { get { return ++tick; } }
        
        /// <summary>Get the current number without incrementing the ticker.</summary>
        /// 
        public static UInt64 Current { get { return tick; } }
    }
}
