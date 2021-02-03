using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Serialization;
using System.Collections.Generic;
using kOS.Safe;

namespace kOS.Suffixed
{
    /// <summary>
    /// Both TimeStamp and TimeSpan will be derived from this common base class,
    /// to avoid duplicate code between them.  This is because really the only
    /// difference between TimeStamp and TimeSpan is whether the nomenlcature
    /// counts starting at 1 or starting at 0.  The Kerbal calendar doesn't have
    /// all the same ugly problems as the real world calender that necessitate a
    /// separate timestamp type (a leap day in the middle of the year instead of
    /// at the end offsetting all the dates that come after Feb 29th, for example,
    /// or having this mythical concept of 'a month' which isn't even the same
    /// number of days for all months, etc.  The Kerbals never bothered with months
    /// and instead just count days into the year, and don't put in leap days, instead
    /// just having the same leftover fractional day at the end of every year.  These
    /// things make a seprate timespan and timestamp type a bit less necessary in the
    /// Kerbal world, but one the remaining difference is whether you count starting at 1
    /// or starting at 0.
    /// </summary>
    [kOS.Safe.Utilities.KOSNomenclature("TimeBase")]
    public abstract class TimeBase : SerializableStructure
    {
        /// <summary>
        /// For serializaation, how will it be named in the JSON output.
        /// </summary>
        public abstract string DumpName { get; }
        
        /// <summary>
        /// Override with either 0 or 1 for whether counting years and days starts counting at 0 or at 1.
        /// </summary>
        protected abstract double CountOffset { get; }

        protected double seconds;

        protected int SecondsPerDay { get { return KSPUtil.dateTimeFormatter.Day; } }
        protected int SecondsPerHour { get { return KSPUtil.dateTimeFormatter.Hour; } }
        protected int SecondsPerYear { get { return KSPUtil.dateTimeFormatter.Year; } }
        protected int SecondsPerMinute { get { return KSPUtil.dateTimeFormatter.Minute; } }

        // Only used by CreateFromDump() and the other constructors.
        // Don't make it public because it leaves fields
        // unpopulated:
        protected TimeBase()
        {
            InitializeSuffixes();
        }

        public TimeBase(double unixStyleTime) : this()
        {
            seconds = unixStyleTime;
        }

        // Required for all IDumpers for them to work, but can't enforced by the interface because it's static:
        /* Derivative classes of TimeBase need to make something like this, replacing "TimeBase" with their own
         * class name:
         * 
        public static TimeBase CreateFromDump(SafeSharedObjects shared, Dump d)
        {
            var newObj = new TimeBase();
            newObj.LoadDump(d);
            return newObj;
        }
        *
        */

        protected abstract void InitializeSuffixes();

        /* need to give the two types different suffixes
         * 
        private void InitializeSuffixes()
        {
            AddSuffix("YEAR", new Suffix<ScalarValue>(CalculateYear));
            AddSuffix("DAY", new Suffix<ScalarValue>(CalculateDay));
            AddSuffix("HOUR", new Suffix<ScalarValue>(CalculateHour));
            AddSuffix("MINUTE", new Suffix<ScalarValue>(CalculateMinute));
            AddSuffix("SECOND", new Suffix<ScalarValue>(CalculateSecond));
            AddSuffix("SECONDS", new Suffix<ScalarValue>(() => seconds));
            AddSuffix("CLOCK", new Suffix<StringValue>(() => string.Format("{0:00}:{1:00}:{2:00}", (int)CalculateHour(), (int)CalculateMinute(), (int)CalculateSecond())));
            AddSuffix("CALENDAR", new Suffix<StringValue>(() => "Year " + CalculateYear() + ", day " + CalculateDay()));
        }
        */

        protected ScalarValue CalculateYear()
        {
            return (int)Math.Floor(seconds / SecondsPerYear) + CountOffset;
        }

        /// <summary>
        /// Change JUST the year part of the time, leaving the remainder (days, hours, etc) still intact.
        /// </summary>
        /// <param name="newValue"></param>
        protected void ChangeYear(ScalarValue newValue)
        {
            int zeroBasedNewValue = newValue - CountOffset;
            seconds = (seconds % SecondsPerYear) + (zeroBasedNewValue * SecondsPerYear);
        }

        protected ScalarValue CalculateDay()
        {
            return (int)Math.Floor(seconds % SecondsPerYear / SecondsPerDay) + CountOffset;
        }

