﻿using System;
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

        double span;
        private const int DAYS_IN_YEAR = 365;

        public const int HOURS_IN_EARTH_DAY = 24;
        public const int HOURS_IN_KERBIN_DAY = 6;
        
        private const int MINUTE_IN_HOUR = 60;
        private const int SECONDS_IN_MINUTE = 60;

        private const int SECONDS_IN_KERBIN_HOUR = MINUTE_IN_HOUR * SECONDS_IN_MINUTE;
        private const int SECONDS_IN_KERBIN_DAY = SECONDS_IN_KERBIN_HOUR * HOURS_IN_KERBIN_DAY;
        private const int SECONDS_IN_KERBIN_YEAR = SECONDS_IN_KERBIN_DAY * DAYS_IN_YEAR;
        private const int SECONDS_IN_EARTH_HOUR = MINUTE_IN_HOUR * SECONDS_IN_MINUTE;
        private const int SECONDS_IN_EARTH_DAY = SECONDS_IN_EARTH_HOUR * HOURS_IN_EARTH_DAY;
        private const int SECONDS_IN_EARTH_YEAR = SECONDS_IN_EARTH_DAY * DAYS_IN_YEAR;

        public TimeSpan()
        {
            InitializeSuffixes();
        }

        public TimeSpan(double unixStyleTime) : this()
        {
            span = unixStyleTime;
        }

        private void InitializeSuffixes()
        {
            AddSuffix("YEAR", new Suffix<ScalarValue>(CalculateYear));
            AddSuffix("DAY", new Suffix<ScalarValue>(CalculateDay));
            AddSuffix("HOUR", new Suffix<ScalarValue>(CalculateHour));
            AddSuffix("MINUTE", new Suffix<ScalarValue>(CalculateMinute));
            AddSuffix("SECOND", new Suffix<ScalarValue>(CalculateSecond));
            AddSuffix("SECONDS", new Suffix<ScalarValue>(() => span));
            AddSuffix("CLOCK", new Suffix<StringValue>(() => string.Format("{0:00}:{1:00}:{2:00}", CalculateHour(), CalculateMinute(), CalculateSecond())));
            AddSuffix("CALENDAR", new Suffix<StringValue>(() => "Year " + CalculateYear() + ", day " + CalculateDay()));
        }

        private ScalarValue CalculateYear()
        {
            if (GameSettings.KERBIN_TIME)
            {
                return (int)Math.Floor(span / SECONDS_IN_KERBIN_YEAR) + 1;
            }
            return (int)Math.Floor(span / SECONDS_IN_EARTH_YEAR) + 1;
        }

        private int SecondsPerDay { get { return GameSettings.KERBIN_TIME ? SECONDS_IN_KERBIN_DAY : SECONDS_IN_EARTH_DAY; } }
        private int SecondsPerHour { get { return GameSettings.KERBIN_TIME ? SECONDS_IN_KERBIN_HOUR : SECONDS_IN_EARTH_HOUR; } }
        private int SecongsPerYear { get { return GameSettings.KERBIN_TIME ? SECONDS_IN_KERBIN_YEAR : SECONDS_IN_EARTH_YEAR; } }

        private ScalarValue CalculateDay()
        {
            return (int)Math.Floor(span % SecongsPerYear / SecondsPerDay) + 1;
        }

        private ScalarValue CalculateHour()
        {
            return (int)Math.Floor(span % SecondsPerDay / SecondsPerHour);
        }

        private ScalarValue CalculateMinute()
        {
            return (int)Math.Floor(span % SecondsPerHour / SECONDS_IN_MINUTE);
        }

        private ScalarValue CalculateSecond()
        {
            return span%SECONDS_IN_MINUTE;
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

        public override Dump Dump()
        {
            var dump = new Dump
            {
                {DumpSpan, span}
            };

            return dump;
        }

        public override void LoadDump(Dump dump)
        {
            span = Convert.ToDouble(dump[DumpSpan]);
        }
            
        public int CompareTo(TimeSpan other)
        {
            return span.CompareTo(other.span);
        }
    }
}
