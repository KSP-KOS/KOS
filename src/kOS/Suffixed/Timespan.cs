using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Serialization;
using System.Collections.Generic;
using kOS.Safe;

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("Timespan")]
    public class TimeSpan : SerializableStructure, IComparable<TimeSpan>
    {
        public const string DumpSpan = "span";
        public const string DumpZeroMode = "zeroMode";

        double span;

        private int SecondsPerDay { get { return KSPUtil.dateTimeFormatter.Day; } }
        private int SecondsPerHour { get { return KSPUtil.dateTimeFormatter.Hour; } }
        private int SecondsPerYear { get { return KSPUtil.dateTimeFormatter.Year; } }
        private int SecondsPerMinute { get { return KSPUtil.dateTimeFormatter.Minute; } }
        /// <summary>True if years and days should start counting from zero.
        /// (The default is to call the first year '1" and the first day "1" if this is false.)</summary>
        public bool ZeroMode { get; set; }

        // Only used by CreateFromDump() and the other constructors.
        // Don't make it public because it leaves fields
        // unpopulated:
        private TimeSpan()
        {
            InitializeSuffixes();
        }

        public TimeSpan(double unixStyleTime) : this()
        {
            span = unixStyleTime;
        }

        public TimeSpan(double year, double day, double hour, double minute, double second, bool zeroMode) : this()
        {
            ZeroMode = zeroMode;
            SetYear(year);
            SetDay(day);
            SetHour(hour);
            SetMinute(minute);
            SetSecond(second);
        }

        // Required for all IDumpers for them to work, but can't enforced by the interface because it's static:
        public static TimeSpan CreateFromDump(SafeSharedObjects shared, Dump d)
        {
            var newObj = new TimeSpan();
            newObj.LoadDump(d);
            return newObj;
        }

        private void InitializeSuffixes()
        {
            AddSuffix("YEAR", new SetSuffix<ScalarValue>(CalculateYear, SetYear));
            AddSuffix("DAY", new SetSuffix<ScalarValue>(CalculateDay, SetDay));
            AddSuffix("HOUR", new SetSuffix<ScalarValue>(CalculateHour, SetHour));
            AddSuffix("MINUTE", new SetSuffix<ScalarValue>(CalculateMinute, SetMinute));
            AddSuffix("SECOND", new SetSuffix<ScalarValue>(CalculateSecond, SetSecond));
            AddSuffix("SECONDS", new SetSuffix<ScalarValue>(() => span, value => span = value));
            AddSuffix("CLOCK", new Suffix<StringValue>(() => string.Format("{0:00}:{1:00}:{2:00}", (int)CalculateHour(), (int)CalculateMinute(), (int)CalculateSecond())));
            AddSuffix("CALENDAR", new Suffix<StringValue>(() => "Year " + CalculateYear() + ", day " + CalculateDay()));
            AddSuffix("ZEROMODE", new SetSuffix<BooleanValue>(() => ZeroMode, value => ZeroMode = value));
        }

        private ScalarValue CalculateYear()
        {
            return (int)Math.Floor(span / SecondsPerYear) + (ZeroMode ? 0 : 1);
        }

        private void SetYear(ScalarValue newYear)
        {
            // First remove the year and leave just the remainder:
            span -= (CalculateYear() - (ZeroMode ? 0 : 1));

            // Then add the new year value back in:
            span += (ZeroMode ? newYear : newYear - 1) * SecondsPerYear;
        }

        private ScalarValue CalculateDay()
        {
            return (int)Math.Floor(span % SecondsPerYear / SecondsPerDay) + (ZeroMode ? 0 : 1);
        }

        private void SetDay(ScalarValue newDay)
        {
            // First remove the day part:
            span -= (CalculateDay() - (ZeroMode ? 0 : 1)) * SecondsPerDay;

            // Then add the new day value back in:
            span += (ZeroMode ? newDay : newDay - 1) * SecondsPerDay;
        }

        private ScalarValue CalculateHour()
        {
            return (int)Math.Floor(span % SecondsPerDay / SecondsPerHour);
        }

        private void SetHour(ScalarValue newHour)
        {
            // First remove the Hour part:
            span -= CalculateHour() * SecondsPerHour;

            // Then add the new Hour value back in:
            span += newHour * SecondsPerHour;
        }

        private ScalarValue CalculateMinute()
        {
            return (int)Math.Floor(span % SecondsPerHour / SecondsPerMinute);
        }

        private void SetMinute(ScalarValue newMinute)
        {
            // First remove the Minute part:
            span -= CalculateMinute() * SecondsPerMinute;

            // Then add the new Minute value back in:
            span += newMinute * SecondsPerMinute;
        }

        private ScalarValue CalculateSecond()
        {
            return (int)Math.Floor(span % SecondsPerMinute);
        }

        private void SetSecond(ScalarValue newSecond)
        {
            // First remove the Seconds part:
            span -= CalculateSecond();

            // Then add the new Seconds value back in:
            span += newSecond;
        }

        public double ToUnixStyleTime()
        {
            return span;
        }

        public static TimeSpan operator +(TimeSpan a, TimeSpan b) { return new TimeSpan(a.ToUnixStyleTime() + b.ToUnixStyleTime()); }
        public static TimeSpan operator -(TimeSpan a, TimeSpan b) { return new TimeSpan(a.ToUnixStyleTime() - b.ToUnixStyleTime()); }
        public static TimeSpan operator +(TimeSpan a, double b) { return new TimeSpan(a.ToUnixStyleTime() + b); }
        public static TimeSpan operator -(TimeSpan a, double b) { return new TimeSpan(a.ToUnixStyleTime() - b); }
        public static TimeSpan operator *(TimeSpan a, double b) { return new TimeSpan(a.ToUnixStyleTime() * b); }
        public static TimeSpan operator /(TimeSpan a, double b) { return new TimeSpan(a.ToUnixStyleTime() / b); }
        public static TimeSpan operator +(double b, TimeSpan a) { return new TimeSpan(b + a.ToUnixStyleTime()); }
        public static TimeSpan operator -(double b, TimeSpan a) { return new TimeSpan(b - a.ToUnixStyleTime()); }
        public static TimeSpan operator *(double b, TimeSpan a) { return new TimeSpan(b * a.ToUnixStyleTime()); }
        public static TimeSpan operator /(double b, TimeSpan a) { return new TimeSpan(b / a.ToUnixStyleTime()); }
        public static TimeSpan operator /(TimeSpan b, TimeSpan a) { return new TimeSpan(b.ToUnixStyleTime() / a.ToUnixStyleTime()); }
        public static bool operator >(TimeSpan a, TimeSpan b) { return a.ToUnixStyleTime() > b.ToUnixStyleTime(); }
        public static bool operator <(TimeSpan a, TimeSpan b) { return a.ToUnixStyleTime() < b.ToUnixStyleTime(); }
        public static bool operator >=(TimeSpan a, TimeSpan b) { return a.ToUnixStyleTime() >= b.ToUnixStyleTime(); }
        public static bool operator <=(TimeSpan a, TimeSpan b) { return a.ToUnixStyleTime() <= b.ToUnixStyleTime(); }
        public static bool operator >(TimeSpan a, double b) { return a.ToUnixStyleTime() > b; }
        public static bool operator <(TimeSpan a, double b) { return a.ToUnixStyleTime() < b; }
        public static bool operator >=(TimeSpan a, double b) { return a.ToUnixStyleTime() >= b; }
        public static bool operator <=(TimeSpan a, double b) { return a.ToUnixStyleTime() <= b; }
        public static bool operator >(double a, TimeSpan b) { return a > b.ToUnixStyleTime(); }
        public static bool operator <(double a, TimeSpan b) { return a < b.ToUnixStyleTime(); }
        public static bool operator >=(double a, TimeSpan b) { return a >= b.ToUnixStyleTime(); }
        public static bool operator <=(double a, TimeSpan b) { return a <= b.ToUnixStyleTime(); }

        public static TimeSpan operator +(TimeSpan a, ScalarValue b) { return new TimeSpan(a.ToUnixStyleTime() + b); }
        public static TimeSpan operator -(TimeSpan a, ScalarValue b) { return new TimeSpan(a.ToUnixStyleTime() - b); }
        public static TimeSpan operator *(TimeSpan a, ScalarValue b) { return new TimeSpan(a.ToUnixStyleTime() * b); }
        public static TimeSpan operator /(TimeSpan a, ScalarValue b) { return new TimeSpan(a.ToUnixStyleTime() / b); }
        public static TimeSpan operator +(ScalarValue b, TimeSpan a) { return new TimeSpan(b + a.ToUnixStyleTime()); }
        public static TimeSpan operator -(ScalarValue b, TimeSpan a) { return new TimeSpan(b - a.ToUnixStyleTime()); }
        public static TimeSpan operator *(ScalarValue b, TimeSpan a) { return new TimeSpan(b * a.ToUnixStyleTime()); }
        public static TimeSpan operator /(ScalarValue b, TimeSpan a) { return new TimeSpan(b / a.ToUnixStyleTime()); }
        public static bool operator >(TimeSpan a, ScalarValue b) { return a.ToUnixStyleTime() > b; }
        public static bool operator <(TimeSpan a, ScalarValue b) { return a.ToUnixStyleTime() < b; }
        public static bool operator >=(TimeSpan a, ScalarValue b) { return a.ToUnixStyleTime() >= b; }
        public static bool operator <=(TimeSpan a, ScalarValue b) { return a.ToUnixStyleTime() <= b; }
        public static bool operator >(ScalarValue a, TimeSpan b) { return a > b.ToUnixStyleTime(); }
        public static bool operator <(ScalarValue a, TimeSpan b) { return a < b.ToUnixStyleTime(); }
        public static bool operator >=(ScalarValue a, TimeSpan b) { return a >= b.ToUnixStyleTime(); }
        public static bool operator <=(ScalarValue a, TimeSpan b) { return a <= b.ToUnixStyleTime(); }

        public override bool Equals(object obj)
        {
            Type compareType = typeof(TimeSpan);
            if (compareType.IsInstanceOfType(obj))
            {
                TimeSpan t = obj as TimeSpan;
                // Check the equality of the span value
                return span == t.ToUnixStyleTime();
            }
            return false;
        }

        public override int GetHashCode()
        {
            return span.GetHashCode();
        }

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

        public override string ToString()
        {
            return string.Format("TIME({0:0})", span);
        }

        public override Dump Dump()
        {
            var dump = new Dump
            {
                {DumpSpan, span},
                {DumpZeroMode, ZeroMode}
            };

            return dump;
        }

        public override void LoadDump(Dump dump)
        {
            span = Convert.ToDouble(dump[DumpSpan]);
            ZeroMode = Convert.ToBoolean(dump[DumpZeroMode]);
        }
            
        public int CompareTo(TimeSpan other)
        {
            return span.CompareTo(other.span);
        }
    }
}
