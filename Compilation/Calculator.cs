using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Suffixed;

namespace kOS.Compilation
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
            if (argument1 is SpecialValue) specialCount++;
            if (argument1 is bool) boolCount++;
            if (argument2 is int) intCount++;
            if (argument2 is double) doubleCount++;
            if (argument2 is string) stringCount++;
            if (argument2 is SpecialValue) specialCount++;
            if (argument2 is bool) boolCount++;

            if (intCount == 2) return new CalculatorIntInt();
            if (doubleCount == 2) return new CalculatorDoubleDouble();
            if (intCount == 1 && doubleCount == 1) return new CalculatorIntDouble();
            if (stringCount > 0) return new CalculatorString();
            if (boolCount > 0) return new CalculatorBool();
            if (specialCount > 0) return new CalculatorSpecialValue();

            throw new NotImplementedException(string.Format("Can't operate types {0} and {1}", argument1.GetType(), argument2.GetType()));
        }
    }

    public class CalculatorIntInt : Calculator
    {
        private object PromoteToDouble(double result)
        {
            if (Math.Abs(result) <= int.MaxValue) return (int)result;
            else return result;
        }

        public override object Add(object argument1, object argument2)
        {
            return PromoteToDouble((double)(int)argument1 + (int)argument2);
        }

        public override object Subtract(object argument1, object argument2)
        {
            return PromoteToDouble((double)(int)argument1 - (int)argument2);
        }

        public override object Multiply(object argument1, object argument2)
        {
            return PromoteToDouble((double)(int)argument1 * (int)argument2);
        }

        public override object Divide(object argument1, object argument2)
        {
            return (double)(int)argument1 / (int)argument2;
        }

        public override object Power(object argument1, object argument2)
        {
            return PromoteToDouble(Math.Pow((int)argument1, (int)argument2));
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
            return (int)Math.Min((int)argument1, (int)argument2);
        }

        public override object Max(object argument1, object argument2)
        {
            return (int)Math.Max((int)argument1, (int)argument2);
        }
    }

    public class CalculatorDoubleDouble : Calculator
    {
        public override object Add(object argument1, object argument2)
        {
            return (double)argument1 + (double)argument2;
        }

        public override object Subtract(object argument1, object argument2)
        {
            return (double)argument1 - (double)argument2;
        }

        public override object Multiply(object argument1, object argument2)
        {
            return (double)argument1 * (double)argument2;
        }

        public override object Divide(object argument1, object argument2)
        {
            return (double)argument1 / (double)argument2;
        }

        public override object Power(object argument1, object argument2)
        {
            return Math.Pow((double)argument1, (double)argument2);
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
        public override object Add(object argument1, object argument2)
        {
            if (argument1 is int) return (int)argument1 + (double)argument2;
            else return (double)argument1 + (int)argument2;
        }

        public override object Subtract(object argument1, object argument2)
        {
            if (argument1 is int) return (int)argument1 - (double)argument2;
            else return (double)argument1 - (int)argument2;
        }

        public override object Multiply(object argument1, object argument2)
        {
            if (argument1 is int) return (int)argument1 * (double)argument2;
            else return (double)argument1 * (int)argument2;
        }

        public override object Divide(object argument1, object argument2)
        {
            if (argument1 is int) return (int)argument1 / (double)argument2;
            else return (double)argument1 / (int)argument2;
        }

        public override object Power(object argument1, object argument2)
        {
            if (argument1 is int) return Math.Pow((int)argument1, (double)argument2);
            else return Math.Pow((double)argument1, (int)argument2);
        }

        public override object GreaterThan(object argument1, object argument2)
        {
            if (argument1 is int) return (int)argument1 > (double)argument2;
            else return (double)argument1 > (int)argument2;
        }

        public override object LessThan(object argument1, object argument2)
        {
            if (argument1 is int) return (int)argument1 < (double)argument2;
            else return (double)argument1 < (int)argument2;
        }

        public override object GreaterThanEqual(object argument1, object argument2)
        {
            if (argument1 is int) return (int)argument1 >= (double)argument2;
            else return (double)argument1 >= (int)argument2;
        }

        public override object LessThanEqual(object argument1, object argument2)
        {
            if (argument1 is int) return (int)argument1 <= (double)argument2;
            else return (double)argument1 <= (int)argument2;
        }

        public override object NotEqual(object argument1, object argument2)
        {
            if (argument1 is int) return (int)argument1 != (double)argument2;
            else return (double)argument1 != (int)argument2;
        }

        public override object Equal(object argument1, object argument2)
        {
            if (argument1 is int) return (int)argument1 == (double)argument2;
            else return (double)argument1 == (int)argument2;
        }

        public override object Min(object argument1, object argument2)
        {
            if (argument1 is int) return Math.Min((int)argument1, (double)argument2);
            else return Math.Min((double)argument1, (int)argument2);
        }

        public override object Max(object argument1, object argument2)
        {
            if (argument1 is int) return Math.Max((int)argument1, (double)argument2);
            else return Math.Max((double)argument1, (int)argument2);
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
            throw new ArgumentException("Can't subtract two strings");
        }

        public override object Multiply(object argument1, object argument2)
        {
            throw new ArgumentException("Can't multiply two strings");
        }

        public override object Divide(object argument1, object argument2)
        {
            throw new ArgumentException("Can't divide two strings");
        }

        public override object Power(object argument1, object argument2)
        {
            throw new ArgumentException("Can't power two strings");
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
            return Convert.ToBoolean(argument1) | Convert.ToBoolean(argument2);
        }

        public override object Subtract(object argument1, object argument2)
        {
            throw new ArgumentException("Can't subtract two booleans");
        }

        public override object Multiply(object argument1, object argument2)
        {
            return Convert.ToBoolean(argument1) & Convert.ToBoolean(argument2);
        }

        public override object Divide(object argument1, object argument2)
        {
            throw new ArgumentException("Can't divide two booleans");
        }

        public override object Power(object argument1, object argument2)
        {
            throw new ArgumentException("Can't power two booleans");
        }

        public override object GreaterThan(object argument1, object argument2)
        {
            // true > false
            return Convert.ToBoolean(argument1) & !Convert.ToBoolean(argument2);
        }

        public override object LessThan(object argument1, object argument2)
        {
            return !Convert.ToBoolean(argument1) & Convert.ToBoolean(argument2);
        }

        public override object GreaterThanEqual(object argument1, object argument2)
        {
            bool arg1 = Convert.ToBoolean(argument1);
            bool arg2 = Convert.ToBoolean(argument2);
            return (arg1 & !arg2) | (arg1 == arg2);
        }

        public override object LessThanEqual(object argument1, object argument2)
        {
            bool arg1 = Convert.ToBoolean(argument1);
            bool arg2 = Convert.ToBoolean(argument2);
            return (!arg1 & arg2) | (arg1 == arg2);
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
            bool arg1 = Convert.ToBoolean(argument1);
            bool arg2 = Convert.ToBoolean(argument2);
            return !arg1 ? arg1 : arg2;
        }

        public override object Max(object argument1, object argument2)
        {
            bool arg1 = Convert.ToBoolean(argument1);
            bool arg2 = Convert.ToBoolean(argument2);
            return arg1 ? arg1 : arg2;
        }
    }

    public class CalculatorSpecialValue : Calculator
    {
        private object Calculate(string op, object argument1, object argument2)
        {
            if (argument1 is SpecialValue) return ((SpecialValue)argument1).TryOperation(op, argument2, false);
            else return ((SpecialValue)argument2).TryOperation(op, argument1, true);
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
