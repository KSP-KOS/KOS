namespace kOS.Safe.Encapsulation
{
    public class ScalarIntValue : ScalarValue
    {
        public override bool IsDouble
        {
            get { return false; }
        }

        public override bool IsInt
        {
            get { return true; }
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

    }
}