using System;

namespace kOS.Sound
{
    /// <summary>
    /// Generates a random white noise sound wave.
    /// </summary>
    public class NoiseSoundWave : ProceduralSoundWave
    {

        private Random rand;
        private float halfSamplePeriod;

        public NoiseSoundWave() : base()
        {            
        }
        
        public override void InitSettings()
        {
            base.InitSettings();
            // pre-calc this to speed up noiseGenerator() later.
            halfSamplePeriod = SampleRange/2;
            
            rand = new Random();            
        }
        
        public override float SampleFunction(float t)
        {
            // This noise generator is still pretty bad.  You can still
            // hear the 'tone' hidden inside the noise.  I'll work on this
            // more after I get the rest of the system working.
            if (t < halfSamplePeriod)
                return (float)(-1 + 2*(rand.NextDouble()*rand.NextDouble()));
            else
                return (float)(1 - 2*(rand.NextDouble()*rand.NextDouble()));
        }
    }
}
