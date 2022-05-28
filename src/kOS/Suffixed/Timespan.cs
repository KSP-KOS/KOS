using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Compilation;
using kOS.Safe.Serialization;
using System.Collections.Generic;
using kOS.Safe;

namespace kOS.Suffixed
{
    /// <summary>
    /// The kind of time that refers to differences between two times,
    /// and therefore it counts starting at zero for years and days.
    /// </summary>
    [kOS.Safe.Utilities.KOSNomenclature("TimeSpan")]
    public class TimeSpan : TimeBase, IComparable<TimeSpan>
    {
        public override string DumpName { get { return "timespan"; } }
        
        /// <summary>
        /// Override with either 0 or 1 for whether counting years and days starts counting at 0 or at 1.
        /// </summary>
        protected override double CountOffset { get { return 0.0; } }

        // Only used by CreateFromDump() and the other constructors.
        // Don't make it public because it leaves fields
        // unpopulated:
        private TimeSpan()
        {
            InitializeSuffixes();
        }

        public TimeSpan(double unixStyleTime) : base(unixStyleTime)
        {
        }

        /// <summary>
        /// Make a new TimeSpan giving it all the fields.   Note this uses TimeSpan's convention of counting
        /// years and days starting at zero not one.  In other words you create a time of zero seconds
        /// since epoch by passing in (0, 0, 0, 0, 0).  (contrast that with TimeStamp's simlar
        /// consturctor where you'd pass in (1, 1, 0, 0, 0).)
        /// </summary>
        /// <param name="year"></param>
        /// <param name="day"></param>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        public TimeSpan(double year, double day, double hour, double minute, double second) : this()
        {
            seconds =
                year * SecondsPerYear +
                day * SecondsPerDay +
                hour * SecondsPerHour +
                minute * SecondsPerMinute +
                second;
        }

        public static TimeSpan CreateFromDump(SafeSharedObjects shared, Dump d)
        {
            var newObj = new TimeSpan();
            newObj.LoadDump(d);
            return newObj;
        }

        protected override void InitializeSuffixes()
        {
            AddSuffix("YEAR", new SetSuffix<ScalarValue>(CalculateYear, value => ChangeYear(value)));
            AddSuffix("YEARS", new SetSuffix<ScalarValue>(() => seconds / SecondsPerYear, value => seconds = value * SecondsPerYear));
            AddSuffix("DAY", new SetSuffix<ScalarValue>(CalculateDay, value =>ChangeDay(value)));
            AddSuffix("DAYS", new SetSuffix<ScalarValue>(() => seconds / SecondsPerDay, value => seconds = value * SecondsPerDay));
            AddSuffix("HOUR", new SetSuffix<ScalarValue>(CalculateHour, value => ChangeHour(value)));
            AddSuffix("HOURS", new SetSuffix<ScalarValue>(() => seconds / SecondsPerHour, value => seconds = value * SecondsPerHour));
            AddSuffix("MINUTE", new SetSuffix<ScalarValue>(CalculateMinute, value => ChangeMinute(value)));
            AddSuffix("MINUTES", new SetSuffix<ScalarValue>(() => seconds / SecondsPerMinute, value => seconds = value * SecondsPerMinute));
            AddSuffix("SECOND", new SetSuffix<ScalarValue>(CalculateSecond, value => ChangeSecond(value)));
            AddSuffix("SECONDS", new SetSuffix<ScalarValue>(() => seconds, value => seconds = value));
            AddSuffix("FULL", new Suffix<StringValue>(() => string.Format("{0}y{1}d{2}h{3}m{4}s", (int)CalculateYear(), (int)CalculateDay(), (int)CalculateHour(), (int)CalculateMinute(), (int)CalculateSecond())));
        }

