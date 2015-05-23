using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;

namespace kOS.Safe.Compilation
{
    public abstract class Calculator
    {
        public abstract object Add(object argument1, object argument2);
        public abstract object Subtract(object argument1, object argument2);
        public abstract object Multiply(object argument1, object argument2);
        public abstract object Divide(object argument1, object argument2);
        public abstract object Power(object argument1, object argument2);
        public abstract object GreaterThan(object argument1, object argument2);
        public abstract object LessThan(object argument1, object argument2);
        public abstract object GreaterThanEqual(object argument1, object argument2);
        public abstract object LessThanEqual(object argument1, object argument2);
        public abstract object NotEqual(object argument1, object argument2);
        public abstract object Equal(object argument1, object argument2);
        public abstract object Min(object argument1, object argument2);
        public abstract object Max(object argument1, object argument2);

        public static Calculator GetCalculator(object argument1, object argument2)
        {
            int intCount = 0;
            int doubleCount = 0;
            int stringCount = 0;
            int specialCount = 0;
            int boolCount = 0;

            // convert floats to doubles
            if (argument1 is float) argument1 = Convert.ToDouble(argument1);
            if (argument2 is float) argument2 = Convert.ToDouble(argument2);

            if (argument1 is int) intCount++;
            if (argument1 is double) doubleCount++;
            if (argument1 is string) stringCount++;
            if (argument1 is ISuffixed) specialCount++;
            if (argument1 is bool) boolCount++;
            if (argument2 is int) intCount++;
            if (argument2 is double) doubleCount++;
            if (argument2 is string) stringCount++;
            if (argument2 is ISuffixed) specialCount++;
            if (argument2 is bool) boolCount++;

            if (intCount == 2) return new CalculatorIntInt();
            if (doubleCount == 2) return new CalculatorDoubleDouble();
            if (intCount == 1 && doubleCount == 1) return new CalculatorIntDouble();
            if (stringCount > 0) return new CalculatorString();
            if (boolCount > 0) return new CalculatorBool();
            if (specialCount > 0) return new CalculatorStructure();

            throw new NotImplementedException(string.Format("Can't operate types {0} and {1}", argument1.GetType(), argument2.GetType()));
        }
    }

    public class CalculatorIntInt : Calculator
    {
        /// <summary>If result is too big to fit in an int32, then turn it
        /// into a double (since kOS doesn't do long's):</summary>
        /// <param name="result">The value to maybe promote.  Use long so it
        /// is capable of storing numbers too big for int.</param>
        /// <returns>the value, possibly promoted to double</returns>
        private object PromoteIfTooBig(long result)
        {
            if (Math.Abs(result) <= int.MaxValue) return (int)result;
            return (double)result;
        }

        /// <summary>If result is too big to fit in an int32, then turn it
        /// into a double (since kOS doesn't do long's):</summary>
        /// <param name="result">The value to maybe promote.</param>
        /// <returns>the value, possibly promoted to double</returns>
        private object PromoteIfTooBig(double result)
        {
            if (Math.Abs(result) <= int.MaxValue) return (int)result;
            return (double)result;
        }

        public override object Add(object argument1, object argument2)
        {
            // C# doesn't know how to both unbox the object and promote it in the same cast, thus the (long)(int) syntax:
            return PromoteIfTooBig((long)(int)argument1 + (long)(int)argument2);
        }

        public override object Subtract(object argument1, object argument2)
        {
            // C# doesn't know how to both unbox the object and promote it in the same cast, thus the (long)(int) syntax:
            return PromoteIfTooBig((long)(int)argument1 - (long)(int)argument2);
        }

        public override object Multiply(object argument1, object argument2)
        {
            // C# doesn't know how to both unbox the object and promote it in the same cast, thus the (long)(int) syntax:
            return PromoteIfTooBig((long)(int)argument1 * (long)(int)argument2);
        }

        public override object Divide(object argument1, object argument2)
        {
            // Avoid integer division truncation.  Make double
            // if there's a fractional part to preserve:
            int remainder = (int)argument1 % (int)argument2;
            if (remainder == 0)
                return (int)argument1 / (int)argument2;
            else
                return Convert.ToDouble((int)argument1) /(int)argument2;
        }

