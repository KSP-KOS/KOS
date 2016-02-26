using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Serialization;
using System;
using System.Collections.Generic;

namespace kOS.Safe
{
    [kOS.Safe.Utilities.KOSNomenclature("Range")]
    public class RangeValue : EnumerableValue<ScalarIntValue, Range>
    {
        private const string DumpFrom = "from";
        private const string DumpTo = "to";
        private const string DumpStep = "step";
        private const string Label = "RANGE";

        public static readonly int DEFAULT_FROM = 0;
        public static readonly int DEFAULT_TO = 1;
        public static readonly int DEFAULT_STEP = 1;

        public RangeValue()
            : this(DEFAULT_TO)
        {
        }

        public RangeValue(int to)
            : this(DEFAULT_FROM, to)
        {
        }

        public RangeValue(int from, int to)
            : this(from, to, DEFAULT_STEP)
        {
        }

        public RangeValue(int from, int to, int step)
            : base(Label, new Range(from, to, step))
        {
            InitializeRangeSuffixes();

            if (step < 1)
            {
                throw new KOSException("Step must be a positive integer");
            }
        }

        private void InitializeRangeSuffixes()
        {
            AddSuffix("FROM", new NoArgsSuffix<ScalarValue>(() => InnerEnumerable.From));
            AddSuffix("TO", new NoArgsSuffix<ScalarValue>(() => InnerEnumerable.To));
            AddSuffix("STEP", new NoArgsSuffix<ScalarValue>(() => InnerEnumerable.Step));
        }

        public override void LoadDump(Dump dump)
        {
            InnerEnumerable.To = Convert.ToInt32(dump[DumpTo]);
            InnerEnumerable.From = Convert.ToInt32(dump[DumpFrom]);
            InnerEnumerable.Step = Convert.ToInt32(dump[DumpStep]);
        }

        public override Dump Dump()
        {
            DumpWithHeader result = new DumpWithHeader();

            result.Header = "RANGE";

            result.Add(DumpTo, InnerEnumerable.To);
            result.Add(DumpFrom, InnerEnumerable.From);
            result.Add(DumpStep, InnerEnumerable.Step);

            return result;
        }

        public override string ToString()
        {
            return "RANGE(" + InnerEnumerable.From + ", " + InnerEnumerable.To + ", " + InnerEnumerable.Step + ")";
        }
    }

    public class Range : IEnumerable<ScalarIntValue>
    {
        public int From { get; set; }
        public int To { get; set; }
        public int Step { get; set; }

        public Range(int from, int to, int step)
        {
            From = from;
            To = to;
            Step = step;
        }

        IEnumerator<ScalarIntValue> IEnumerable<ScalarIntValue>.GetEnumerator()
        {
            if (From < To)
            {
                for (int i = From; i < To; i += Step)
                {
                    yield return i;
                }
            }
            else
            {
                for (int i = From; i > To; i -= Step)
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