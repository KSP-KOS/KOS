using System;

namespace kOS.Sound
{
    /// <summary>
    /// A variant of ProceduralSoundWave that just makes a dumb square waveform.
    /// </summary>
    public class SquareSoundWave : ProceduralSoundWave
    {
        /// <summary>
        /// What portion of the total period is spent with
        /// the square wave in its "high" state?  Default is
        /// 0.5, meaning half the time it's high, and half
        /// it's low.  Setting it to, say, 0.1f would mean
        /// it spends 90% of it's time in the "low" state and 10%
        /// in the "high" state.
        /// </summary>
        public float HighPortion {get; set;}

        public SquareSoundWave() : base()
        {
        }
        
        public override void InitSettings()
        {
            base.InitSettings();
            HighPortion = 0.5f;
        }
        
        public override float SampleFunction(float t)
        {
            if (t < HighPortion)
                return 1;
            else
                return -1;
        }
    }
}