        //
        // Binary arithmetic operators where both operands are TimeSpans:
        //
        public static TimeSpan operator +(TimeSpan a, TimeSpan b) { return new TimeSpan(a.ToUnixStyleTime() + b.ToUnixStyleTime()); }
        public static TimeSpan operator -(TimeSpan a, TimeSpan b) { return new TimeSpan(a.ToUnixStyleTime() - b.ToUnixStyleTime()); }
        public static TimeSpan operator *(TimeSpan a, TimeSpan b) { throw new KOSBinaryOperandTypeException(new OperandPair(a, b), "multiply", "by"); }
        public static TimeSpan operator /(TimeSpan a, TimeSpan b) { throw new KOSBinaryOperandTypeException(new OperandPair(a, b), "divide", "by"); }
        //
        // Binary arithmetic operators where one operand is TimeSpan and the other is (Double or Scalar):
        // Assume the scalars are seconds when doing addition or subtraction operations:
        // Assume the scalars are unit-less when doing multiplication or division operations:
        //
        public static TimeSpan operator +(TimeSpan a, double b) { return new TimeSpan(a.ToUnixStyleTime() + b); }
        public static TimeSpan operator -(TimeSpan a, double b) { return new TimeSpan(a.ToUnixStyleTime() - b); }
        public static TimeSpan operator *(TimeSpan a, double b) { return new TimeSpan(a.ToUnixStyleTime() * b); }
        public static TimeSpan operator /(TimeSpan a, double b) { return new TimeSpan(a.ToUnixStyleTime() / b); }
        public static TimeSpan operator +(double a, TimeSpan b) { return new TimeSpan(a + b.ToUnixStyleTime()); }
        public static TimeSpan operator -(double a, TimeSpan b) { return new TimeSpan(a - b.ToUnixStyleTime()); }
        public static TimeSpan operator *(double a, TimeSpan b) { return new TimeSpan(a * b.ToUnixStyleTime()); }
        public static TimeSpan operator /(double a, TimeSpan b) { throw new KOSBinaryOperandTypeException(new OperandPair(a, b), "divide", "by"); }
        public static TimeSpan operator +(TimeSpan a, ScalarValue b) { return new TimeSpan(a.ToUnixStyleTime() + b); }
        public static TimeSpan operator -(TimeSpan a, ScalarValue b) { return new TimeSpan(a.ToUnixStyleTime() - b); }
        public static TimeSpan operator *(TimeSpan a, ScalarValue b) { return new TimeSpan(a.ToUnixStyleTime() * b); }
        public static TimeSpan operator /(TimeSpan a, ScalarValue b) { return new TimeSpan(a.ToUnixStyleTime() / b); }
        public static TimeSpan operator +(ScalarValue b, TimeSpan a) { return new TimeSpan(b + a.ToUnixStyleTime()); }
        public static TimeSpan operator -(ScalarValue b, TimeSpan a) { return new TimeSpan(b - a.ToUnixStyleTime()); }
        public static TimeSpan operator *(ScalarValue b, TimeSpan a) { return new TimeSpan(b * a.ToUnixStyleTime()); }
        public static TimeSpan operator /(ScalarValue b, TimeSpan a) { throw new KOSBinaryOperandTypeException(new OperandPair(a, b), "divide", "by"); }
        //
        // Binary comparison operators where both operands are TimeSpans:
        //
        public static bool operator >(TimeSpan a, TimeSpan b) { return a.ToUnixStyleTime() > b.ToUnixStyleTime(); }
        public static bool operator <(TimeSpan a, TimeSpan b) { return a.ToUnixStyleTime() < b.ToUnixStyleTime(); }
        public static bool operator >=(TimeSpan a, TimeSpan b) { return a.ToUnixStyleTime() >= b.ToUnixStyleTime(); }
        public static bool operator <=(TimeSpan a, TimeSpan b) { return a.ToUnixStyleTime() <= b.ToUnixStyleTime(); }
        //
        // Binary comparison operators between a TimeSpan and a (Double or Scalar):
        // Assume the scalar is a number of seconds in these cases:
        //
        public static bool operator >(TimeSpan a, double b) { return a.ToUnixStyleTime() > b; }
        public static bool operator <(TimeSpan a, double b) { return a.ToUnixStyleTime() < b; }
        public static bool operator >=(TimeSpan a, double b) { return a.ToUnixStyleTime() >= b; }
        public static bool operator <=(TimeSpan a, double b) { return a.ToUnixStyleTime() <= b; }
        public static bool operator >(double a, TimeSpan b) { return a > b.ToUnixStyleTime(); }
        public static bool operator <(double a, TimeSpan b) { return a < b.ToUnixStyleTime(); }
        public static bool operator >=(double a, TimeSpan b) { return a >= b.ToUnixStyleTime(); }
        public static bool operator <=(double a, TimeSpan b) { return a <= b.ToUnixStyleTime(); }
        public static bool operator >(TimeSpan a, ScalarValue b) { return a.ToUnixStyleTime() > b; }
        public static bool operator <(TimeSpan a, ScalarValue b) { return a.ToUnixStyleTime() < b; }
        public static bool operator >=(TimeSpan a, ScalarValue b) { return a.ToUnixStyleTime() >= b; }
        public static bool operator <=(TimeSpan a, ScalarValue b) { return a.ToUnixStyleTime() <= b; }
        public static bool operator >(ScalarValue a, TimeSpan b) { return a > b.ToUnixStyleTime(); }
        public static bool operator <(ScalarValue a, TimeSpan b) { return a < b.ToUnixStyleTime(); }
        public static bool operator >=(ScalarValue a, TimeSpan b) { return a >= b.ToUnixStyleTime(); }
        public static bool operator <=(ScalarValue a, TimeSpan b) { return a <= b.ToUnixStyleTime(); }

        //
        // Binary operators in which one operand is a TimeSpan and the other is a TimeStamp:
        // 
        //     --- These can either be defined here in TimeSpan or over in TimeStamp but not both.
        //     --- They were defined over in TimeStamp insteaad of here.
        //

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

        public override int GetHashCode()
        {
            return seconds.GetHashCode();
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
            return string.Format("TIMESPAN({0})", seconds);
        }

        public override Dump Dump()
        {
            var dump = new Dump
            {
                {DumpName, seconds}
            };

            return dump;
        }

        public override void LoadDump(Dump dump)
        {
            seconds = Convert.ToDouble(dump[DumpName]);
        }
            
        public int CompareTo(TimeSpan other)
        {
            return seconds.CompareTo(other.seconds);
        }
    }
}
