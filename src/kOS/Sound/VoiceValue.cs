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
        private NoteValue curNote;
        private float noteEndTimeStamp;
        private float noteStartTimeStamp;
        /// <summary>Records the total delta frequency the note will experience across its duration
        /// if it's a slide note (pre-cached for faster calculation during updates)</summary>
        private float noteFreqTotalChange;
        private float tempo = 1f;
        private bool isPlaying;
        public bool IsPlaying
        {
            get { return isPlaying; }
            set { isPlaying = value; if (!isPlaying) voice.Stop(); }
        }
        
        /// <summary>
        /// If we notice the game is paused, record the timestamp where the pausing began.
        /// Also used as a flag - set it to negative to indicate we're not currently paused.
        /// </summary>
        private float freezeBeganTimestamp = -1f; 

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
            AddSuffix("STOP", new NoArgsVoidSuffix(Stop));
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

        public void Stop()
        {
            IsPlaying = false;
            // if we only set IsPlaying to false, the note that is currently playing on the underlying voice would
            // be allowed to finish.  In the case of a song where notes are usually pretty short this would be OK,
            // but if the user has set a note to play for 60s the effects of calling Stop may not be seen immediately.
            // So we stop the underlying voice too.
            voice.Stop();
        }

        public void KOSUpdate(double deltaTime)
        {
            if (! IsPlaying)
                return;

            // Be sure we use the same game clock here as in Voice.cs's Update():  (i.e. unscaledTime vs Time vs fixedTime):
            float now = Time.unscaledTime;
            
            if (Time.timeScale == 0f) // game is frozen (i.e. the Escape Menu is up.)
            {
                if (freezeBeganTimestamp < 0f) // And we weren't frozen before so it's the start of a new freeze instance.
                    freezeBeganTimestamp = now;
                return; // do none of the rest of this work until the pause is over.
            }
            else // game is not frozen.
            {
                if (freezeBeganTimestamp >= 0f) // And we were frozen before so we just became unfrozen now
                {
                    // Push the timestamp ahead by the duration of the pause so it will continue what's left of the note
                    // instead of truncating it early:
                    float freezeDuration = now - freezeBeganTimestamp;
                    noteStartTimeStamp += freezeDuration;
                    noteEndTimeStamp += freezeDuration;
                    freezeBeganTimestamp = -1f;
                }
            }

            // If still playing prev note, do nothing except maybe change
            // its frequency if it's a slidenote:
            if (now < noteEndTimeStamp)
            {
                NoteValue note = song[noteNum] as NoteValue;
                if (noteFreqTotalChange != 0.0)
                {
                    float durationPortion = (now - noteStartTimeStamp) / (noteEndTimeStamp - noteStartTimeStamp);
                    float newFreq = note.Frequency + durationPortion*noteFreqTotalChange;
                    voice.ChangeFrequency(newFreq);
                }
                return;
            }

            // Increment to next note and start playing it:
            ++noteNum;
            if (noteNum >= song.Count())
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
                curNote = song[noteNum] as NoteValue;
                if (curNote != null)
                {
                    noteStartTimeStamp = now;
                    noteEndTimeStamp = now + tempo*curNote.Duration;
                    noteFreqTotalChange = curNote.EndFrequency - curNote.Frequency;
                    voice.BeginProceduralSound(curNote.Frequency, tempo*curNote.KeyDownLength, curNote.Volume);
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
