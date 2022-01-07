using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Compilation;
using kOS.Safe.Serialization;
using kOS.Safe;

namespace kOS.Suffixed
{
    /// <summary>
    /// The kind of time that refers to a fixed point in time and therefore
    /// starts counting days and years at 1.
    /// </summary>
    [kOS.Safe.Utilities.KOSNomenclature("TimeStamp")]
    public class TimeStamp : TimeBase, IComparable<TimeStamp>
    {
        public const string DumpName = "timestamp";

        /// <summary>
        /// Override with either 0 or 1 for whether counting years and days starts counting at 0 or at 1.
        /// </summary>
        protected override double CountOffset { get { return 1.0; } }

        public TimeStamp(double unixStyleTime) : base(unixStyleTime)
        {
        }

        /// <summary>
        /// Make a new TimeStamp giving it all the fields.  Note this uses TimeStamp's convention of counting
        /// years and days starting at one, not zero.  In other words you create a time of zero seconds
        /// since epoch by passing in (1, 1, 0, 0, 0).  (contrast that with TimeSpan's simlar
        /// consturctor where you'd pass in (0, 0, 0, 0, 0).)
        /// </summary>
        /// <param name="year"></param>
        /// <param name="day"></param>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        public TimeStamp(double year, double day, double hour, double minute, double second) : this(0)
        {
                seconds =
                    (year - 1) * SecondsPerYear +
                    (day - 1) * SecondsPerDay +
                    hour * SecondsPerHour +
                    minute * SecondsPerMinute +
                    second;
        }

        protected override void InitializeSuffixes()
        {
            AddSuffix("YEAR", new SetSuffix<ScalarValue>(CalculateYear, value => ChangeYear(value)));
            AddSuffix("DAY", new SetSuffix<ScalarValue>(CalculateDay, value => ChangeDay(value)));
            AddSuffix("HOUR", new SetSuffix<ScalarValue>(CalculateHour, value => ChangeHour(value)));
            AddSuffix("MINUTE", new SetSuffix<ScalarValue>(CalculateMinute, value => ChangeMinute(value)));
            AddSuffix("SECOND", new SetSuffix<ScalarValue>(CalculateSecond, value => ChangeSecond(value)));
            AddSuffix("SECONDS", new Suffix<ScalarValue>(() => seconds));
            AddSuffix("FULL", new Suffix<StringValue>(() => string.Format("Year {0} Day {1} {2,2:00}:{3,2:00}:{4,2:00}", (int)CalculateYear(), (int)CalculateDay(), (int)CalculateHour(), (int)CalculateMinute(), (int)CalculateSecond())));
            AddSuffix("CLOCK", new Suffix<StringValue>(() => string.Format("{0,2:00}:{1,2:00}:{2,2:00}", (int)CalculateHour(), (int)CalculateMinute(), (int)CalculateSecond())));
            AddSuffix("CALENDAR", new Suffix<StringValue>(() => string.Format("Year {0} Day {1}", CalculateYear(),  CalculateDay())));
        }
        
