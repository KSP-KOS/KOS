namespace kOS.Safe.Execution
{
    /// <summary>
    /// A kind of YieldFinishedDetector for use when you just want a 'dumb' timer
    /// based on the KSP game clock (note: uses the game's notion of simulated time rather
    /// than the real-time wall clock.  I.e. the timer is frozen for the duration of
    /// a FixedUpdate, and doesn't move at all if the game is paused, etc.).<br/>
    /// <br/>
    /// You'd have to make a slightly different derivative of YieldFinishedDetector in
    /// order to base your timer on the out-of-game wall clock.<br/>
    /// </summary>
    public class YieldFinishedGameTimer : YieldFinishedDetector
    {
        private SafeSharedObjects shared;
        private double endTime;

        /// <summary>
        /// Make a new timer that will expire after the given number of seconds
        /// have passed in game-clock time (not real-world time).
        /// </summary>
        /// <param name="shared"></param>
        /// <param name="duration"></param>
        public YieldFinishedGameTimer(double duration)
        {
            // This is temporarily incorrect until Begin() gets called.
            // It will be added to current time in the Begin() method to get the real endTime.
            endTime = duration;
        }

        /// <summary>
        /// Need to track the shared object in order to query current game time.  The timer
        /// starts "counting" from where the clock was when the CPU calls this (which it does
        /// as soon as you do a YieldProgram() call.)
        /// </summary>
        /// <param name="shared"></param>
        public override void Begin(SafeSharedObjects shared)
        {
            this.shared = shared;
            endTime += shared.UpdateHandler.CurrentFixedTime;
        }

        /// <summary>
        /// Return true if the timer ran out, or false if the timer has not run out.
        /// </summary>
        public override bool IsFinished()
        {
            return shared.UpdateHandler.CurrentFixedTime >= endTime;
        }
    }
}