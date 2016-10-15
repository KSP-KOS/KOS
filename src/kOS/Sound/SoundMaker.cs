using System;
using System.Collections.Generic;
using UnityEngine;
using kOS.Safe.Sound;

namespace kOS.Sound
{
    /// <summary>
    /// SoundMaker is the "unsafe" implementation of ISoundMaker, that has calls into the Unity API.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class SoundMaker : MonoBehaviour, ISoundMaker
    {
        private static SoundMaker myself;
        public static ISoundMaker Instance {get{ return myself;}}
        
        private string kspDirectory = KSPUtil.ApplicationRootPath.Replace("\\", "/");
        private Dictionary<string, AudioSource> sounds;
        private AudioSource[] voices;        
        private Dictionary<string, ProceduralSoundWave> waveGenerators;
        
        // All kOS PartModules should actually share the single same instance of me:
        void Awake()
        {
            myself = this;
            sounds = new Dictionary<string, AudioSource>();
            waveGenerators = new Dictionary<string, ProceduralSoundWave>();
            DontDestroyOnLoad(gameObject);            
            
            // Sound samples coming from sound files:
            LoadFileSound("beep", "file://"+ kspDirectory + "GameData/kOS/GFX/terminal-beep.wav");
            LoadFileSound("click", "file://"+ kspDirectory + "GameData/kOS/GFX/terminal-click.wav");
            LoadFileSound("error", "file://"+ kspDirectory + "GameData/kOS/GFX/error.wav");

            LoadProceduralSound("noise", new NoiseSoundWave());
            LoadProceduralSound("pulse", new PulseSoundWave());
            LoadProceduralSound("sine", new SineSoundWave());
            LoadProceduralSound("triangle", new TriangleSoundWave());
            LoadProceduralSound("sawtooth", new SawtoothSoundWave());
            
            AddGenericVoices(4);
        }
        
        /// <summary>
        /// Load a fixed sound effect from a file.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="url"></param>
        public void LoadFileSound(string name, string url)
        {
            WWW fileGetter = new WWW(url);
            AudioClip clip = fileGetter.audioClip;            
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            
            sounds[name] = source;
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
            voices = new AudioSource[howMany];
            for (int i = 0; i < howMany ; ++i)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                voices[i] = source;
                source.loop = true;                
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
            if (source.isPlaying)
                source.Stop();
            source.volume = GameSettings.UI_VOLUME * volume;
            
            // This is nonblocking.  Begins playing sound in background.  Code will not wait for it to finish:
            source.Play();
            return true;
        }
        
        /// <summary>
        /// Begin a single note from a ProceduralSoundWave reference sample.
        /// You can pass in a frequency and it will "stretch" the reference sample
        /// to make it fit the given frequency.
        /// </summary>
        /// <param name="voiceNum">a number corresponding to one of the "voices" on the audio "chip".</param>
        /// <param name="name">a sound name that corresponds to a previous call to LoadProceduraulSound</param>
        /// <param name="frequency">the note, expressed in Hertz (not musical scales)</param>
        /// <param name="duration">the note's duration, in seconds.</param>
        /// <param name="volume">the note's volume, from 0.0 up to 1.0</param>
        /// <returns>false if the sound name given doesn't seem to be found or it is but it was 
        /// a sound file not a procedural sound.</returns>
        public bool BeginProceduralSound(int voiceNum, string name, float frequency, float duration, float volume = 1f)
        {
            if (! waveGenerators.ContainsKey(name))
                return false;

            AudioSource source = voices[voiceNum];

            // Attempted to play the same sound while it's already playing,
            // so stop the previous sound:
            if (source.isPlaying)
               source.Stop();

            ProceduralSoundWave waveGen = waveGenerators[name];
            source.clip = waveGen.Clip;            
            source.volume = GameSettings.UI_VOLUME * volume;            
            // Speed up or slow down the playback rate to give the desired frequency:
            // (AudioSource.pitch is actually just a speed multiplier, not the raw pitch.
            // For example, AudioSource.pitch of 2.0 means "play the sound twice as
            // fast by skipping every other sample point.":
            source.pitch = frequency / waveGen.Periods;

            source.Play();
            source.SetScheduledEndTime(AudioSettings.dspTime + duration);
            return true;
        }
    }
}
