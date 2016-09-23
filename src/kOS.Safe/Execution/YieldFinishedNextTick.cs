namespace kOS.Safe.Execution
{
    /// <summary>
    /// A kind of YieldFinishedDetector for when you want to wait literaly just
    /// until the next tick, by returning true unconditionally when asked "are you done".
    /// </summary>
    public class YieldFinishedNextTick : YieldFinishedDetector
    {
        public override void Begin(SafeSharedObjects shared)
        {
        }

        public override bool IsFinished()
        {
            return true;
        }
    }
}