        /// <summary>
        /// Change JUST the day part of the time, leaving the year, hour, minute, and second intact:
        /// </summary>
        /// <param name="newValue"></param>
        protected void ChangeDay(ScalarValue newValue)
        {
            int zeroBasedNewValue = newValue - CountOffset;
            // Subtract old day value:
            seconds -= Math.Floor(seconds % SecondsPerYear / SecondsPerDay);
            // Add new day value:
            seconds += zeroBasedNewValue * SecondsPerDay;
        }

        protected ScalarValue CalculateHour()
        {
            return (int)Math.Floor(seconds % SecondsPerDay / SecondsPerHour);
        }

        /// <summary>
        /// Change JUST the Hour part of the time, leaving the year, day, minute, and second intact:
        /// </summary>
        /// <param name="newValue"></param>
        protected void ChangeHour(ScalarValue newValue)
        {
            int zeroBasedNewValue = newValue - CountOffset;
            // Subtract old hour value:
            seconds -= Math.Floor(seconds % SecondsPerDay / SecondsPerHour);
            // Add new hour value:
            seconds += zeroBasedNewValue * SecondsPerHour;
        }

        protected ScalarValue CalculateMinute()
        {
            return (int)Math.Floor(seconds % SecondsPerHour / SecondsPerMinute);
        }

        /// <summary>
        /// Change JUST the Minute part of the time, leaving the year, day, hour, and second intact:
        /// </summary>
        /// <param name="newValue"></param>
        protected void ChangeMinute(ScalarValue newValue)
        {
            int zeroBasedNewValue = newValue - CountOffset;
            // Subtract old minute value:
            seconds -= Math.Floor(seconds % SecondsPerHour / SecondsPerMinute);
            // Add new minute value:
            seconds += zeroBasedNewValue * SecondsPerMinute;
        }

        protected ScalarValue CalculateSecond()
        {
            return (int)Math.Floor(seconds % SecondsPerMinute);
        }

        /// <summary>
        /// Change JUST the Second part of the time, leaving the year, day, hour, and minute intact:
        /// </summary>
        /// <param name="newValue"></param>
        protected void ChangeSecond(ScalarValue newValue)
        {
            int zeroBasedNewValue = newValue - CountOffset;
            // Subtract old minute value:
            seconds -= Math.Floor(seconds % SecondsPerMinute);
            // Add new minute value:
            seconds += zeroBasedNewValue;
        }

        public double ToUnixStyleTime()
        {
            return seconds;
        }

