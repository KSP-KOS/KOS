using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Serialization;
using System;
using System.Collections.Generic;

namespace kOS.Safe
{
    [kOS.Safe.Utilities.KOSNomenclature("Range")]
    public class RangeValue : EnumerableValue<ScalarValue, Range>
    {
        private const string DumpStart = "start";
        private const string DumpStop = "stop";
        private const string DumpStep = "step";
        private const string Label = "RANGE";

        public static readonly int DEFAULT_START = 0;
        public static readonly int DEFAULT_STOP = 1;
        public static readonly int DEFAULT_STEP = 1;

        public RangeValue()
            : this(DEFAULT_STOP)
        {
        }

        public RangeValue(ScalarValue stop)
            : this(DEFAULT_START, stop)
        {
        }

        public RangeValue(ScalarValue start, ScalarValue stop)
            : this(start, stop, DEFAULT_STEP)
        {
        }

        public RangeValue(ScalarValue start, ScalarValue stop, ScalarValue step)
            : base(Label, new Range(start, stop, step))
        {
            InitializeRangeSuffixes();

            if (step < 1)
            {
                throw new KOSException("Step must be a positive integer");
            }
        }

        // Required for all IDumpers for them to work, but can't enforced by the interface because it's static:
        public static RangeValue CreateFromDump(SafeSharedObjects shared, Dump d)
        {
            var newObj = new RangeValue();
            newObj.LoadDump(d);
            return newObj;
        }


        private void InitializeRangeSuffixes()
        {
            AddSuffix("START", new NoArgsSuffix<ScalarValue>(() => InnerEnumerable.Start));
            AddSuffix("STOP", new NoArgsSuffix<ScalarValue>(() => InnerEnumerable.Stop));
            AddSuffix("STEP", new NoArgsSuffix<ScalarValue>(() => InnerEnumerable.Step));
        }

        public override void LoadDump(Dump dump)
        {
            InnerEnumerable.Stop = Convert.ToDouble(dump[DumpStop]);
            InnerEnumerable.Start = Convert.ToDouble(dump[DumpStart]);
            InnerEnumerable.Step = Convert.ToDouble(dump[DumpStep]);
        }

        public override Dump Dump()
        {
            DumpWithHeader result = new DumpWithHeader();

            result.Header = "RANGE";

            result.Add(DumpStop, InnerEnumerable.Stop);
            result.Add(DumpStart, InnerEnumerable.Start);
            result.Add(DumpStep, InnerEnumerable.Step);

            return result;
        }

        public override string ToString()
        {
            return "RANGE(" + InnerEnumerable.Start + ", " + InnerEnumerable.Stop + ", " + InnerEnumerable.Step + ")";
        }

        public override string ToStringIndented(int level)
        {
            // By default, an Enumerable's ToStringIndented() would print out a header line, but here it's
            // not needed, so override it to just print the content line only:
            return ToStringItems(level);
        }
        public override string ToStringItems(int level)
        {
            // Indent level is being ignored because this is single-line and
            // never contains other things, despite being implemented as
            // a EnumerableValue which needs a ToStringItems().
            return ToString();
        }
    }

    public class Range : IEnumerable<ScalarValue>
    {
        public ScalarValue Start { get; set; }
        public ScalarValue Stop { get; set; }
        public ScalarValue Step { get; set; }

        public Range(ScalarValue start, ScalarValue stop, ScalarValue step)
        {
            Start = start;
            Stop = stop;
            Step = step;
        }

        IEnumerator<ScalarValue> IEnumerable<ScalarValue>.GetEnumerator()
        {
            if (Start < Stop)
            {
                for (ScalarValue i = Start; i < Stop; i += Step)
                {
                    yield return i;
                }
            }
            else
            {
                for (ScalarValue i = Start; i > Stop; i -= Step)
                {
                    yield return i;
                }
            }
        }

        public System.Collections.IEnumerator GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}