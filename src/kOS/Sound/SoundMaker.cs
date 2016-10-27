using System;
using System.Collections.Generic;
using UnityEngine;
using kOS.Safe.Sound;

namespace kOS.Sound
{
    /// <summary>
    /// SoundMaker is the "unsafe" implementation of ISoundMaker, that has calls into the Unity API.
    /// </summary>
    public class SoundMaker : MonoBehaviour, ISoundMaker
    {        
        private string kspDirectory = KSPUtil.ApplicationRootPath.Replace("\\", "/");
        private Dictionary<string, AudioSource> sounds;
        private Voice[] voices;
        private Dictionary<string, ProceduralSoundWave> waveGenerators;
        
        // Each Terminal should hold one instance of me.
        void Awake()
        {
            sounds = new Dictionary<string, AudioSource>();
            waveGenerators = new Dictionary<string, ProceduralSoundWave>();
            DontDestroyOnLoad(gameObject);            
            
            // Sound samples coming from sound files:
            LoadFileSound("beep", "file://"+ kspDirectory + "GameData/kOS/GFX/terminal-beep.wav");
            LoadFileSound("click", "file://"+ kspDirectory + "GameData/kOS/GFX/terminal-click.wav");
            LoadFileSound("error", "file://"+ kspDirectory + "GameData/kOS/GFX/error.wav");

            LoadProceduralSound("noise", new NoiseSoundWave());
            LoadProceduralSound("square", new SquareSoundWave());
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

        public IVoice GetVoice(int num)
        {
            return voices[num];
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
            if (num < 0 || num > waveGenerators.Count)
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
                Voice voice = gameObject.AddComponent<Voice>();
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
        /// <param name="name">a procedural sound wave name that was loaded and prepped ahead of time</param>
        /// <param name="frequency">the note, expressed in Hertz (not musical scales)</param>
        /// <param name="duration">the note's duration, in seconds.</param>
        /// <param name="volume">the note's volume, from 0.0 up to 1.0</param>
        /// <returns>false if the sound name given doesn't seem to be found or it is but it was 
        /// a sound file not a procedural sound.</returns>
        public bool BeginProceduralSound(int voiceNum, string name, float frequency, float duration, float volume = 1f)
        {
            if (! waveGenerators.ContainsKey(name))
                return false;

            voices[voiceNum].BeginProceduralSound(waveGenerators[name],frequency,duration,volume);
            return true;
        }
    }
}