        /* These conversions will have to be in the overriding classes:
         * 
         * 
        public static TimeBase operator +(TimeBase a, TimeBase b) { return new TimeBase(a.ToUnixStyleTime() + b.ToUnixStyleTime()); }
        public static TimeBase operator -(TimeBase a, TimeBase b) { return new TimeBase(a.ToUnixStyleTime() - b.ToUnixStyleTime()); }
        public static TimeBase operator +(TimeBase a, double b) { return new TimeBase(a.ToUnixStyleTime() + b); }
        public static TimeBase operator -(TimeBase a, double b) { return new TimeBase(a.ToUnixStyleTime() - b); }
        public static TimeBase operator *(TimeBase a, double b) { return new TimeBase(a.ToUnixStyleTime() * b); }
        public static TimeBase operator /(TimeBase a, double b) { return new TimeBase(a.ToUnixStyleTime() / b); }
        public static TimeBase operator +(double b, TimeBase a) { return new TimeBase(b + a.ToUnixStyleTime()); }
        public static TimeBase operator -(double b, TimeBase a) { return new TimeBase(b - a.ToUnixStyleTime()); }
        public static TimeBase operator *(double b, TimeBase a) { return new TimeBase(b * a.ToUnixStyleTime()); }
        public static TimeBase operator /(double b, TimeBase a) { return new TimeBase(b / a.ToUnixStyleTime()); }
        public static TimeBase operator /(TimeBase b, TimeBase a) { return new TimeBase(b.ToUnixStyleTime() / a.ToUnixStyleTime()); }
        public static bool operator >(TimeBase a, TimeBase b) { return a.ToUnixStyleTime() > b.ToUnixStyleTime(); }
        public static bool operator <(TimeBase a, TimeBase b) { return a.ToUnixStyleTime() < b.ToUnixStyleTime(); }
        public static bool operator >=(TimeBase a, TimeBase b) { return a.ToUnixStyleTime() >= b.ToUnixStyleTime(); }
        public static bool operator <=(TimeBase a, TimeBase b) { return a.ToUnixStyleTime() <= b.ToUnixStyleTime(); }
        public static bool operator >(TimeBase a, double b) { return a.ToUnixStyleTime() > b; }
        public static bool operator <(TimeBase a, double b) { return a.ToUnixStyleTime() < b; }
        public static bool operator >=(TimeBase a, double b) { return a.ToUnixStyleTime() >= b; }
        public static bool operator <=(TimeBase a, double b) { return a.ToUnixStyleTime() <= b; }
        public static bool operator >(double a, TimeBase b) { return a > b.ToUnixStyleTime(); }
        public static bool operator <(double a, TimeBase b) { return a < b.ToUnixStyleTime(); }
        public static bool operator >=(double a, TimeBase b) { return a >= b.ToUnixStyleTime(); }
        public static bool operator <=(double a, TimeBase b) { return a <= b.ToUnixStyleTime(); }

        public static TimeBase operator +(TimeBase a, ScalarValue b) { return new TimeBase(a.ToUnixStyleTime() + b); }
        public static TimeBase operator -(TimeBase a, ScalarValue b) { return new TimeBase(a.ToUnixStyleTime() - b); }
        public static TimeBase operator *(TimeBase a, ScalarValue b) { return new TimeBase(a.ToUnixStyleTime() * b); }
        public static TimeBase operator /(TimeBase a, ScalarValue b) { return new TimeBase(a.ToUnixStyleTime() / b); }
        public static TimeBase operator +(ScalarValue b, TimeBase a) { return new TimeBase(b + a.ToUnixStyleTime()); }
        public static TimeBase operator -(ScalarValue b, TimeBase a) { return new TimeBase(b - a.ToUnixStyleTime()); }
        public static TimeBase operator *(ScalarValue b, TimeBase a) { return new TimeBase(b * a.ToUnixStyleTime()); }
        public static TimeBase operator /(ScalarValue b, TimeBase a) { return new TimeBase(b / a.ToUnixStyleTime()); }
        public static bool operator >(TimeBase a, ScalarValue b) { return a.ToUnixStyleTime() > b; }
        public static bool operator <(TimeBase a, ScalarValue b) { return a.ToUnixStyleTime() < b; }
        public static bool operator >=(TimeBase a, ScalarValue b) { return a.ToUnixStyleTime() >= b; }
        public static bool operator <=(TimeBase a, ScalarValue b) { return a.ToUnixStyleTime() <= b; }
        public static bool operator >(ScalarValue a, TimeBase b) { return a > b.ToUnixStyleTime(); }
        public static bool operator <(ScalarValue a, TimeBase b) { return a < b.ToUnixStyleTime(); }
        public static bool operator >=(ScalarValue a, TimeBase b) { return a >= b.ToUnixStyleTime(); }
        public static bool operator <=(ScalarValue a, TimeBase b) { return a <= b.ToUnixStyleTime(); }
        *
        *
        */

        /*
        public override bool Equals(object obj)
        {
            Type compareType = typeof(TimeSpan);
            if (compareType.IsInstanceOfType(obj))
            {
                TimeSpan t = obj as TimeSpan;
                // Check the equality of the span value
                return seconds == t.ToUnixStyleTime();
            }
            return false;
        }
        */

        public override int GetHashCode()
        {
            return seconds.GetHashCode();
        }

        /*
        public static bool operator ==(TimeSpan a, TimeSpan b)
        {
            Type compareType = typeof(TimeSpan);
            if (compareType.IsInstanceOfType(a))
            {
                return a.Equals(b); // a is not null, we can use the built in equals function
            }
            return !compareType.IsInstanceOfType(b); // a is null, return true if b is null and false if not null
        }

        public static bool operator !=(TimeSpan a, TimeSpan b)
        {
            return !(a == b);
        }
        */

        public override string ToString()
        {
            return string.Format("TIME({0:0})", seconds);
        }

        /*
        public override Dump Dump()
        {
            var dump = new Dump
            {
                {DumpSpan, seconds}
            };

            return dump;
        }

        public override void LoadDump(Dump dump)
        {
            seconds = Convert.ToDouble(dump[DumpSpan]);
        }
            
        public int CompareTo(TimeSpan other)
        {
            return seconds.CompareTo(other.seconds);
        }
        */
    }
}
