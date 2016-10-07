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
            // Use a Sine wave to limit the amplitude of the
            // random noise samples, so the sample has a
            // detectable pitch to it:
            return (float)(Math.Sin(t)*rand.NextDouble());
        }
    }
}
