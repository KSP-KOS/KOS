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

        // All kOS PartModules should actually share the single same instance of me:
        void Awake()
        {
            myself = this;
            sounds = new Dictionary<string, AudioSource>();
            DontDestroyOnLoad(gameObject);            
            
            LoadSound("beep", "file://"+ kspDirectory + "GameData/kOS/GFX/terminal-beep.wav");
            LoadSound("click", "file://"+ kspDirectory + "GameData/kOS/GFX/terminal-click.wav");
            LoadSound("error", "file://"+ kspDirectory + "GameData/kOS/GFX/error.wav");
        }
        
        
        public void LoadSound(string name, string url)
        {
            WWW fileGetter = new WWW(url);
            AudioClip clip = fileGetter.audioClip;            
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            
            sounds[name] = source;
        }
        
        public bool BeginSound(string name)
        {
            if (! sounds.ContainsKey(name))
                return false;
            AudioSource source = sounds[name];
            source.volume = GameSettings.UI_VOLUME;
            if (!source.clip.isReadyToPlay || source.isPlaying)
                return false; // prev beep sound still is happening.
            
            // This is nonblocking.  Begins playing sound in background.  Code will not wait for it to finish:
            source.Play();
            return true;
        }
    }
}
