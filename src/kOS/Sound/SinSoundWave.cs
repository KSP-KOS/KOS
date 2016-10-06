using UnityEngine;
using System;

namespace kOS.Sound
{
    /// <summary>
    /// A variant of ProceduralSoundWave that emits a sine wave.
    /// </summary>
    public class SinSoundWave : ProceduralSoundWave
    {
        public SinSoundWave(): base()
        {
        }
        
        public override void InitSettings()
        {
            base.InitSettings();
            SampleRange = 2*Mathf.PI;
        }
        
        public override float SampleFunction(float t)
        {
            return Mathf.Sin(t);
        }
                                    
    }
}
