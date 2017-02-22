using UnityEngine;
using System;

namespace kOS.Sound
{
    /// <summary>
    /// A variant of ProceduralSoundWave that emits a sawtooth shaped wave.
    /// </summary>
    public class SawtoothSoundWave : ProceduralSoundWave
    {
        public SawtoothSoundWave(): base()
        {
        }
                
        public override float SampleFunction(float t)
        {
            // if (t >= 0.5f)   //eraseme
            //    t -= 0.5f;    //eraseme
            // return 2*t - 1;  //eraseme
            return 1 - 2*t;
        }

    }
}
