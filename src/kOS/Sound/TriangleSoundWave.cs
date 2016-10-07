using UnityEngine;
using System;

namespace kOS.Sound
{
    /// <summary>
    /// A variant of ProceduralSoundWave that emits a triangle wave.
    /// </summary>
    public class TriangleSoundWave : ProceduralSoundWave
    {
        public TriangleSoundWave(): base()
        {
        }
                
        public override float SampleFunction(float t)
        {
            if (t < 0.5)
                return 1 - 2*t;
            else
                return -1 + 2*(t-0.5f);
        }
                                    
    }
}
