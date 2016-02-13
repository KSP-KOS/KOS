using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;

namespace kOS.Safe.Compilation
{
    public class CalculatorString : Calculator
    {
        public static void ThrowIfNotStrings(OperandPair pair)
        {
            if (!(pair.Left is string))
            {
                throw new KOSCastException(pair.Left.GetType(), typeof(string));
            }
            if (!(pair.Right is string))
            {
                throw new KOSCastException(pair.Right.GetType(), typeof(string));
            }
        }
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
            ThrowIfNotStrings(pair);
            return pair.Left.ToString().Length > pair.Right.ToString().Length;
        }

        public override object LessThan(OperandPair pair)
        {
            ThrowIfNotStrings(pair);
            return pair.Left.ToString().Length < pair.Right.ToString().Length;
        }

        public override object GreaterThanEqual(OperandPair pair)
        {
            ThrowIfNotStrings(pair);
            return pair.Left.ToString().Length >= pair.Right.ToString().Length;
        }

        public override object LessThanEqual(OperandPair pair)
        {
            ThrowIfNotStrings(pair);
            return pair.Left.ToString().Length <= pair.Right.ToString().Length;
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
            ThrowIfNotStrings(pair);
            string arg1 = pair.Left.ToString();
            string arg2 = pair.Right.ToString();
            return (arg1.Length < arg2.Length) ? arg1 : arg2;
        }

        public override object Max(OperandPair pair)
        {
            ThrowIfNotStrings(pair);
            string arg1 = pair.Left.ToString();
            string arg2 = pair.Right.ToString();
            return (arg1.Length > arg2.Length) ? arg1 : arg2;
        }
    }
}