        public override object Power(object argument1, object argument2)
        {
            // If the exponent is negative, then integer power operations would normally
            // round to zero (i.e. 4^(-2) is (1/16) which rounds to zero).  This
            // checks for that condition and if it happens it turns into a double operation:
            if ((int)argument2 < 0)
                return Math.Pow(Convert.ToDouble((int)argument1), Convert.ToDouble((int)argument2));
            else
                // C# doesn't know how to both unbox the object and promote it in the same cast, thus the (long)(int) syntax:
                return PromoteIfTooBig(Math.Pow((long)(int)argument1, (long)(int)argument2));
        }

        public override object GreaterThan(object argument1, object argument2)
        {
            return (int)argument1 > (int)argument2;
        }

        public override object LessThan(object argument1, object argument2)
        {
            return (int)argument1 < (int)argument2;
        }

        public override object GreaterThanEqual(object argument1, object argument2)
        {
            return (int)argument1 >= (int)argument2;
        }

        public override object LessThanEqual(object argument1, object argument2)
        {
            return (int)argument1 <= (int)argument2;
        }

        public override object NotEqual(object argument1, object argument2)
        {
            return (int)argument1 != (int)argument2;
        }

        public override object Equal(object argument1, object argument2)
        {
            return (int)argument1 == (int)argument2;
        }

        public override object Min(object argument1, object argument2)
        {
            return Math.Min((int)argument1, (int)argument2);
        }

        public override object Max(object argument1, object argument2)
        {
            return Math.Max((int)argument1, (int)argument2);
        }
    }

    public class CalculatorDoubleDouble : Calculator
    {
        /// <summary>
        /// Turn the double into an integer if the value is
        /// a round number without a fractional component,
        /// and it's small enough magnitude to fit in an int.
        /// </summary>
        /// <param name="result">the value to maybe demote</param>
        /// <returns>the value, possibly demoted</returns>
        public object DemoteIfRound(double result)
        {
            if (Math.Floor(result) == result && (Math.Abs(result) <= int.MaxValue))
                return (int)result;
            return (double)result;            
        }

        public override object Add(object argument1, object argument2)
        {
            return DemoteIfRound((double)argument1 + (double)argument2);
        }

        public override object Subtract(object argument1, object argument2)
        {
            return DemoteIfRound((double)argument1 - (double)argument2);
        }

        public override object Multiply(object argument1, object argument2)
        {
            return DemoteIfRound((double)argument1 * (double)argument2);
        }

        public override object Divide(object argument1, object argument2)
        {
            return DemoteIfRound((double)argument1 / (double)argument2);
        }

        public override object Power(object argument1, object argument2)
        {
            return DemoteIfRound(Math.Pow((double)argument1, (double)argument2));
        }

        public override object GreaterThan(object argument1, object argument2)
        {
            return (double)argument1 > (double)argument2;
        }

        public override object LessThan(object argument1, object argument2)
        {
            return (double)argument1 < (double)argument2;
        }

        public override object GreaterThanEqual(object argument1, object argument2)
        {
            return (double)argument1 >= (double)argument2;
        }

        public override object LessThanEqual(object argument1, object argument2)
        {
            return (double)argument1 <= (double)argument2;
        }

        public override object NotEqual(object argument1, object argument2)
        {
            return (double)argument1 != (double)argument2;
        }

        public override object Equal(object argument1, object argument2)
        {
            return (double)argument1 == (double)argument2;
        }

        public override object Min(object argument1, object argument2)
        {
            return Math.Min((double)argument1, (double)argument2);
        }

        public override object Max(object argument1, object argument2)
        {
            return Math.Max((double)argument1, (double)argument2);
        }
    }

    public class CalculatorIntDouble : Calculator
    {
        /// <summary>
        /// Turn the double into an integer if the value is
        /// a round number without a fractional component,
        /// and it's small enough magnitude to fit in an int.
        /// </summary>
        /// <param name="result">the value to maybe demote</param>
        /// <returns>the value, possibly demoted</returns>
        public object DemoteIfRound(double result)
        {
            if (Math.Floor(result) == result && (Math.Abs(result) <= int.MaxValue))
                return (int)result;
            return (double)result;            
        }

