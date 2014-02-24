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
        public abstract object Equal(object argument1, object argument2);
    }

    public class CalculatorIntInt : Calculator
    {
        public override object Add(object argument1, object argument2)
        {
            return (int)argument1 + (int)argument2;
        }

        public override object Subtract(object argument1, object argument2)
        {
            return (int)argument1 - (int)argument2;
        }

        public override object Multiply(object argument1, object argument2)
        {
            return (int)argument1 * (int)argument2;
        }

        public override object Divide(object argument1, object argument2)
        {
            //TODO: catch exception
            return (double)(int)argument1 / (int)argument2;
        }

        public override object Power(object argument1, object argument2)
        {
            return (int)Math.Pow((int)argument1, (int)argument2);
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

        public override object Equal(object argument1, object argument2)
        {
            return (int)argument1 == (int)argument2;
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
            //TODO: catch exception
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

        public override object Equal(object argument1, object argument2)
        {
            return (double)argument1 == (double)argument2;
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
            //TODO: catch exception
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

        public override object Equal(object argument1, object argument2)
        {
            if (argument1 is int) return (int)argument1 == (double)argument2;
            else return (double)argument1 == (int)argument2;
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

        public override object Equal(object argument1, object argument2)
        {
            return argument1.ToString().ToLower() == argument2.ToString().ToLower();
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

        public override object Equal(object argument1, object argument2)
        {
            return Convert.ToBoolean(argument1) == Convert.ToBoolean(argument2);
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

        public override object Equal(object argument1, object argument2)
        {
            return Calculate("==", argument1, argument2);
        }
    }
}
