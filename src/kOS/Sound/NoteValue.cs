using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Sound
{
    /// <summary>
    /// Holds the information about a single note to be played, maybe as part of a song, or not.
    /// </summary>
    [kOS.Safe.Utilities.KOSNomenclature("Note")]
    public class NoteValue : Structure
    {
        public float Frequency { get; set; }
        public float Volume { get; set; }
        public float KeyDownLength { get; set; }
        public float Duration { get; set; }
        
        public NoteValue(float freq, float vol, float keyDownLength, float duration)
        {
            this.Frequency = freq;
            this.Volume = vol;
            this.KeyDownLength = keyDownLength;
            this.Duration = duration;
            
            InitializeSuffixes();
        }
        
        private void InitializeSuffixes()
        {
            AddSuffix("FREQUENCY", new Suffix<ScalarDoubleValue>(() => Frequency));
            AddSuffix("VOLUME", new Suffix<ScalarDoubleValue>(() => Volume));
            AddSuffix("KEYDOWNLENGTH", new Suffix<ScalarDoubleValue>(() => KeyDownLength));
            AddSuffix("DURATION", new Suffix<ScalarDoubleValue>(() => Duration));
        }

        public override string ToString()
        {
            return String.Format("Note({0},{1},{2},{3})", Frequency, KeyDownLength, Duration, Volume);
        }
        
    }
}
