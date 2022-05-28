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
        /// Value from 0.0f to 1.0f for the volume of this <see cref="Voice"/>.
        /// The final volume is computed by multiplying this value with <see cref="soundVolume"/> as specified by <see cref="BeginProceduralSound"/> and the ADSR envelope.
        /// </summary>
        public float Volume { get; set; }

        /// <summary>
        /// Value from 0.0f to 1.0f for the sound being played, as it is specified by <see cref="BeginProceduralSound"/>.
        /// </summary>
        private float soundVolume { get;set;}

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
        
        /// <summary>
        /// If we notice the game is paused, record the timestamp where the pausing began.
        /// Also used as a flag - set it to negative to indicate we're not currently paused.
        /// </summary>
        private float freezeBeganTimestamp = -1f;
        
        /// <summary>
        /// What the voice volume had been when we began the freeze
        /// </summary>
        private float volumeBeforeFreeze = 0f;

        public void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.loop = true;
            source.spatialBlend = 0; // Makes it ignore spatial position for calculating sound.
            source.bypassListenerEffects = true; // Makes other mods like Rocket Sound Enhancement not affect it.

            // Dummy test stupid values:
            Attack = 0f;
            Decay = 0f;
            Sustain = 1f;
            Release = 0.1f;
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
        /// <returns>false if frequency is less than zero, indicating a rest</returns>
        public bool BeginProceduralSound(IProceduralSoundWave waveGen, float frequency, float duration, float volume = 1f)
        {
            SetWave(waveGen); // update the wave even if called for a rest note
            if (frequency > 0)
            {
                soundVolume = volume;
                noteAttackStart = Time.unscaledTime;
                noteReleaseStart = Time.unscaledTime + duration;
                needNoteInit = true;
                ChangeFrequency(frequency);
                return true;
            }
            return false;
        }


        /// <summary>
        /// Begin a single note.
        /// You can pass in a frequency and it will "stretch" the reference sample
        /// to make it fit the given frequency.
        /// </summary>
        /// <param name="frequency">the note, expressed in Hertz (not musical scales)</param>
        /// <param name="duration">the note's duration, in seconds.</param>
        /// <param name="volume">the note's volume, from 0.0 up to 1.0</param>
        /// <returns>false if frequency is less than zero, indicating a rest</returns>
        public bool BeginProceduralSound(float frequency, float duration, float volume = 1f)
        {
            if (frequency > 0)
            {
                soundVolume = volume;
                noteAttackStart = Time.unscaledTime;
                noteReleaseStart = Time.unscaledTime + duration;
                needNoteInit = true;
                ChangeFrequency(frequency);
                return true;
            }
            return false;
        }

        public void Stop()
        {
            // We set the volume to 0 and stop the source instead of changing the pitch
            // to 0 because technically AudioSource doesn't want a pitch of 0, and this
            // will close the audio channel entirely preventing sound artifacts.
            source.volume = 0;
            source.Stop();
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
        public void Update()
        {
            // A note about which time clock to use for the notes:
            // There are several choices for which Unity clock to use to time the notes,
            // and it's a stylistic choice.  We narrowed it down to these two:
            //
            // Time.time
            //     This is the "in character" time.  In other words, under 4x physics warp
            //     it moves 4x faster, and in times of high part count lag, it moves slower.
            //     If we used this to time the notes then songs would go faster or slower with
            //     the game universe.  While this is correct from a simulation point of view,
            //     if we think of the SKID chip being an object in the Kerbal's universe,
            //     it makes for annoying user interfaces.
            //
            // Time.unscaledTime
            //     This is the "out of character" time.  In other words, it moves at the
            //     same speed whether under physics warp or not.  If you make a song list,
            //     the song will play at the same tempo regardless of physics warp or game
            //     lag.
            //
            // We decided to go with Time.unscaledTime.
            //
            // If you want to see what it's like to move the sound with the physics speed,
            // you can replace all the instances of Time.unscaledTime with Time.Time within
            // this file and within the VoiceValue.cs file.
            // We decided in the end that its better to make the sound seem right to the player
            // than to make it realistic to the little Kerbals in their fast-moving universe.

            if (needNoteInit)
                InitNote();

            if (!source.isPlaying)
                return;

            if (Waveform == null)
                return;

            if (Time.timeScale == 0f) // game is paused (i.e. the Escape Menu is up.)
            {
                if (freezeBeganTimestamp < 0f) // And we were not previously paused, so starting a new pause here.
                {
                    freezeBeganTimestamp = Time.unscaledTime;
                    volumeBeforeFreeze = source.volume;
                    source.volume = 0f;
                }
                return; // don't do any of the normal work when the game is paused.
            }
            else // game is not paused
            {
                if (freezeBeganTimestamp >= 0f) // But it had been before, so it's just coming out of pause now.
                {
                    // Before we continue, update the timestamp clocks to account for all
                    // that paused time so notes can continue where they left off:
                    float freezeDuration = Time.unscaledTime - freezeBeganTimestamp;
                    noteAttackStart += freezeDuration;
                    noteReleaseStart += freezeDuration;
                    freezeBeganTimestamp = -1f;
                    source.volume = volumeBeforeFreeze;
                }
            }


            float now = Time.unscaledTime;
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
            source.volume = GameSettings.UI_VOLUME * Volume * soundVolume * envelopeVolume;
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