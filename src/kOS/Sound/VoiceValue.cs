using System;
using System.Collections.Generic;
using UnityEngine;
using kOS.Safe.Sound;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Sound
{
    /// <summary>
    /// The kOS wrapper around the control of a single kOS.Sound.Voice object.
    /// </summary>
    [kOS.Safe.Utilities.KOSNomenclature("Voice")]
    public class VoiceValue : Structure
    {
        private ISoundMaker maker;
        private IVoice voice;
        private int voiceNum;
        
        public VoiceValue(int voiceNum, ISoundMaker maker)
        {
            this.voiceNum = voiceNum;
            this.maker = maker;
            voice = maker.GetVoice(voiceNum);
            InitalizeSuffixes();
        }
        
        public void InitalizeSuffixes()
        {
            AddSuffix("ATTACK", new SetSuffix<ScalarValue>(() => voice.Attack, value => voice.Attack = value));
            AddSuffix("DECAY", new SetSuffix<ScalarValue>(() => voice.Decay, value => voice.Decay = value));
            AddSuffix("SUSTAIN", new SetSuffix<ScalarValue>(() => voice.Sustain, value => voice.Sustain = value));
            AddSuffix("RELEASE", new SetSuffix<ScalarValue>(() => voice.Release, value => voice.Release = value));
            AddSuffix("VOLUME", new SetSuffix<ScalarValue>(() => voice.Volume, value => voice.Volume = value));
            AddSuffix("WAVE", new SetSuffix<StringValue>(() => maker.GetWaveName(voiceNum), value => maker.SetWave(voiceNum, value.ToString())));
            AddSuffix("PLAY", new OneArgsSuffix<NoteValue>(Play));
        }
        
        public void Play(NoteValue note)
        {
            voice.BeginProceduralSound(note.Frequency, note.KeyDownLength, note.Volume);
        }
        
        public override string ToString()
        {
            return String.Format("Voice({0})", voiceNum);
        }

    }
}
