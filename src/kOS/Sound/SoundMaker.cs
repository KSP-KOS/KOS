using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using kOS.Safe.Sound;

namespace kOS.Sound
{
    /// <summary>
    /// SoundMaker is the "unsafe" implementation of ISoundMaker, that has calls into the Unity API.
    /// </summary>
    public class SoundMaker : MonoBehaviour, ISoundMaker
    {
        private string kspDirectory;
        private Dictionary<string, AudioSource> sounds;
        private Voice[] voices;
        private Dictionary<string, ProceduralSoundWave> waveGenerators;
        private SharedObjects shared;

        /// <summary>
        /// This top-level game object holds the AudioSources the kOS PartModule will use.
        /// See the initialization comment in SoundMaker.Awake() for why.
        /// </summary>
        private GameObject soundObject;

        /// <summary>
        /// Our pretend hardware limit on a "SKID" chip's number of voices
        /// </summary>
        private int hardwareMaxVoices = 10;

        public void AttachTo(SharedObjects sharedObj)
        {
            shared = sharedObj;
        }

        // Each Terminal should hold one instance of me.
        void Awake()
        {
            kspDirectory = KSPUtil.ApplicationRootPath.Replace("\\", "/");

            // To get AudioMufflerRedux to stop quashing kOS sound effects, we had
            // to put the AudioSource components into a GameObject that was NOT part of the
            // kOS PartModule.  As long as it was in the kOS PartModule, AudioMufflerRedux would
            // mistake the sounds for 3D sound effects coming from the part that need silencing
            // in a vacuum.  (It seems to ignore AudioSource.spatialBlend being set to zero, which
            // is supposed to tell Unity3d the sound is explicitly NOT coming from a position in 3D
            // space.  AudioMufflerRedux muffles it anyway.)  The only fix that seems to work
            // is to remove the sound from the PartModule's GameObject and make a global GameObject
            // to hold it instead:
            soundObject = new GameObject();
            soundObject.name = "kOS.SoundMaker sound library";

            sounds = new Dictionary<string, AudioSource>();
            waveGenerators = new Dictionary<string, ProceduralSoundWave>();

            // Sound samples coming from sound files:
            LoadFileSound("beep", "file://"+ kspDirectory + "GameData/kOS/GFX/terminal-beep.wav");
            LoadFileSound("click", "file://"+ kspDirectory + "GameData/kOS/GFX/terminal-click.wav");
            LoadFileSound("error", "file://"+ kspDirectory + "GameData/kOS/GFX/error.wav");

            // This could be expanded later if we felt like it, to include
            // any sort of "instrument" wave pattern we feel like as a "hardware sound".
            // Making a different wave pattern is just a matter of defining a new mathematical
            // function for that sound wave's graph over time.
            // Implementing sounds that are similar to actual instruments usually means having
            // to encode a more complex pattern into that function.
            LoadProceduralSound("noise", new NoiseSoundWave());
            LoadProceduralSound("square", new SquareSoundWave());
            LoadProceduralSound("sine", new SineSoundWave());
            LoadProceduralSound("triangle", new TriangleSoundWave());
            LoadProceduralSound("sawtooth", new SawtoothSoundWave());

            AddGenericVoices(hardwareMaxVoices);
        }

        private void Destroy()
        {
            if (shared != null)
            {
                StopAllVoices();
                shared.AllVoiceValues.Clear();
                shared = null;
            }
            soundObject.DestroyGameObject();
        }

        /// <summary>
        /// Load a fixed sound effect from a WAV file (file must be WAV format).
        /// </summary>
        /// <param name="name"></param>
        /// <param name="url"></param>
        public void LoadFileSound(string name, string url)
        {
            // Deliberately not fixing the following deprecation warning for using WWW, because I want this
            // codebase to be back-portable to older KSP versions for RO/RP-1 without too much hassle.  Eventually
            // it might not work and we may be forced to change this, but the KSP1 lifecycle may be done
            // by then, so I don't want to make the effort prematurely.  Fixing this requires a very ugly
            // coroutine mess to load URLs the new way Unity wants you to do it.
#pragma warning disable CS0618 // ^^^ see above comment about why this is disabled.
            WWW fileGetter = new WWW(url);
#pragma warning restore CS0618
            AudioClip clip = fileGetter.GetAudioClip();
            AudioSource source = soundObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.spatialBlend = 0; // Makes it ignore spatial position for calculating sound.
            source.bypassListenerEffects = true; // Makes other mods like Rocket Sound Enchancement not affect it.
            sounds[name] = source;
        }

        public IVoice GetVoice(int num)
        {
            return voices[num];
        }

        public void StopAllVoices()
        {
            if (shared != null)
            {
                foreach (var key in shared.AllVoiceValues.Keys)
                {
                    VoiceValue voiceVal = shared.AllVoiceValues[key];
                    if (voiceVal != null)
                    {
                        // VoiceValue.Stop will also handle stoping the underlying voice for us
                        voiceVal.Stop();
                    }
                }
            }
        }

        public string GetWaveName(int voiceNum)
        {
            foreach (string key in waveGenerators.Keys)
                if (waveGenerators[key] == voices[voiceNum].Waveform)
                    return key;
            return "";
        }

        public bool SetWave(int num, string waveName)
        {
            if (! waveGenerators.ContainsKey(waveName))
                return false;
            if (num < 0 || num >= voices.Length)
                return false;

            voices[num].SetWave(waveGenerators[waveName]);
            return true;
        }

        /// <summary>
        /// Load a sound wave sample that was built procedurally in memory.
        /// These kinds of samples can be stretched later to play at different
        /// notes.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="waveGen"></param>
        public void LoadProceduralSound(string name, ProceduralSoundWave waveGen)
        {
            waveGenerators[name] = waveGen;
        }

        public void AddGenericVoices(int howMany)
        {
            voices = new Voice[howMany];
            for (int i = 0; i < howMany ; ++i)
            {
                Voice voice = soundObject.AddComponent<Voice>();
                voices[i] = voice;
            }
        }

        public bool BeginFileSound(string name, float volume = 1f)
        {
            if (! sounds.ContainsKey(name))
                return false;

            // Not allowed to call this on wave generators:
            if (waveGenerators.ContainsKey(name))
                return false;

            AudioSource source = sounds[name];
            if (source.clip.loadState != AudioDataLoadState.Loaded)
                return false; // the clip is not ready
            if (source.isPlaying)
                source.Stop();
            source.volume = GameSettings.UI_VOLUME * volume;

            // This is nonblocking.  Begins playing sound in background.  Code will not wait for it to finish:
            source.Play();
            return true;
        }
    }
}
