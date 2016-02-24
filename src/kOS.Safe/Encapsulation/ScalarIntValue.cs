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
    }
}