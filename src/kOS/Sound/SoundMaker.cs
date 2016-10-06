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
        private Dictionary<string, ProceduralSoundWave> waveGenerators;
        
        // All kOS PartModules should actually share the single same instance of me:
        void Awake()
        {
            myself = this;
            sounds = new Dictionary<string, AudioSource>();
            waveGenerators = new Dictionary<string, ProceduralSoundWave>();
            DontDestroyOnLoad(gameObject);            
            
            // Sound samples coming from sound files:
            LoadSound("beep", "file://"+ kspDirectory + "GameData/kOS/GFX/terminal-beep.wav");
            LoadSound("click", "file://"+ kspDirectory + "GameData/kOS/GFX/terminal-click.wav");
            LoadSound("error", "file://"+ kspDirectory + "GameData/kOS/GFX/error.wav");

            LoadProceduralSound("noise", new NoiseSoundWave());
            LoadProceduralSound("pulse", new PulseSoundWave());
            LoadProceduralSound("sin", new SinSoundWave());
        }
        
        public void LoadSound(string name, string url)
        {
            WWW fileGetter = new WWW(url);
            AudioClip clip = fileGetter.audioClip;            
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            
            sounds[name] = source;
        }
        
        public void LoadProceduralSound(string name, ProceduralSoundWave waveGen)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            sounds[name] = source;
            source.clip = waveGen.Clip; 
            source.loop = true;
            waveGenerators[name] = waveGen;

            float[] dataBuffer = new float[ AudioSettings.outputSampleRate ]; // eraseme
            source.clip.GetData(dataBuffer,0); // eraseme
            Console.WriteLine("==== LoadProceduralData, sample dump for {0} =====", name); // eraseme
            for( int i = 0; i < dataBuffer.Length; ++i) //eraseme
            { // eraseme
                Console.WriteLine("[{0:D5}] = {1}", i, dataBuffer[i]); // eraseme
            } // eraseme
        }

        public bool BeginSound(string name, float volume = 1f)
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
        /// Begin a single note from a sound source and specify its parameters.
        /// If using a ProceduralSoundWave sound, it must be played with this variant
        /// of BeginSound (not the simpler variant) because those wave generators
        /// have no specified end time and just emit the wave forever. (Thus the sound
        /// would never stop if you told it to play out the whole "clip".)
        /// </summary>
        /// <param name="name"></param>
        /// <param name="frequency"></param>
        /// <param name="duration"></param>
        /// <param name="volume"></param>
        /// <returns></returns>
        public bool BeginSound(string name, float frequency, float duration, float volume = 1f)
        {
            // TODO - add envelope falloff stuff (attack, sustain, release).
            //        For my first pass at this, it's just one solid volume the whole way through.
            if (! sounds.ContainsKey(name))
                return false;
            AudioSource source = sounds[name];

            // Attempted to play the same sound while it's already playing:
            if (source.isPlaying)
               source.Stop();
            
            source.volume = GameSettings.UI_VOLUME * volume;
            source.pitch = frequency;
            
            if (waveGenerators.ContainsKey(name))
            {
                ProceduralSoundWave waveGen = waveGenerators[name];
                source.pitch /= waveGen.Periods;
            }
            
            source.Play();
            source.SetScheduledEndTime(AudioSettings.dspTime + duration);
            return true;
        }
    }
}
