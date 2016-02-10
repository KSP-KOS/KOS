using kOS.Safe.Encapsulation;

namespace kOS.Safe.Compilation
{
    public class CalculatorScalar : Calculator
    {
        public override object Add(OperandPair pair)
        {
            return ScalarValue.Create(pair.Left) + ScalarValue.Create(pair.Right);
        }

        public override object Subtract(OperandPair pair)
        {
            return ScalarValue.Create(pair.Left) - ScalarValue.Create(pair.Right);
        }

        public override object Multiply(OperandPair pair)
        {
            return ScalarValue.Create(pair.Left) * ScalarValue.Create(pair.Right);
        }

        public override object Divide(OperandPair pair)
        {
            return ScalarValue.Create(pair.Left) / ScalarValue.Create(pair.Right);
        }

        public override object Power(OperandPair pair)
        {
            return ScalarValue.Create(pair.Left) ^ ScalarValue.Create(pair.Right);
        }

        public override object GreaterThan(OperandPair pair)
        {
            return ScalarValue.Create(pair.Left) > ScalarValue.Create(pair.Right);
        }

        public override object LessThan(OperandPair pair)
        {
            return ScalarValue.Create(pair.Left) < ScalarValue.Create(pair.Right);
        }

        public override object GreaterThanEqual(OperandPair pair)
        {
            return ScalarValue.Create(pair.Left) >= ScalarValue.Create(pair.Right);
        }

        public override object LessThanEqual(OperandPair pair)
        {
            return ScalarValue.Create(pair.Left) <= ScalarValue.Create(pair.Right);
        }

        public override object NotEqual(OperandPair pair)
        {
            return ScalarValue.Create(pair.Left) != ScalarValue.Create(pair.Right);
        }

        public override object Equal(OperandPair pair)
        {
            return ScalarValue.Create(pair.Left) == ScalarValue.Create(pair.Right);
        }

        public override object Min(OperandPair pair)
        {
            return ScalarValue.Min(ScalarValue.Create(pair.Left), ScalarValue.Create(pair.Right));
        }

        public override object Max(OperandPair pair)
        {
            return ScalarValue.Max(ScalarValue.Create(pair.Left), ScalarValue.Create(pair.Right));
        }
    }
}