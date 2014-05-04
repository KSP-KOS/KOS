using System;

namespace kOS.Suffixed
{
    public class TimeSpan : SpecialValue
    {
        readonly double span;
        private const int DAYS_IN_YEAR = 365;
        private const int HOURS_IN_DAY = 6;
        private const int MINUTE_IN_HOUR = 60;
        private const int SECONDS_IN_MINUTE = 60;

        private const int SECONDS_IN_HOUR = MINUTE_IN_HOUR * SECONDS_IN_MINUTE;
        private const int SECONDS_IN_DAY = SECONDS_IN_HOUR * HOURS_IN_DAY;
        private const int SECONDS_IN_YEAR = SECONDS_IN_DAY * DAYS_IN_YEAR;

        public TimeSpan(double unixStyleTime)
        {
            UnityEngine.Debug.Log("kOS: Timespan Input: " + unixStyleTime);
            span = unixStyleTime;
            UnityEngine.Debug.Log("kOS: Timespan Span: " + span);
        }

        private int CalculateYear()
        {
            return (int)Math.Floor(span / SECONDS_IN_YEAR) + 1;
        }

        private int CalculateDay()
        {
            var secondsThisYear = span%SECONDS_IN_YEAR;
            return (int)Math.Floor(secondsThisYear / SECONDS_IN_DAY) + 1;
        }

        private int CalculateHour()
        {
            var secondsToday = span%SECONDS_IN_DAY;
            return (int)Math.Floor(secondsToday / SECONDS_IN_HOUR);
        }

        private int CalculateMinute()
        {
            var secondsThisHour = span%SECONDS_IN_HOUR;
            return (int)Math.Floor(secondsThisHour / SECONDS_IN_MINUTE);
        }

        private double CalculateSecond()
        {
            return span%SECONDS_IN_MINUTE;
        }

        public double ToUnixStyleTime()
        {
            return span;
        }

        public override object GetSuffix(string suffixName)
        {
            if (suffixName == "YEAR") return CalculateYear();
            if (suffixName == "DAY") return CalculateDay();
            if (suffixName == "HOUR") return CalculateHour();
            if (suffixName == "MINUTE") return CalculateMinute();
            if (suffixName == "SECOND") return CalculateSecond();

            if (suffixName == "SECONDS") return span;

            if (suffixName == "CLOCK") return string.Format("{0:00}:{1:00}:{2:00}", CalculateHour(), CalculateMinute(), CalculateSecond());
            if (suffixName == "CALENDAR") return "Year " + CalculateYear() + ", day " + CalculateDay();

            return base.GetSuffix(suffixName);
        }

        public static TimeSpan operator +(TimeSpan a, TimeSpan b) { return new TimeSpan(a.ToUnixStyleTime() + b.ToUnixStyleTime()); }
        public static TimeSpan operator -(TimeSpan a, TimeSpan b) { return new TimeSpan(a.ToUnixStyleTime() - b.ToUnixStyleTime()); }
        public static TimeSpan operator +(TimeSpan a, double b) { return new TimeSpan(a.ToUnixStyleTime() + b); }
        public static TimeSpan operator -(TimeSpan a, double b) { return new TimeSpan(a.ToUnixStyleTime() - b); }
        public static TimeSpan operator *(TimeSpan a, double b) { return new TimeSpan(a.ToUnixStyleTime() * b); }
        public static TimeSpan operator /(TimeSpan a, double b) { return new TimeSpan(a.ToUnixStyleTime() / b); }
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

        public override object TryOperation(string op, object other, bool reverseOrder)
        {
            other = ConvertToDoubleIfNeeded(other);

            // Order shouldn't matter here
            if (other is TimeSpan && op == "+") return this + (TimeSpan)other;
            if (other is double && op == "+") return this + (double)other;
            if (other is double && op == "*") return this * (double)other; // Order would matter here if this were matrices

            if (!reverseOrder)
            {
                if (other is TimeSpan && op == "-") return this - (TimeSpan)other;
                if (other is TimeSpan && op == "/") return this / (TimeSpan)other;
                if (other is double && op == "-") return this - (double)other;
                if (other is double && op == "/") return this / (double)other;
                if (other is TimeSpan && op == ">") return this > (TimeSpan)other;
                if (other is TimeSpan && op == "<") return this < (TimeSpan)other;
                if (other is TimeSpan && op == ">=") return this >= (TimeSpan)other;
                if (other is TimeSpan && op == "<=") return this <= (TimeSpan)other;
                if (other is double && op == ">") return this > (double)other;
                if (other is double && op == "<") return this < (double)other;
                if (other is double && op == ">=") return this >= (double)other;
                if (other is double && op == "<=") return this <= (double)other;
            }
            else
            {
                if (other is TimeSpan && op == "-") return (TimeSpan)other - this;
                if (other is TimeSpan && op == "/") return (TimeSpan)other / this;
                if (other is double && op == "-") return (double)other - this;
               if (other is double && op == "/") return (double)other / this; // Can't imagine why the heck you'd want to do this but here it is
               if (other is TimeSpan && op == ">") return this < (TimeSpan)other;
               if (other is TimeSpan && op == "<") return this > (TimeSpan)other;
               if (other is TimeSpan && op == ">=") return (TimeSpan)other >= this;
               if (other is TimeSpan && op == "<=") return (TimeSpan)other <= this;
               if (other is double && op == ">") return (double)other > this;
               if (other is double && op == "<") return (double)other < this;
               if (other is double && op == ">=") return (double)other >= this;
               if (other is double && op == "<=") return (double)other <= this;

            }

            return base.TryOperation(op, other, reverseOrder);
        }

        public override string ToString()
        {
            return string.Format("TIME({0:0})", span);
        }
    }
}
