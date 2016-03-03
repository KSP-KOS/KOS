using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;

namespace kOS.Safe.Compilation
{
    public class CalculatorString : Calculator
    {
        public override object Add(OperandPair pair)
        {
            return new StringValue(string.Concat(pair.Left, pair.Right));
        }

        public override object Subtract(OperandPair pair)
        {
            throw new KOSBinaryOperandTypeException(pair, "subtract", "from");
        }

        public override object Multiply(OperandPair pair)
        {
            throw new KOSBinaryOperandTypeException(pair, "multiply", "by");
        }

        public override object Divide(OperandPair pair)
        {
            throw new KOSBinaryOperandTypeException(pair, "divide", "by");
        }

        public override object Power(OperandPair pair)
        {
            throw new KOSBinaryOperandTypeException(pair, "exponentiate", "by");
        }

        public override object GreaterThan(OperandPair pair)
        {
            int compareNum = string.Compare(pair.Left.ToString(), pair.Right.ToString(), StringComparison.OrdinalIgnoreCase);
            return compareNum > 0;
        }

        public override object LessThan(OperandPair pair)
        {
            int compareNum = string.Compare(pair.Left.ToString(), pair.Right.ToString(), StringComparison.OrdinalIgnoreCase);
            return compareNum < 0;
        }

        public override object GreaterThanEqual(OperandPair pair)
        {
            int compareNum = string.Compare(pair.Left.ToString(), pair.Right.ToString(), StringComparison.OrdinalIgnoreCase);
            return compareNum >= 0;
        }

        public override object LessThanEqual(OperandPair pair)
        {
            int compareNum = string.Compare(pair.Left.ToString(), pair.Right.ToString(), StringComparison.OrdinalIgnoreCase);
            return compareNum <= 0;
        }

        public override object NotEqual(OperandPair pair)
        {
            return !string.Equals(pair.Left.ToString(), pair.Right.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        
        public override object Equal(OperandPair pair)
        {
            return string.Equals(pair.Left.ToString(), pair.Right.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        public override object Min(OperandPair pair)
        {
            string arg1 = pair.Left.ToString();
            string arg2 = pair.Right.ToString();
            int compareNum = string.Compare(arg1, arg2, StringComparison.OrdinalIgnoreCase);
            return (compareNum < 0) ? arg1 : arg2;
        }

        public override object Max(OperandPair pair)
        {
            string arg1 = pair.Left.ToString();
            string arg2 = pair.Right.ToString();
            int compareNum = string.Compare(arg1, arg2, StringComparison.OrdinalIgnoreCase);
            return (compareNum > 0) ? arg1 : arg2;
        }
    }
}