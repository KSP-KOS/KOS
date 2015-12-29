using kOS.Safe.Encapsulation;
using System;

namespace kOS.Safe.Compilation
{
    public abstract class Calculator
    {
        public abstract object Add(OperandPair pair);
        public abstract object Subtract(OperandPair pair);
        public abstract object Multiply(OperandPair pair);
        public abstract object Divide(OperandPair pair);
        public abstract object Power(OperandPair pair);
        public abstract object GreaterThan(OperandPair pair);
        public abstract object LessThan(OperandPair pair);
        public abstract object GreaterThanEqual(OperandPair pair);
        public abstract object LessThanEqual(OperandPair pair);
        public abstract object NotEqual(OperandPair pair);
        public abstract object Equal(OperandPair pair);
        public abstract object Min(OperandPair pair);
        public abstract object Max(OperandPair pair);

        public static Calculator GetCalculator(OperandPair operandPair)
        {
            var scalarCount = 0;
            var stringCount = 0;
            var specialCount = 0;
            var boolCount = 0;

            if (operandPair.Left is ScalarValue) scalarCount++;
            if (operandPair.Left is StringValue) stringCount++;
            if (operandPair.Left is ISuffixed) specialCount++;
            if (operandPair.Left is BooleanValue) boolCount++;
            if (operandPair.Right is ScalarValue) scalarCount++;
            if (operandPair.Right is StringValue) stringCount++;
            if (operandPair.Right is ISuffixed) specialCount++;
            if (operandPair.Right is BooleanValue) boolCount++;

            if (scalarCount == 2) return new CalculatorScalar();
            if (stringCount > 0) return new CalculatorString();
            if (boolCount > 0) return new CalculatorBool();
            if (specialCount > 0) return new CalculatorStructure();

            throw new NotImplementedException(string.Format("Can't operate types {0} and {1}", operandPair.Left.GetType(), operandPair.Right.GetType()));
        }
    }
}