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
            AddSuffix("SUSTAIN", new ClampSetSuffix<ScalarValue>(() => voice.Sustain, value => voice.Sustain = value, 0, 1));
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
            curNote = null;
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

            // If still playing prev note, return, doing nothing except maybe changing
            // the current note's frequency if it's a slidenote:
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

            AdvanceNote(now);
        }
        
        /// <summary>
        /// Advance until finding a note that is within the current timestamp,
        /// and start playing it.
        /// <br/><br/>
        /// This takes into account that under laggy conditions
        /// our physics updates might be coming too infrequently to hit every note on time.
        /// This will advance to where we *should have been* in the song list by now,
        /// by calculating based on how long the notes were *supppsed* to have
        /// lasted and when they were *suppposed* to have ended.
        /// <br/><br/>
        /// This might mean that a note gets its duration shorted to "catch up", or even
        /// that a note gets skipped entirely in order to "catch up".
        /// This is necessary to keep the voices of multi-voice songs synced up with each other.
        /// <br/><br/>
        /// If there is no next note and the voice isn't in looping mode,
        /// it will set isPlaying to false.
        /// <br/>
        /// </summary>
        /// <param name="now">timestamp for current time right now</param>
        private void AdvanceNote(float now)
        {
            // Keep advancing through the notes list until we get to a note
            // that should have been executing at the current time:
            while (now > noteEndTimeStamp)
            {
                NoteValue prevNote = curNote;
                ++noteNum;
                if (loop)
                    noteNum = noteNum % song.Count(); // wraparound to zero if looping and past end.
                if (noteNum >= song.Count())
                {
                    isPlaying = false; // stop if past end.
                    curNote = null;
                    break;
                }

                // Advancing the note:
                // -------------------
                curNote = song[noteNum] as NoteValue;
                if (curNote == null)
                    return;
                if (prevNote == null)
                {
                    // No prev note, so start first note at right now:
                    noteStartTimeStamp = now;
                }
                else
                {
                    // Don't set start time to now, but rather set it to when this note
                    // *should* have started if the physics update had hit at the right time:
                    noteStartTimeStamp = noteStartTimeStamp + tempo*prevNote.Duration;
                }
                
                noteEndTimeStamp = noteStartTimeStamp + tempo*curNote.Duration;
                noteFreqTotalChange = curNote.EndFrequency - curNote.Frequency;
            }
            
            // Now play the note we had advanced to:
            if (isPlaying)
                voice.BeginProceduralSound(curNote.Frequency, tempo*curNote.KeyDownLength, curNote.Volume);
            
            // Be aware that because we told the low level sound chip to start this note *now*, but we
            // tracked our own start time (noteStartTimeStamp) as when the note *should* have started,
            // that the low level sound chip will start the ADSR envelope now, rather than partway through
            // the middle of the envelope.  This means that if a note has to get "shorted" to catch up,
            // then the "shorted" part of the note that gets cut off will be the *end* of that note,
            // not the *start* of it.  Thus if the ADSR envelope makes short staccato notes with fast
            // attack and decay with no sustain, we won't end up selencing the note entirely when it's
            // shorted.  (We would if we had cut off the start of the note and kept the end of it that
            // occurs after the attack and the decay are over.).
            // TL;DR : If we have to play a short duration version of the note, we'd rather snip ff the
            // release part at the end then snip off the attack/decay part at the start.
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
