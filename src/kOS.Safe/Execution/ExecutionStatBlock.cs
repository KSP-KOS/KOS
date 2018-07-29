using System;
using System.Diagnostics;

namespace kOS.Safe
{
    /// <summary>
    /// Remembers the statistics about Opcodes executed in a particular priority level.
    /// There should be one of these per priority level being tracked.
    /// </summary>
    public class ExecutionStatBlock
    {
        public int TotalUpdates {get; private set;}

        /// <summary>"ticks" as in stopwatch clock ticks</summary>
        public long TotalTicks {get; private set;}
        public double TotalMilliseconds {get { return ticksToSeconds(TotalTicks); } }
        /// <summary>"ticks" as in stopwatch clock ticks</summary>
        public long MaxTicksInOneUpdate {get; private set;}
        public double MaxMillisecondsInOneUpdate {get { return ticksToSeconds(MaxTicksInOneUpdate); } }
        /// <summary>"ticks" as in stopwatch clock ticks</summary>
        public double MeanTicksPerUpdate { get { return TotalTicks / (double)TotalUpdates; } }
        public double MeanMillisecondsPerUpdate {get { return ticksToSeconds(MeanTicksPerUpdate); } }

        public int TotalInstructions {get; private set;}
        public int MaxInstructionsPerUpdate {get; private set;}
        public double MeanInstructionsPerUpdate { get { return TotalInstructions / (double)TotalUpdates; } }

        /// <summary>"ticks" as in stopwatch clock ticks</summary>
        private long ticksThisUpdate;
        private int instructionsThisUpdate;

        /// <summary>
        /// True if a call to LogOneInstruction() has happened more recently than the
        /// most recent EndOneUpdate() call.  That means there is still one update who's
        /// stats aren't finished off yet, and the end update needs to happen before anyone
        /// tries reading the values.
        /// </summary>
        private bool updateIsOpen;

        private static double ticksToSeconds(long ticks)
        {
            return ticks * 1000D / Stopwatch.Frequency;
        }
        private static double ticksToSeconds(double ticks)
        {
            return ticks * 1000D / Stopwatch.Frequency;
        }

        public void Clear()
        {
            TotalUpdates = 0;
            TotalTicks = 0L;
            MaxTicksInOneUpdate = 0L;
            TotalInstructions = 0;
            MaxInstructionsPerUpdate = 0;

            instructionsThisUpdate = 0;
            ticksThisUpdate = 0L;
        }

        public void LogOneInstruction(long elapsed)
        {
            ++instructionsThisUpdate;
            ticksThisUpdate += elapsed;
            updateIsOpen = true;
        }

        public void EndOneUpdate()
        {
            ++TotalUpdates;

            TotalInstructions += instructionsThisUpdate;
            TotalTicks += ticksThisUpdate;

            if (MaxInstructionsPerUpdate < instructionsThisUpdate)
                MaxInstructionsPerUpdate = instructionsThisUpdate;

            if (MaxTicksInOneUpdate < ticksThisUpdate)
                MaxTicksInOneUpdate = ticksThisUpdate;

            instructionsThisUpdate = 0;
            ticksThisUpdate = 0L;

            updateIsOpen = false;
        }

        public void SealHangingUpdateIfAny()
        {
            if (updateIsOpen)
                EndOneUpdate();
        }
    }
}

