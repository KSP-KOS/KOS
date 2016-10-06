using System.Collections.Generic;
using UnityEngine;
using System;

namespace kOS.Sound
{
    /// <summary>
    /// Creates arbitrary audio wave data and populates a "clip" with it.
    /// <br/>
    /// Note it can't literally be an AudioClip because AudioClip is a sealed class.
    /// <br/>
    /// Rather it's a MonoBehaviour that includes an AudioClip inside it.
    /// </summary>
    public abstract class ProceduralSoundWave
    {
        /// <summary>
        /// To cause the waveform generated to not use the entire
        /// amplitude range, you can set this to a value smaller than 1.
        /// This is mostly only useful if you want to superimpose two
        /// waveforms on each other and don't want to lose the high
        /// values when they add onto each other and go past 1.0.  For
        /// simple sounds composed of only one wave, you should not mess
        /// with this, instead adjusting the volume in AudioSource when you
        /// play the waveform.
        /// </summary>
        protected float SampleVolume {get; set;}
        
        /// <summary>How many periods worth of data are encoded within the sample from 0..SampleRange</summary>
        public int Periods {get; set;}
        
        /// <summary>Defines the valid domain range of values for t over which
        /// SampleFunction will be called.  That range will be
        /// [0..SamplePeriod), You'll want this to also be the
        /// range of values for t that defines one single period of the
        /// output note.  For example if you are making a sine wave
        /// generator, you will want this to be 2*pi.</summary>
        protected float SampleRange {get; set;}

        /// <summary>
        /// Tracks where we are within the sample function's domain. (what is 't'
        /// that we are inputting into it.)
        /// </summary>
        private float phase = 0f;

        private int sampleRate;
        
        /// <summary>
        /// Audio clip that was procedurally generated.
        /// </summary>
        public AudioClip Clip {get; set;}

        public ProceduralSoundWave()
        {            
            sampleRate = AudioSettings.outputSampleRate;

            InitSettings();

            GenerateClip();
        }

        /// <summary>Meant to be overridden if a derived type has to change settings
        /// before GenerateClip() is called.<br/>
        /// <br/>
        /// IMPORTANT:  When making a derivative of ProceduralSoundWave, put your
        /// initializations HERE not in the constructor, because the base constructor
        /// will call this before it does things, where if you set them in your 
        /// derived constructor, then GenerateClip() would be getting called before
        /// your settings in your constructor happened.<br/>
        /// <br/>
        /// If you override it, be sure to explicitly call the base version too.</summary>
        public virtual void InitSettings()
        {
            SampleVolume = 1f;
            SampleRange = 1f;
            Periods = 500;
        }
        
        /// <summary>
        /// Override this in your subclass to make a wave generator of 
        /// whatever type you like.  It should be a time-domain picture of
        /// the sound wave, outputting values from -1 to +1 for a given time t.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public virtual float SampleFunction(float t)
        {
            return 0;
        }
        
        protected void GenerateClip()
        {
            float[] dataBuffer;
            int channels = 1; //let's assume mono for now.
            string waveName = this.GetType().Name;
            
            Clip = AudioClip.Create(waveName, sampleRate, channels, sampleRate, false);
            dataBuffer = new float[AudioSettings.outputSampleRate * channels];
            FillBuffer(dataBuffer, channels);            
            Clip.SetData(dataBuffer, 0);

            Console.WriteLine("==== GenerateClip(), sample dump for {0} =====", waveName); // eraseme
            for( int i = 0; i < dataBuffer.Length; ++i) //eraseme
            { // eraseme
                Console.WriteLine("[{0:D5}] = {1}", i, dataBuffer[i]); // eraseme
            } // eraseme
        }

        /// <summary>
        /// Whenever a MonoBehaviour with an OnAudioFilterRead is present in the scene,
        /// any audio data sample chunk gets filtered through OnAudioFilterRead() on its way
        /// into the AudioSource for playing.  Thus objects in the game get to edit the
        /// audio being played before it goes out.  (i.e. imagine a mask that when you
        /// wear it adds static to everything you speak.)<br/>
        /// <br/>
        /// If there is no audio effect happening yet, then this is still called, but it starts with a
        /// "silent" sample (a sample of all zeros) for OnAudioFilterRead to "edit".<br/>
        /// <br/>
        /// Thus you can "edit" the input audio samples by adding your wave to them
        /// and end up emitting your own new procedural sound from scratch.
        /// This is the technique used here.
        /// </summary>
        /// <param name="sampleData">input sample chunk to "edit".  Changing
        /// the values in this array will cause new sound to appear.  Unity has
        /// a thread that asks for the next "chunk" of sample data this way every
        /// so often, but there is no guarantee that it will ask for the same
        /// uniform sized chunk every time this is called.</param>
        /// <param name="numChannels">the input sample can come in stereo channels.
        /// If it does, then the format is you get in sampleData packs all the channels'
        /// samples together.. i.e.
        ///    sampleData[0] = sample 0 of channel 0,
        ///    sampleData[1] = sample 0 of channel 1,
        ///    sampleData[2] = sample 1 of channel 0,
        ///    sampleData[3] = sample 1 of channel 1,
        ///    sampleData[4] = sample 2 of channel 0,
        ///    sampleData[5] = sample 2 of channel 1
        /// etc...
        /// </param>
        /// 
        /// TODO: Completely replace the above comment - it's out-dated.
        /// 
        public void FillBuffer(float[] sampleData, int numChannels)
        {
            float phaseInc = Periods*SampleRange/(float)sampleRate; // increment phase this far per sample point.
            float sample;
            int index = 0;
            while (index < sampleData.Length)
            {
                sample = SampleVolume * SampleFunction(phase);

                // Duplicate sample across all channels (i.e. send same mono data to both left and right speaker):
                for (int channel = 0; channel < numChannels ; ++channel)
                    sampleData[index++] += sample; // add to whatever might already be there, which is probably zero.
                
                phase = (phase + phaseInc) % SampleRange;
            }
        }
    }
}
