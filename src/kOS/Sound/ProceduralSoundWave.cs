using kOS.Safe.Sound;
using UnityEngine;

namespace kOS.Sound
{
    /// <summary>
    /// Creates an arbitrary audio wave reference example note and populates an AudioClip with it.
    /// <br/>
    /// These are used with the SoundMaker class to provide a means to do crude FM wave sounds.
    /// <br/>
    /// Each of the "FM wave generators" in the kOS computer's "audio chip" is a derived class
    /// of this.
    /// <br/>
    /// To make a new FM wave type, just inherit from this class and override SampleFunction
    /// with your own mathematical function that describes the sound wave in the time domain.
    /// The base class should populate the audio clip for you from that.
    /// </summary>
    public abstract class ProceduralSoundWave : IProceduralSoundWave
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
        protected float SampleVolume { get; set; }

        /// <summary>How many periods worth of sound wave peaks are encoded within the sample from 0..SampleRange.
        /// This effectively sets the base frequency of the sound reference sample.  (The
        /// sample is assumed to be 1 second long, and have this many repeats of the wave pattern in it.)
        /// </summary>
        public int Periods { get; set; }

        /// <summary>Defines the valid domain range of values for t over which
        /// SampleFunction() will get called.  That range will be
        /// [0..SamplePeriod), This is the range of values that
        /// represents one period of the wave.  (For example, it would be
        /// 2*pi for a trig function.)  The default is 1.0f.</summary>
        protected float SampleRange { get; set; }

        /// <summary>
        /// Tracks where we are within the sample function's domain. (what is 't'
        /// that we are inputting into it.)
        /// </summary>
        private float phase = 0f;

        private int sampleRate;

        /// <summary>
        /// Audio clip that was procedurally generated.
        /// The Unity AudioClip of the wave form.  This must be stored as generic object
        /// instead of the right type so that the kOS.Safe.Sound.IVoice can have this in the
        /// interface without being aware of the Unity type AudioClip.
        /// </summary>
        public object Clip { get; set; }

        /// <summary>
        /// Important: when making a derivative of ProceduralSoundWave, put
        /// your initializations of important things inside InitSettings(),
        /// not in your constructor.  Instead make your constructor call
        /// the base constructor and do nothing else.  The base constructor
        /// will call your InitSettings() for you.  This is important
        /// because you need to populate your settings BEFORE the base constructor
        /// tries to call GenerateClip().  If you populated your settings inside your
        /// own constructor, that would be too late.
        /// </summary>
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
            Periods = 1000;
        }

        /// <summary>
        /// Override this in your derived class to make a wave generator of
        /// whatever type you like.  It should be a time-domain picture of
        /// the sound wave over one wave period, outputting values from -1 to +1 for a given time t.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public virtual float SampleFunction(float t)
        {
            return 0;
        }

        /// <summary>
        /// Generates the AudioClip (happens as part of the constructor).<br/>
        /// It does this by querying your SampleFunction() to build a reference
        /// wave in memory.
        /// </summary>
        private void GenerateClip()
        {
            float[] dataBuffer;
            int channels = 1;
            //let's assume mono for now.
            string waveName = this.GetType().Name;

            Clip = AudioClip.Create(waveName, sampleRate, channels, sampleRate, false);
            dataBuffer = new float[AudioSettings.outputSampleRate * channels];
            FillBuffer(dataBuffer, channels);
            ((AudioClip)Clip).SetData(dataBuffer, 0);
        }

        /// <summary>
        /// The workhorse under the hood for GenerateClip().
        /// </summary>
        /// <param name="sampleData"></param>
        /// <param name="numChannels"></param>
        private void FillBuffer(float[] sampleData, int numChannels)
        {
            float phaseInc = Periods * SampleRange / (float)sampleRate;
            // increment phase this far per sample point.
            float sample;
            int index = 0;
            while (index < sampleData.Length)
            {
                sample = SampleVolume * SampleFunction(phase);

                // Duplicate sample across all channels (i.e. send same mono data to both left and right speaker):
                for (int channel = 0; channel < numChannels; ++channel)
                    sampleData[index++] += sample;
                // add to whatever might already be there, which is probably zero.
                phase = (phase + phaseInc) % SampleRange;
            }
        }
    }
}