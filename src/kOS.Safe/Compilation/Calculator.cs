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

        public abstract Type GetAddResultType(Type leftType, Type rightType);
        public abstract Type GetSubtractResultType(Type leftType, Type rightType);
        public abstract Type GetMultiplyResultType(Type leftType, Type rightType);
        public abstract Type GetDivideResultType(Type leftType, Type rightType);
        public abstract Type GetPowerResultType(Type leftType, Type rightType);
        public virtual Type GetGreaterThanResultType(Type leftType, Type rightType) => typeof(BooleanValue);
        public virtual Type GetLessThanResultType(Type leftType, Type rightType) => typeof(BooleanValue);
        public virtual Type GetGreaterThanEqualResultType(Type leftType, Type rightType) => typeof(BooleanValue);
        public virtual Type GetLessThanEqualResultType(Type leftType, Type rightType) => typeof(BooleanValue);
        public virtual Type GetNotEqualResultType(Type leftType, Type rightType) => typeof(BooleanValue);
        public virtual Type GetEqualResultType(Type leftType, Type rightType) => typeof(BooleanValue);

        private static CalculatorScalar calculatorScalar;
        private static CalculatorString calculatorString;
        private static CalculatorBool calculatorBool;
        private static CalculatorStructure calculatorStructure;

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

            if (scalarCount == 2) return calculatorScalar ?? (calculatorScalar = new CalculatorScalar());
            if (stringCount > 0) return calculatorString ?? (calculatorString = new CalculatorString());
            if (boolCount > 0) return calculatorBool ?? (calculatorBool = new CalculatorBool());
            if (specialCount > 0) return calculatorStructure ?? (calculatorStructure = new CalculatorStructure());

            throw new NotImplementedException(string.Format("Can't operate types {0} and {1}", operandPair.Left.GetType(), operandPair.Right.GetType()));
        }

        public static Calculator GetCalculator(Type leftType, Type rightType)
        {
            var scalarCount = 0;
            var stringCount = 0;
            var specialCount = 0;
            var boolCount = 0;

            if (typeof(ScalarValue).IsAssignableFrom(leftType)) scalarCount++;
            if (typeof(StringValue).IsAssignableFrom(leftType)) stringCount++;
            if (typeof(ISuffixed).IsAssignableFrom(leftType)) specialCount++;
            if (typeof(BooleanValue).IsAssignableFrom(leftType)) boolCount++;
            if (typeof(ScalarValue).IsAssignableFrom(rightType)) scalarCount++;
            if (typeof(StringValue).IsAssignableFrom(rightType)) stringCount++;
            if (typeof(ISuffixed).IsAssignableFrom(rightType)) specialCount++;
            if (typeof(BooleanValue).IsAssignableFrom(rightType)) boolCount++;

            if (scalarCount == 2) return calculatorScalar ?? (calculatorScalar = new CalculatorScalar());
            if (stringCount > 0) return calculatorString ?? (calculatorString = new CalculatorString());
            if (boolCount > 0) return calculatorBool ?? (calculatorBool = new CalculatorBool());
            if (specialCount > 0) return calculatorStructure ?? (calculatorStructure = new CalculatorStructure());

            throw new NotImplementedException(string.Format("Can't operate types {0} and {1}", leftType, rightType));
        }
    }
}