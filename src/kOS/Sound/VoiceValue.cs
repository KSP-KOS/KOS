using System;
using System.Collections.Generic;
using UnityEngine;
using kOS.Safe.Sound;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe;

namespace kOS.Sound
{
    /// <summary>
    /// The kOS wrapper around the control of a single kOS.Sound.Voice object.
    /// </summary>
    [kOS.Safe.Utilities.KOSNomenclature("Voice")]
    public class VoiceValue : Structure, IUpdateObserver
    {
        private ISoundMaker maker;
        private IVoice voice;
        private UpdateHandler updateHandler;
        private int voiceNum;
        private bool loop;  // true if looping the note or song:

        // If it's playing a song (note list) right now, these private
        // fields track where it is within the song at the moment:
        private int noteNum;
        private ListValue song;
        private float noteEndTimeStamp;
        private float tempo = 1f;
        private bool isPlaying;
        public bool IsPlaying
        {
            get { return isPlaying; }
            set { isPlaying = value; if (!isPlaying) voice.Stop(); }
        }

        public VoiceValue(UpdateHandler updateHandler, int voiceNum, ISoundMaker maker)
        {
            this.voiceNum = voiceNum;
            this.maker = maker;
            this.updateHandler = updateHandler;
            voice = maker.GetVoice(voiceNum);
            maker.SetWave(voiceNum, "square");
            this.updateHandler.AddObserver(this);

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
            AddSuffix("PLAY", new OneArgsSuffix<Structure>(Play));
            AddSuffix("LOOP", new SetSuffix<BooleanValue>(() => loop, value => loop = value));
            AddSuffix("ISPLAYING", new SetSuffix<BooleanValue>(() => IsPlaying, value => IsPlaying = value));
            AddSuffix("TEMPO", new SetSuffix<ScalarValue>(() => tempo, value => tempo = (float)value.GetDoubleValue()));
        }

        public void Play(Structure notes)
        {
            if (notes is NoteValue)
            {
                song = new ListValue();
                song.Add(notes);
            }
            else if (notes is ListValue)
            {
                song = notes as ListValue;
            }
            else
                throw new KOSInvalidArgumentException("Play", "note", "Requires either a NOTE() or a LIST() of NOTE()'s as its parameter.");

            noteNum = -1;
            noteEndTimeStamp = -1f;
            IsPlaying = true;

        }
        
        public void KOSUpdate(double deltaTime)
        {
            if (! IsPlaying)
                return;

            float now = Time.fixedTime;
            
            // If still playing prev note, do nothing:
            if (now < noteEndTimeStamp)
                return;
            
            // Increment to next note and start playing it:
            ++noteNum;
            if (noteNum > song.Count())
            {
                if (loop)
                {
                    noteNum = -1;
                    noteEndTimeStamp = -1f;
                }
                else
                    IsPlaying = false;
            }
            else
            {
                NoteValue nextNote = song[noteNum] as NoteValue;
                if (nextNote != null)
                {
                    noteEndTimeStamp = now + tempo*nextNote.Duration;
                    voice.BeginProceduralSound(nextNote.Frequency, tempo*nextNote.KeyDownLength, nextNote.Volume);
                }
            }
        }
        
        public void Dispose()
        {
            updateHandler.RemoveObserver(this);            
            voice.Frequency = 0f;
            song = null;
            voice = null;
            maker = null;
        }
        
        public override string ToString()
        {
            return String.Format("Voice({0})", voiceNum);
        }

    }
}