        public override object Add(object argument1, object argument2)
        {
            if (argument1 is int) return DemoteIfRound((int)argument1 + (double)argument2);
            return DemoteIfRound((double)argument1 + (int)argument2);
        }

        public override object Subtract(object argument1, object argument2)
        {
            if (argument1 is int) return DemoteIfRound((int)argument1 - (double)argument2);
            return DemoteIfRound((double)argument1 - (int)argument2);
        }

        public override object Multiply(object argument1, object argument2)
        {
            if (argument1 is int) return DemoteIfRound((int)argument1 * (double)argument2);
            return DemoteIfRound((double)argument1 * (int)argument2);
        }

        public override object Divide(object argument1, object argument2)
        {
            if (argument1 is int) return DemoteIfRound((int)argument1 / (double)argument2);
            return DemoteIfRound((double)argument1 / (int)argument2);
        }

        public override object Power(object argument1, object argument2)
        {
            if (argument1 is int) return DemoteIfRound(Math.Pow((int)argument1, (double)argument2));
            return DemoteIfRound(Math.Pow((double)argument1, (int)argument2));
        }

        public override object GreaterThan(object argument1, object argument2)
        {
            if (argument1 is int) return (int)argument1 > (double)argument2;
            return (double)argument1 > (int)argument2;
        }

        public override object LessThan(object argument1, object argument2)
        {
            if (argument1 is int) return (int)argument1 < (double)argument2;
            return (double)argument1 < (int)argument2;
        }

        public override object GreaterThanEqual(object argument1, object argument2)
        {
            if (argument1 is int) return (int)argument1 >= (double)argument2;
            return (double)argument1 >= (int)argument2;
        }

        public override object LessThanEqual(object argument1, object argument2)
        {
            if (argument1 is int) return (int)argument1 <= (double)argument2;
            return (double)argument1 <= (int)argument2;
        }

        public override object NotEqual(object argument1, object argument2)
        {
            if (argument1 is int) return (int)argument1 != (double)argument2;
            return (double)argument1 != (int)argument2;
        }

        public override object Equal(object argument1, object argument2)
        {
            if (argument1 is int) return (int)argument1 == (double)argument2;
            return (double)argument1 == (int)argument2;
        }

        public override object Min(object argument1, object argument2)
        {
            if (argument1 is int) return Math.Min((int)argument1, (double)argument2);
            return Math.Min((double)argument1, (int)argument2);
        }

        public override object Max(object argument1, object argument2)
        {
            if (argument1 is int) return Math.Max((int)argument1, (double)argument2);
            return Math.Max((double)argument1, (int)argument2);
        }
    }

    public class CalculatorString : Calculator
    {
        public override object Add(object argument1, object argument2)
        {
            return argument1.ToString() + argument2.ToString();
        }

        public override object Subtract(object argument1, object argument2)
        {
            throw new KOSBinaryOperandTypeException(argument2, "subtract", "from", argument1);
        }

        public override object Multiply(object argument1, object argument2)
        {
            throw new KOSBinaryOperandTypeException(argument1, "multiply", "by", argument2);
        }

        public override object Divide(object argument1, object argument2)
        {
            throw new KOSBinaryOperandTypeException(argument1, "divide", "by", argument2);
        }

        public override object Power(object argument1, object argument2)
        {
            throw new KOSBinaryOperandTypeException(argument1, "exponentiate", "by", argument2);
        }

        public override object GreaterThan(object argument1, object argument2)
        {
            return argument1.ToString().Length > argument2.ToString().Length;
        }

        public override object LessThan(object argument1, object argument2)
        {
            return argument1.ToString().Length < argument2.ToString().Length;
        }

        public override object GreaterThanEqual(object argument1, object argument2)
        {
            return argument1.ToString().Length >= argument2.ToString().Length;
        }

        public override object LessThanEqual(object argument1, object argument2)
        {
            return argument1.ToString().Length <= argument2.ToString().Length;
        }

        public override object NotEqual(object argument1, object argument2)
        {
            return argument1.ToString().ToLower() != argument2.ToString().ToLower();
        }
        
        public override object Equal(object argument1, object argument2)
        {
            return argument1.ToString().ToLower() == argument2.ToString().ToLower();
        }

