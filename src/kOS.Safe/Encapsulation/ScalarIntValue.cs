using kOS.Safe.Serialization;
using System;
using System.Globalization;

namespace kOS.Safe.Encapsulation
{
    [kOS.Safe.Utilities.KOSNomenclature("Scalar", KOSToCSharp = false)]
    public class ScalarIntValue : ScalarValue
    {
        // those are handy especially in tests
        public static ScalarIntValue Zero = new ScalarIntValue(0);
        public static ScalarIntValue One = new ScalarIntValue(1);
        public static ScalarIntValue Two = new ScalarIntValue(2);

        public int Value { get; private set; }

        public override bool IsDouble
        {
            get { return false; }
        }

        public override bool IsInt
        {
            get { return true; }
        }

        public override int GetIntValue() { return Value; }
        public override double GetDoubleValue() { return Value; }

        public override bool BooleanMeaning
        {
            get { return Value != 0; }
        }

        public ScalarIntValue(int value)
        {
            Value = value;
        }

        public static implicit operator ScalarIntValue(int val)
        {
            return new ScalarIntValue(val);
        }

        public static ScalarIntValue MinValue()
        {
            return new ScalarIntValue(int.MinValue);
        }

        public static ScalarIntValue MaxValue()
        {
            return new ScalarIntValue(int.MaxValue);
        }

        public override Dump Dump(DumperState s)
        {
            DumpDictionary dump = new DumpDictionary(typeof(ScalarIntValue));

            dump.Add("value", Value);

            return dump;
        }

        [DumpDeserializer]
        public static ScalarIntValue CreateFromDump(DumpDictionary d, SafeSharedObjects shared)
        {
            return new ScalarIntValue((int)d.GetDouble("value"));
        }

        [DumpPrinter]
        public static void Print(DumpDictionary d, IndentedStringBuilder sb)
        {
            sb.Append(((int)d.GetDouble("value")).ToString(CultureInfo.InvariantCulture));
        }

        public override object ToPrimitive()
        {
            return Value;
        }
    }
}