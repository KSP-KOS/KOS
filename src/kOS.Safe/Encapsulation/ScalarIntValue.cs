using kOS.Safe.Serialization;
using System;

namespace kOS.Safe.Encapsulation
{
    [kOS.Safe.Utilities.KOSNomenclature("Scalar", KOSToCSharp = false)]
    public class ScalarIntValue : ScalarValue
    {
        // those are handy especially in tests
        public static ScalarIntValue Zero = new ScalarIntValue(0);
        public static ScalarIntValue One = new ScalarIntValue(1);
        public static ScalarIntValue Two = new ScalarIntValue(2);

        public override bool IsDouble
        {
            get { return false; }
        }

        public override bool IsInt
        {
            get { return true; }
        }

        public override bool BooleanMeaning
        {
            get { return (int)Value != 0; }
        }

        // All serializable structures need a default constructor, but it can
        // be private to prevent it from being used externally:
        private ScalarIntValue()
        {

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

        // Required for all IDumpers for them to work, but can't enforced by the interface because it's static:
        public static ScalarIntValue CreateFromDump(SafeSharedObjects shared, Dump d)
        {
            var newObj = new ScalarIntValue();
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
            Value = Convert.ToInt32(dump["value"]);
        }
    }
}