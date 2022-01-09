using System;
using System.Globalization;
using kOS.Safe.Serialization;

namespace kOS.Safe.Encapsulation
{
    [kOS.Safe.Utilities.KOSNomenclature("Scalar", KOSToCSharp = false)]
    public class ScalarDoubleValue : ScalarValue
    {
        public static ScalarDoubleValue Zero = new ScalarDoubleValue(0);

        public double Value { get; private set; }

        public override bool IsDouble
        {
            get { return true; }
        }

        public override bool IsInt
        {
            get { return false; }
        }

        public override int GetIntValue() { return (int)Value; }
        public override double GetDoubleValue() { return Value; }

        public override bool BooleanMeaning
        {
            get { return (double)Value != 0d; }
        }
        // All serializable structures need a default constructor even if it's not public:
        private ScalarDoubleValue()
        {

        }
        public ScalarDoubleValue(double value)
        {
            Value = value;
        }

        public static implicit operator ScalarDoubleValue(double val)
        {
            return new ScalarDoubleValue(val);
        }

        public static ScalarDoubleValue MinValue()
        {
            return new ScalarDoubleValue(double.MinValue);
        }

        public static ScalarDoubleValue MaxValue()
        {
            return new ScalarDoubleValue(double.MaxValue);
        }

        public override Dump Dump(DumperState s)
        {
            DumpDictionary dump = new DumpDictionary(typeof(ScalarDoubleValue));

            dump.Add("value", Value);

            return dump;
        }

        [DumpDeserializer]
        public static ScalarDoubleValue CreateFromDump(DumpDictionary d, SafeSharedObjects shared)
        {
            return new ScalarDoubleValue(d.GetDouble("value"));
        }

        [DumpPrinter]
        public static void Print(DumpDictionary d, IndentedStringBuilder sb)
        {
            sb.Append(d.GetDouble("value").ToString(CultureInfo.InvariantCulture));
        }

        public override object ToPrimitive()
        {
            return Value;
        }
    }
}