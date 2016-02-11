using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;
using kOS.Safe.Encapsulation.Suffixes;
using System.Collections.Generic;
using kOS.Safe.Serialization;
using System.Collections;

namespace kOS.Safe
{
    public class RangeValue : EnumerableValue<ScalarIntValue, Range>
    {
        private const string DumpFrom = "from";
        private const string DumpTo = "to";
        private const string DumpStep = "step";
        private const string Label = "RANGE";

        public RangeValue() : this(0)
        {
        }

        public RangeValue(int to) : this(0, to, 1)
        {
        }

        public RangeValue(int from, int to) : this(from, to, 1)
        {
        }

        public RangeValue(int from, int to, int step) : base(Label, new Range(from, to, step))
        {
            InitializeRangeSuffixes();

            if (step < 1) {
                throw new KOSException("Step must be a positive integer");
            }
        }

        private void InitializeRangeSuffixes()
        {
            AddSuffix("FROM",   new NoArgsSuffix<ScalarValue>(() => InnerEnum.From));
            AddSuffix("TO",     new NoArgsSuffix<ScalarValue>(() => InnerEnum.To));
            AddSuffix("STEP",   new NoArgsSuffix<ScalarValue>(() => InnerEnum.Step));
        }

        public override void LoadDump(Dump dump)
        {
            InnerEnum.To = Convert.ToInt32(dump[DumpTo]);
            InnerEnum.From = Convert.ToInt32(dump[DumpFrom]);
            InnerEnum.Step = Convert.ToInt32(dump[DumpStep]);
        }

        public override Dump Dump()
        {
            DumpWithHeader result = new DumpWithHeader();

            result.Header = "RANGE";

            result.Add(DumpTo, InnerEnum.To);
            result.Add(DumpFrom, InnerEnum.From);
            result.Add(DumpStep, InnerEnum.Step);

            return result;
        }

        public override string ToString()
        {
            return "RANGE(" + InnerEnum.From + ", " + InnerEnum.To + ", " + InnerEnum.Step + ")";
        }
    }

    public class Range : IEnumerable<ScalarIntValue> {

        public int From { get; set; }
        public int To { get; set; }
        public int Step  { get; set; }

        public Range(int from, int to, int step)
        {
            From = from;
            To = to;
            Step = step;
        }

        IEnumerator<ScalarIntValue> IEnumerable<ScalarIntValue>.GetEnumerator()
        {
            if (From < To) {
                for (int i = From; i < To; i += Step) {
                    yield return i;
                }
            } else {
                for (int i = From; i > To; i -= Step) {
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

