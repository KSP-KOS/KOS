using System;
using System.Collections.Generic;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Serialization;
using kOS.Safe;

namespace kOS.Sound
{
    /// <summary>
    /// Holds the information about a single note to be played, maybe as part of a song, or not.
    /// </summary>
    [kOS.Safe.Utilities.KOSNomenclature("Note")]
    [kOS.Safe.Utilities.KOSNomenclature("SlideNote", CSharpToKOS = false)]
    public class NoteValue : SerializableStructure
    {
        public float Frequency { get; set; }
        public float EndFrequency { get; set; }
        public float Volume { get; set; }
        public float KeyDownLength { get; set; }
        public float Duration { get; set; }

        public NoteValue(float freq, float vol, float keyDownLength, float duration)
        {
            this.Frequency = freq;
            this.EndFrequency = freq;
            this.Volume = vol;
            this.KeyDownLength = keyDownLength;
            this.Duration = duration;

            InitializeSuffixes();
        }

        public NoteValue(float freq, float endFreq, float vol, float keyDownLength, float duration) :
            this(freq, vol, keyDownLength, duration)
        {
            this.EndFrequency = endFreq;
        }

        public NoteValue(string letterNote, float vol, float keyDownLength, float duration) : 
            this( LetterToHertz(letterNote), vol, keyDownLength, duration)
        {
        }

        public NoteValue(string letterNote, string endLetterNote, float vol, float keyDownLength, float duration) : 
            this( LetterToHertz(letterNote), LetterToHertz(endLetterNote), vol, keyDownLength, duration)
        {
        }

        // Only used by CreateFromDump()- don't make it public because it leaves fields
        // unpopulated if not immediately followed up by LoadDump():
        private NoteValue()
        {
        }

        // Required for all IDumpers for them to work, but can't enforced by the interface because it's static:
        public static NoteValue CreateFromDump(SafeSharedObjects shared, Dump d)
        {
            var newObj = new NoteValue();
            newObj.LoadDump(d);
            return newObj;
        }

        private void InitializeSuffixes()
        {
            AddSuffix("FREQUENCY", new Suffix<ScalarDoubleValue>(() => Frequency));
            AddSuffix("ENDFREQUENCY", new Suffix<ScalarDoubleValue>(() => EndFrequency));
            AddSuffix("VOLUME", new Suffix<ScalarDoubleValue>(() => Volume));
            AddSuffix("KEYDOWNLENGTH", new Suffix<ScalarDoubleValue>(() => KeyDownLength));
            AddSuffix("DURATION", new Suffix<ScalarDoubleValue>(() => Duration));
        }

        public override string ToString()
        {
            if (Frequency == EndFrequency)
                return String.Format("Note({0},{1},{2},{3})", Frequency, KeyDownLength, Duration, Volume);
            else
                return String.Format("SlideNote({0},{1},{2},{3},{4})", Frequency, EndFrequency, KeyDownLength, Duration, Volume);
        }

        public override Dump Dump()
        {
            DumpWithHeader result = new DumpWithHeader();

            result.Header = "NOTE";

            result.Add("freq", Frequency);
            result.Add("endfreq", EndFrequency);
            result.Add("vol", Volume);
            result.Add("keydown", KeyDownLength);
            result.Add("duration", Duration);

            return result;
        }

        public override void LoadDump(Dump dump)
        {
            Frequency = Convert.ToSingle(dump["freq"]);
            EndFrequency = Convert.ToSingle(dump["endfreq"]);
            Volume = Convert.ToSingle(dump["vol"]);
            KeyDownLength = Convert.ToSingle(dump["keydown"]);
            Duration = Convert.ToSingle(dump["duration"]);
        }

        /// <summary>
        /// A utility function to return the frequency in Hertz given a note expressed as
        /// in "letter" form as a string.
        /// </summary>
        /// <param name="letterString">The format of the string must be as follows:<br/>
        /// Starts with a Letter from A to G for the note.<br/>
        /// Next, optionaly a "#" or "b" for sharp or flat.<br/>
        /// Next, mandatorily, a digit of 0-8 for the octave.<br/>
        /// Examples: "C4" = middle C.  "C#4" = middle c sharp.  "Cb4" = middle c flat.  C5 = high C.  C3 = low C.<br/>
        /// octaves follow the weird musical convention of starting at C instead of at A.  (i.e. C4 is one higher than B3, not B4)<br/></param>
        /// <returns>Hertz</returns>
        static public float LetterToHertz(string letterString)
        {
            int len = letterString.Length;
            if (len < 2 || len > 3)
                return 0f;

            int octave = (int)(letterString[len - 1] - '0');
            string octaveLessNote = letterString.Substring(0,len-1).ToLower();
            double referenceHz;
            if (octave4Lookup.TryGetValue(octaveLessNote, out referenceHz))
            {
                int octaveDiff = octave - 4;
                return (float)(referenceHz * Math.Pow(2.0, (double)octaveDiff));
            }
            return 0f; // bogus when input was garbage
        }

        /// <summary>
        /// Lookup table to find the Frequency (Hertz) for a note in the reference octave (octave 4).
        /// Be sure to lowercase the string before passing it in to this lookup table.
        /// </summary>
        private static Dictionary<string, double> octave4Lookup = new Dictionary<string, double>()
        {
            {"r", 0.0}, // a rest - no frequency.
            {"c", 261.626},
            {"c#", 277.183}, {"db", 277.183},
            {"d", 293.665},
            {"d#", 311.127}, {"eb", 311.127},
            {"e", 329.628},
            {"f", 349.228},
            {"f#", 369.994}, {"fb", 369.994},
            {"g", 391.995},
            {"g#", 415.305}, {"ab", 415.305},
            {"a", 440},
            {"a#", 466.164}, {"bb", 466.164},
            {"b", 493.883}
        };
    }
}