        //
        // Binary arithmetic operators in which both operands are TimeStamp:
        //
        public static TimeStamp operator +(TimeStamp a, TimeStamp b) { throw new KOSBinaryOperandTypeException(new OperandPair(a, b), "add", "to"); }
        public static TimeSpan operator -(TimeStamp a, TimeStamp b) { return new TimeSpan(a.ToUnixStyleTime() - b.ToUnixStyleTime()); }
        public static TimeStamp operator *(TimeStamp a, TimeStamp b) { throw new KOSBinaryOperandTypeException(new OperandPair(a, b), "multiply", "by"); }
        public static TimeStamp operator /(TimeStamp a, TimeStamp b) { throw new KOSBinaryOperandTypeException(new OperandPair(a, b), "divide", "by"); }
        //
        // Binary arithmetic operators on TimeStamp and a (Double or Scalar):
        // Assume when adding or subtracting that the scalar is a number of seconds.
        // Assume when multiplying or dividing that the scalar is unit-less:
        //
        public static TimeStamp operator +(TimeStamp a, double b) { return new TimeStamp(a.ToUnixStyleTime() + b); }
        public static TimeStamp operator -(TimeStamp a, double b) { return new TimeStamp(a.ToUnixStyleTime() - b); }
        public static TimeStamp operator *(TimeStamp a, double b) { throw new KOSBinaryOperandTypeException(new OperandPair(a, b), "multiply", "by"); }
        public static TimeStamp operator /(TimeStamp a, double b) { throw new KOSBinaryOperandTypeException(new OperandPair(a, b), "divide", "by"); }
        public static TimeStamp operator +(double a, TimeStamp b) { return new TimeStamp(a + b.ToUnixStyleTime()); }
        public static TimeStamp operator -(double a, TimeStamp b) { return new TimeStamp(a - b.ToUnixStyleTime()); }
        public static TimeStamp operator *(double a, TimeStamp b) { throw new KOSBinaryOperandTypeException(new OperandPair(a, b), "multiply", "by"); }
        public static TimeStamp operator /(double a, TimeStamp b) { throw new KOSBinaryOperandTypeException(new OperandPair(a, b), "divide", "by"); }
        public static TimeStamp operator +(TimeStamp a, ScalarValue b) { return new TimeStamp(a.ToUnixStyleTime() + b); }
        public static TimeStamp operator -(TimeStamp a, ScalarValue b) { return new TimeStamp(a.ToUnixStyleTime() - b); }
        public static TimeStamp operator *(TimeStamp a, ScalarValue b) { throw new KOSBinaryOperandTypeException(new OperandPair(a, b), "multiply", "by"); }
        public static TimeStamp operator /(TimeStamp a, ScalarValue b) { throw new KOSBinaryOperandTypeException(new OperandPair(a, b), "divide", "by"); ; }
        public static TimeStamp operator +(ScalarValue a, TimeStamp b) { return new TimeStamp(a + b.ToUnixStyleTime()); }
        public static TimeStamp operator -(ScalarValue a, TimeStamp b) { return new TimeStamp(a - b.ToUnixStyleTime()); }
        public static TimeStamp operator *(ScalarValue a, TimeStamp b) { throw new KOSBinaryOperandTypeException(new OperandPair(a, b), "divide", "by"); }
        public static TimeStamp operator /(ScalarValue a, TimeStamp b) { throw new KOSBinaryOperandTypeException(new OperandPair(a, b), "divide", "by"); }
        //
        // Binary comparison operators where both operands are TimeStamps:
        //
        public static bool operator >(TimeStamp a, TimeStamp b) { return a.ToUnixStyleTime() > b.ToUnixStyleTime(); }
        public static bool operator <(TimeStamp a, TimeStamp b) { return a.ToUnixStyleTime() < b.ToUnixStyleTime(); }
        public static bool operator >=(TimeStamp a, TimeStamp b) { return a.ToUnixStyleTime() >= b.ToUnixStyleTime(); }
        public static bool operator <=(TimeStamp a, TimeStamp b) { return a.ToUnixStyleTime() <= b.ToUnixStyleTime(); }
        //
        // Binary comparison operators between a TimeStamp and a (Scalar or Double):
        // Assume the scalar is a Unix-Style seconds since epoch when comparing:
        //
        public static bool operator >(TimeStamp a, double b) { return a.ToUnixStyleTime() > b; }
        public static bool operator <(TimeStamp a, double b) { return a.ToUnixStyleTime() < b; }
        public static bool operator >=(TimeStamp a, double b) { return a.ToUnixStyleTime() >= b; }
        public static bool operator <=(TimeStamp a, double b) { return a.ToUnixStyleTime() <= b; }
        public static bool operator >(double a, TimeStamp b) { return a > b.ToUnixStyleTime(); }
        public static bool operator <(double a, TimeStamp b) { return a < b.ToUnixStyleTime(); }
        public static bool operator >=(double a, TimeStamp b) { return a >= b.ToUnixStyleTime(); }
        public static bool operator <=(double a, TimeStamp b) { return a <= b.ToUnixStyleTime(); }
        public static bool operator >(TimeStamp a, ScalarValue b) { return a.ToUnixStyleTime() > b; }
        public static bool operator <(TimeStamp a, ScalarValue b) { return a.ToUnixStyleTime() < b; }
        public static bool operator >=(TimeStamp a, ScalarValue b) { return a.ToUnixStyleTime() >= b; }
        public static bool operator <=(TimeStamp a, ScalarValue b) { return a.ToUnixStyleTime() <= b; }
        public static bool operator >(ScalarValue a, TimeStamp b) { return a > b.ToUnixStyleTime(); }
        public static bool operator <(ScalarValue a, TimeStamp b) { return a < b.ToUnixStyleTime(); }
        public static bool operator >=(ScalarValue a, TimeStamp b) { return a >= b.ToUnixStyleTime(); }
        public static bool operator <=(ScalarValue a, TimeStamp b) { return a <= b.ToUnixStyleTime(); }
        //
        // Binary arithmetic operators where one operand is a TimeStamp and the other is a TimeSpan:
        // These could have been defined either here in TimeStamp or in TimeSpan, but to keep it in one
        // place they've all been defined here:
        //
        public static TimeStamp operator +(TimeStamp a, TimeSpan b) { return new TimeStamp(a.ToUnixStyleTime() + b.ToUnixStyleTime()); }
        public static TimeStamp operator -(TimeStamp a, TimeSpan b) { return new TimeStamp(a.ToUnixStyleTime() - b.ToUnixStyleTime()); }
        public static TimeStamp operator *(TimeStamp a, TimeSpan b) { throw new KOSBinaryOperandTypeException(new OperandPair(a, b), "multiply", "by"); }
        public static TimeStamp operator /(TimeStamp a, TimeSpan b) { throw new KOSBinaryOperandTypeException(new OperandPair(a, b), "divide", "by"); }
        public static TimeStamp operator +(TimeSpan a, TimeStamp b) { return new TimeStamp(a.ToUnixStyleTime() + b.ToUnixStyleTime()); }
        public static TimeStamp operator -(TimeSpan a, TimeStamp b) { return new TimeStamp(a.ToUnixStyleTime() - b.ToUnixStyleTime()); }
        public static TimeStamp operator *(TimeSpan a, TimeStamp b) { throw new KOSBinaryOperandTypeException(new OperandPair(a, b), "multiply", "by"); }
        public static TimeStamp operator /(TimeSpan a, TimeStamp b) { throw new KOSBinaryOperandTypeException(new OperandPair(a, b), "divide", "by"); }