        public override object Min(object argument1, object argument2)
        {
            string arg1 = argument1.ToString();
            string arg2 = argument2.ToString();
            return (arg1.Length < arg2.Length) ? arg1 : arg2;
        }

        public override object Max(object argument1, object argument2)
        {
            string arg1 = argument1.ToString();
            string arg2 = argument2.ToString();
            return (arg1.Length > arg2.Length) ? arg1 : arg2;
        }
    }

    public class CalculatorBool : Calculator
    {
        public override object Add(object argument1, object argument2)
        {
            throw new KOSBinaryOperandTypeException(argument2, "add", "to", argument1);
        }

        public override object Subtract(object argument1, object argument2)
        {
            throw new KOSBinaryOperandTypeException(argument2, "subtract", "from", argument1);
        }

        public override object Multiply(object argument1, object argument2)
        {
            throw new KOSBinaryOperandTypeException(argument2, "multiply", "by", argument1);
        }

        public override object Divide(object argument1, object argument2)
        {
            throw new KOSBinaryOperandTypeException(argument1, "divide", "by", argument2);
        }

        public override object Power(object argument1, object argument2)
        {
            throw new KOSBinaryOperandTypeException(argument1, "exponentiate", "by", argument2);
        }

        public override object GreaterThan(object argument1, object argument2)
        {
            throw new KOSBinaryOperandTypeException(argument1, "ordinate", ">", argument2);
        }

        public override object LessThan(object argument1, object argument2)
        {
            throw new KOSBinaryOperandTypeException(argument1, "ordinate", "<", argument2);
        }

        public override object GreaterThanEqual(object argument1, object argument2)
        {
            throw new KOSBinaryOperandTypeException(argument1, "ordinate", ">=", argument2);
        }

        public override object LessThanEqual(object argument1, object argument2)
        {
            throw new KOSBinaryOperandTypeException(argument1, "ordinate", "<=", argument2);
        }

        public override object NotEqual(object argument1, object argument2)
        {
            return Convert.ToBoolean(argument1) != Convert.ToBoolean(argument2);
        }

        public override object Equal(object argument1, object argument2)
        {
            return Convert.ToBoolean(argument1) == Convert.ToBoolean(argument2);
        }

        public override object Min(object argument1, object argument2)
        {
            throw new KOSBinaryOperandTypeException(argument1, "get minimum of", "and", argument2);
        }

        public override object Max(object argument1, object argument2)
        {
            throw new KOSBinaryOperandTypeException(argument1, "get maximum of", "and", argument2);
        }
    }

    public class CalculatorStructure : Calculator
    {
        private object Calculate(string op, object argument1, object argument2)
        {
            if (argument1 is IOperable) return ((IOperable)argument1).TryOperation(op, argument2, false);
            return ((IOperable)argument2).TryOperation(op, argument1, true);
        }

        public override object Add(object argument1, object argument2)
        {
            return Calculate("+", argument1, argument2);
        }

        public override object Subtract(object argument1, object argument2)
        {
            return Calculate("-", argument1, argument2);
        }

        public override object Multiply(object argument1, object argument2)
        {
            return Calculate("*", argument1, argument2);
        }

        public override object Divide(object argument1, object argument2)
        {
            return Calculate("/", argument1, argument2);
        }

        public override object Power(object argument1, object argument2)
        {
            return null;
        }

        public override object GreaterThan(object argument1, object argument2)
        {
            return Calculate(">", argument1, argument2);
        }

        public override object LessThan(object argument1, object argument2)
        {
            return Calculate("<", argument1, argument2);
        }

        public override object GreaterThanEqual(object argument1, object argument2)
        {
            return Calculate(">=", argument1, argument2);
        }

        public override object LessThanEqual(object argument1, object argument2)
        {
            return Calculate("<=", argument1, argument2);
        }

        public override object NotEqual(object argument1, object argument2)
        {
            return Calculate("<>", argument1, argument2);
        }

        public override object Equal(object argument1, object argument2)
        {
            return Calculate("==", argument1, argument2);
        }

        public override object Min(object argument1, object argument2)
        {
            return Calculate("min", argument1, argument2);
        }

        public override object Max(object argument1, object argument2)
        {
            return Calculate("max", argument1, argument2);
        }
    }
}
