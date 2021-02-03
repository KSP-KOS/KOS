using System;
using kOS.Safe.Serialization;

namespace kOS.Safe.Encapsulation
{
    [kOS.Safe.Utilities.KOSNomenclature("Scalar", KOSToCSharp = false)]
    public class ScalarDoubleValue : ScalarValue
    {
        public static ScalarDoubleValue Zero = new ScalarDoubleValue(0);

        public override bool IsDouble
        {
            get { return true; }
        }

        public override bool IsInt
        {
            get { return false; }
        }

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

        // Required for all IDumpers for them to work, but can't enforced by the interface because it's static:
        public static ScalarDoubleValue CreateFromDump(SafeSharedObjects shared, Dump d)
        {
            var newObj = new ScalarDoubleValue();
            newObj.LoadDump(d);
            return newObj;
        }
        public override Dump Dump()
        {
            DumpWithHeader dump = new DumpWithHeader();

            dump.Add("value", Value);

            return dump;
        }
        public override void LoadDump(Dump dump)
        {
            Value = Convert.ToDouble(dump["value"]);
        }
    }
}