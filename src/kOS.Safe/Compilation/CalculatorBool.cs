using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;

namespace kOS.Safe.Compilation
{
    public class CalculatorBool : Calculator
    {
        public override object Add(OperandPair pair)
        {
            throw new KOSBinaryOperandTypeException(pair, "add", "to");
        }
        public override Type GetAddResultType(Type leftType, Type rightType)
        {
            throw new KOSBinaryOperandTypeException(leftType, rightType, "add", "to");
        }

        public override object Subtract(OperandPair pair)
        {
            throw new KOSBinaryOperandTypeException(pair, "subtract", "from");
        }
        public override Type GetSubtractResultType(Type leftType, Type rightType)
        {
            throw new KOSBinaryOperandTypeException(leftType, rightType, "subtract", "from");
        }

        public override object Multiply(OperandPair pair)
        {
            throw new KOSBinaryOperandTypeException(pair, "multiply", "by");
        }
        public override Type GetMultiplyResultType(Type leftType, Type rightType)
        {
            throw new KOSBinaryOperandTypeException(leftType, rightType, "multiply", "by");
        }

        public override object Divide(OperandPair pair)
        {
            throw new KOSBinaryOperandTypeException(pair, "divide", "by");
        }
        public override Type GetDivideResultType(Type leftType, Type rightType)
        {
            throw new KOSBinaryOperandTypeException(leftType, rightType, "divide", "by");
        }

        public override object Power(OperandPair pair)
        {
            throw new KOSBinaryOperandTypeException(pair, "exponentiate", "by");
        }
        public override Type GetPowerResultType(Type leftType, Type rightType)
        {
            throw new KOSBinaryOperandTypeException(leftType, rightType, "exponentiate", "by");
        }

        public override object GreaterThan(OperandPair pair)
        {
            throw new KOSBinaryOperandTypeException(pair, "ordinate", ">");
        }

        public override object LessThan(OperandPair pair)
        {
            throw new KOSBinaryOperandTypeException(pair, "ordinate", "<");
        }

        public override object GreaterThanEqual(OperandPair pair)
        {
            throw new KOSBinaryOperandTypeException(pair, "ordinate", ">=");
        }

        public override object LessThanEqual(OperandPair pair)
        {
            throw new KOSBinaryOperandTypeException(pair, "ordinate", "<=");
        }

        public override object NotEqual(OperandPair pair)
        {
            return new BooleanValue(Convert.ToBoolean(pair.Left) != Convert.ToBoolean(pair.Right));
        }

        public override object Equal(OperandPair pair)
        {
            return new BooleanValue(Convert.ToBoolean(pair.Left) == Convert.ToBoolean(pair.Right));
        }
    }
}