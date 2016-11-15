using kOS.Safe.Sound;
using UnityEngine;

namespace kOS.Sound
{
    /// <summary>
    /// Represents one of the hardware voices in the "sound chip", and its settings.
    /// </summary>
    public class Voice : MonoBehaviour, IVoice
    {
        /// <summary>
        /// In Hertz, for the current note that is playing.
        /// </summary>
        public float Frequency { get; set; }

        /// <summary>
        /// Value from 0.0f to 1.0f for the current *overall* volume (which gets adjusted by ADSR envelope settings).
        /// </summary>
        public float Volume { get; set; }

        /// <summary>
        /// Duration in seconds of the "Attack" part of the sound envelope this voice is currently using.
        /// </summary>
        public float Attack { get; set; }

        /// <summary>
        /// Duration in seconds of the "Decay" part of the sound envelope this voice is currently using.
        /// </summary>
        public float Decay { get; set; }

        /// <summary>
        /// Volume coefficient for the "Sustain" part of the sound envelope.  (i.e. 0.75f means the sustain level is 75% of max peak level.)
        /// </summary>
        public float Sustain { get; set; }

        /// <summary>
        /// Duration in seconds of the "Release" part of the sound envelope this voice is currently using.
        /// </summary>
        public float Release { get; set; }

        /// <summary>
        /// The base waveform this voice is currently using.
        /// </summary>
        public IProceduralSoundWave Waveform { get; set; }

        /// <summary>
        /// What is the UT timestamp where this note began?
        /// </summary>
        public float noteAttackStart { get; set; }

        /// <summary>
        /// What is the UT timestamp where this note should stop holding (sustaining)?
        /// Note that the Release time still occurs after this.
        /// </summary>
        public float noteReleaseStart { get; set; }

        /// <summary>
        /// True if the next thing to do in a fixed update is to start a new note and set it up:
        /// </summary>
        private bool needNoteInit = false;

        private AudioSource source;

        public void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.loop = true;

            // Dummy test stupid values:
            Attack = 0f;
            Decay = 0f;
            Sustain = 1f;
            Release = 0f;
            Volume = 1f;
        }

        /// <summary>
        /// Begin a single note from a ProceduralSoundWave reference sample.
        /// You can pass in a frequency and it will "stretch" the reference sample
        /// to make it fit the given frequency.
        /// </summary>
        /// <param name="waveGen">a procedural sound wave loaded and prepped ahead of time</param>
        /// <param name="frequency">the note, expressed in Hertz (not musical scales)</param>
        /// <param name="duration">the note's duration, in seconds.</param>
        /// <param name="volume">the note's volume, from 0.0 up to 1.0</param>
        /// <returns>false if the sound name given doesn't seem to be found or it is but it was
        /// a sound file not a procedural sound.</returns>
        public bool BeginProceduralSound(IProceduralSoundWave waveGen, float frequency, float duration, float volume = 1f)
        {
            //Frequency = frequency;
            Volume = volume;
            Waveform = waveGen;
            noteAttackStart = Time.fixedTime;
            noteReleaseStart = Time.fixedTime + duration;
            needNoteInit = true;
            ChangeFrequency(frequency);
            return true;
        }

        public bool BeginProceduralSound(float frequency, float duration, float volume = 1f)
        {
            //Frequency = frequency;
            Volume = volume;
            noteAttackStart = Time.fixedTime;
            noteReleaseStart = Time.fixedTime + duration;
            needNoteInit = true;
            ChangeFrequency(frequency);
            return true;
        }

        public void Stop()
        {
            source.pitch = 0;
            source.volume = 0;
            //source.Stop();
        }

        public void SetWave(IProceduralSoundWave waveGen)
        {
            Waveform = waveGen;
            source.Stop(); // stop the source so that the new wave gets queued up
            needNoteInit = true; // re-sample the pitch value
        }

        /// <summary>
        /// Maintains the volume level in accordance to the ADSR envelope rules,
        /// and the note's holding duration.
        /// </summary>
        public void FixedUpdate()
        {
            if (needNoteInit)
                InitNote();

            if (!source.isPlaying)
                return;

            if (Waveform == null)
                return;

            float now = Time.fixedTime;
            float stepStart = noteAttackStart;

            if (now < stepStart)
                return;

            float stepStop = noteAttackStart + Attack;
            float envelopeVolume = 1f;

            // If in the attack step:
            if (now < stepStop)
            {
                envelopeVolume = (now - stepStart) / (stepStop - stepStart);
            }
            else
            {
                // If in the decay step:
                stepStart = stepStop;
                stepStop = stepStart + Decay;
                if (now < stepStop)
                {
                    envelopeVolume = 1 - (1 - Sustain) * (now - stepStart) / (stepStop - stepStart);
                }
                else
                {
                    // If in the sustain step:
                    stepStart = stepStop;
                    stepStop = noteReleaseStart;
                    if (now < stepStop)
                    {
                        envelopeVolume = Sustain;
                    }
                    else
                    {
                        // If in the release step:
                        stepStart = stepStop;
                        stepStop = stepStart + Release;
                        if (now < stepStop)
                        {
                            envelopeVolume = Sustain * (1 - (now - stepStart) / (stepStop - stepStart));
                        }
                        else
                        {
                            // Note is fully over and faded to zero volume: we can stop it entirely now.
                            Stop();
                        }
                    }
                }
            }
            source.volume = GameSettings.UI_VOLUME * Volume * envelopeVolume;
        }

        /// <summary>Called whenever a new note starts, to get the AudioSource and ProceduralSoundWave
        /// to use the note's values, and then start the note.</summary>
        private void InitNote()
        {
            // Keep playing the same sound if it's already playing.
            if (!source.isPlaying)
            {
                source.clip = (AudioClip)Waveform.Clip;
                source.Play();
            }
            needNoteInit = false;
        }

        /// <summary>
        /// Change the frequency of the note in mid-note (for slide effects).
        /// </summary>
        /// <param name="newFrequency"></param>
        public void ChangeFrequency(float newFrequency)
        {
            // Speed up or slow down the playback rate to give the desired frequency:
            // (AudioSource.pitch is actually just a speed multiplier, not the raw pitch.
            // For example, AudioSource.pitch of 2.0 means "play the sound twice as
            // fast by skipping every other sample point.":
            Frequency = newFrequency;
            source.pitch = Frequency / Waveform.Periods;
        }
    }
}