        public override bool Equals(object obj)
        {
            Type compareType = typeof(TimeStamp);
            if (compareType.IsInstanceOfType(obj))
            {
                TimeStamp t = obj as TimeStamp;
                // Check the equality of the span value
                return seconds == t.ToUnixStyleTime();
            }
            return false;
        }

        public override int GetHashCode()
        {
            return seconds.GetHashCode();
        }

        public static bool operator ==(TimeStamp a, TimeStamp b)
        {
            Type compareType = typeof(TimeStamp);
            if (compareType.IsInstanceOfType(a))
            {
                return a.Equals(b); // a is not null, we can use the built in equals function
            }
            return !compareType.IsInstanceOfType(b); // a is null, return true if b is null and false if not null
        }

        public static bool operator !=(TimeStamp a, TimeStamp b)
        {
            return !(a == b);
        }

        public override string ToString()
        {
            return string.Format("TIMESTAMP({0})", seconds);
        }

        public override Dump Dump(DumperState s)
        {
            DumpDictionary dump = new DumpDictionary(this.GetType());

            dump.Add(DumpName, seconds);

            return dump;
        }

        [DumpDeserializer]
        public static TimeStamp CreateFromDump(DumpDictionary d, SafeSharedObjects shared)
        {
            double seconds = d.GetDouble(DumpName);

            return new TimeStamp(seconds);
        }

        [DumpPrinter]
        public static void Print(DumpDictionary d, IndentedStringBuilder sb)
        {
            double seconds = d.GetDouble(DumpName);

            sb.Append(string.Format("TIMESTAMP({0})", seconds));
        }
        
        public int CompareTo(TimeStamp other)
        {
            return seconds.CompareTo(other.seconds);
        }
    }
}
