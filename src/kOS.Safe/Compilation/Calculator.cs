using System;
using System.Linq;
using System.Collections;
using System.Reflection;
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
            int scalarCount = 0;
            int stringCount = 0;
            int specialCount = 0;
            int boolCount = 0;

            argument1 = Structure.FromPrimitive(argument1);
            argument2 = Structure.FromPrimitive(argument2);

            if (argument1 is ScalarValue) scalarCount++;
            if (argument1 is StringValue) stringCount++;
            if (argument1 is ISuffixed) specialCount++;
            if (argument1 is BooleanValue) boolCount++;
            if (argument2 is ScalarValue) scalarCount++;
            if (argument2 is StringValue) stringCount++;
            if (argument2 is ISuffixed) specialCount++;
            if (argument2 is BooleanValue) boolCount++;

            if (scalarCount == 2) return new CalculatorScalar();
            if (stringCount > 0) return new CalculatorString();
            if (boolCount > 0) return new CalculatorBool();
            if (specialCount > 0) return new CalculatorStructure();

            throw new NotImplementedException(string.Format("Can't operate types {0} and {1}", argument1.GetType(), argument2.GetType()));
        }
    }

    public class CalculatorScalar : Calculator
    {
        public override object Add(object argument1, object argument2)
        {
            return ScalarValue.Create(argument1) + ScalarValue.Create(argument2);
        }

        public override object Subtract(object argument1, object argument2)
        {
            return ScalarValue.Create(argument1) - ScalarValue.Create(argument2);
        }

        public override object Multiply(object argument1, object argument2)
        {
            return ScalarValue.Create(argument1) * ScalarValue.Create(argument2);
        }

        public override object Divide(object argument1, object argument2)
        {
            return ScalarValue.Create(argument1) / ScalarValue.Create(argument2);
        }

        public override object Power(object argument1, object argument2)
        {
            return ScalarValue.Create(argument1) ^ ScalarValue.Create(argument2);
        }

        public override object GreaterThan(object argument1, object argument2)
        {
            return ScalarValue.Create(argument1) > ScalarValue.Create(argument2);
        }

        public override object LessThan(object argument1, object argument2)
        {
            return ScalarValue.Create(argument1) < ScalarValue.Create(argument2);
        }

        public override object GreaterThanEqual(object argument1, object argument2)
        {
            return ScalarValue.Create(argument1) >= ScalarValue.Create(argument2);
        }

        public override object LessThanEqual(object argument1, object argument2)
        {
            return ScalarValue.Create(argument1) <= ScalarValue.Create(argument2);
        }

        public override object NotEqual(object argument1, object argument2)
        {
            return ScalarValue.Create(argument1) != ScalarValue.Create(argument2);
        }

        public override object Equal(object argument1, object argument2)
        {
            return ScalarValue.Create(argument1) == ScalarValue.Create(argument2);
        }

        public override object Min(object argument1, object argument2)
        {
            return ScalarValue.Min(ScalarValue.Create(argument1), ScalarValue.Create(argument2));
        }

        public override object Max(object argument1, object argument2)
        {
            return ScalarValue.Max(ScalarValue.Create(argument1), ScalarValue.Create(argument2));
        }
    }

    public class CalculatorString : Calculator
    {
        public static void ThrowIfNotStrings(object argument1, object argument2)
        {
            if (!(argument1 is string))
            {
                throw new KOSCastException(argument1.GetType(), typeof(string));
            }
            if (!(argument2 is string))
            {
                throw new KOSCastException(argument1.GetType(), typeof(string));
            }
        }
        public override object Add(object argument1, object argument2)
        {
            return new StringValue(argument1.ToString() + argument2.ToString());
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
            ThrowIfNotStrings(argument1, argument2);
            return argument1.ToString().Length > argument2.ToString().Length;
        }

        public override object LessThan(object argument1, object argument2)
        {
            ThrowIfNotStrings(argument1, argument2);
            return argument1.ToString().Length < argument2.ToString().Length;
        }

        public override object GreaterThanEqual(object argument1, object argument2)
        {
            ThrowIfNotStrings(argument1, argument2);
            return argument1.ToString().Length >= argument2.ToString().Length;
        }

        public override object LessThanEqual(object argument1, object argument2)
        {
            ThrowIfNotStrings(argument1, argument2);
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
            ThrowIfNotStrings(argument1, argument2);
            string arg1 = argument1.ToString();
            string arg2 = argument2.ToString();
            return (arg1.Length < arg2.Length) ? arg1 : arg2;
        }

        public override object Max(object argument1, object argument2)
        {
            ThrowIfNotStrings(argument1, argument2);
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
            return new BooleanValue(Convert.ToBoolean(argument1) != Convert.ToBoolean(argument2));
        }

        public override object Equal(object argument1, object argument2)
        {
            return new BooleanValue(Convert.ToBoolean(argument1) == Convert.ToBoolean(argument2));
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
        BindingFlags flags = BindingFlags.FlattenHierarchy | BindingFlags.Static | BindingFlags.Public;
        private object Calculate(string op, object argument1, object argument2)
        {
            if (argument1 is IOperable) return ((IOperable)argument1).TryOperation(op, argument2, false);
            return ((IOperable)argument2).TryOperation(op, argument1, true);
        }

        public override object Add(object argument1, object argument2)
        {
            Type type1 = argument1.GetType();
            Type type2 = argument2.GetType();
            MethodInfo method1 = type1.GetMethod("op_Addition", flags, null, new Type[] { type1, type2 }, null);
            if (method1 != null)
            {
                return method1.Invoke(null, new object[] { argument1, argument2 });
            }
            MethodInfo method2 = type2.GetMethod("op_Addition", flags, null, new Type[] { type1, type2 }, null);
            if (method2 != null)
            {
                return method2.Invoke(null, new object[] { argument1, argument2 });
            }
            return Calculate("+", argument1, argument2);
        }

        public override object Subtract(object argument1, object argument2)
        {
            Type type1 = argument1.GetType();
            Type type2 = argument2.GetType();
            MethodInfo method1 = type1.GetMethod("op_Subtraction", flags, null, new Type[] { type1, type2 }, null);
            if (method1 != null)
            {
                return method1.Invoke(null, new object[] { argument1, argument2 });
            }
            MethodInfo method2 = type2.GetMethod("op_Subtraction", flags, null, new Type[] { type1, type2 }, null);
            if (method2 != null)
            {
                return method2.Invoke(null, new object[] { argument1, argument2 });
            }
            return Calculate("-", argument1, argument2);
        }

        public override object Multiply(object argument1, object argument2)
        {
            Type type1 = argument1.GetType();
            Type type2 = argument2.GetType();
            MethodInfo method1 = type1.GetMethod("op_Multiply", flags, null, new Type[] { type1, type2 }, null);
            if (method1 != null)
            {
                return method1.Invoke(null, new object[] { argument1, argument2 });
            }
            MethodInfo method2 = type2.GetMethod("op_Multiply", flags, null, new Type[] { type1, type2 }, null);
            if (method2 != null)
            {
                return method2.Invoke(null, new object[] { argument1, argument2 });
            }
            // TODO: Remove this debugging code, adding to the commit in case it is needed for continued testing.
            //string message = "\n  Multiply-Method1: " + method1 ?? "<null>";
            //message += "\n";
            //message += "  Multiply-Method2: " + method2 ?? "<null>";
            //message += "\n";
            //message += "  Multiply-Type1: " + type1 ?? "<null>";
            //message += "\n";
            //message += "  Multiply-Type2: " + type2 ?? "<null>";
            //message += "\n";
            //message += "  Multiply-Methods1: \n";
            //foreach (var method in type1.GetMethods(flags))
            //{
            //    string paramarray = string.Join(", ", method.GetParameters().Select(e => e.ParameterType.ToString()).ToArray());
            //    message += string.Format("    {0} ({1})\n", method.Name, paramarray);
            //}
            //message += "  Multiply-Methods2: \n";
            //foreach (var method in type2.GetMethods(flags))
            //{
            //    string paramarray = string.Join(", ", method.GetParameters().Select(e => e.ParameterType.ToString()).ToArray());
            //    message += string.Format("    {0} ({1})\n", method.Name, paramarray);
            //}
            //message.TrimEnd('\n');
            //kOS.Safe.Utilities.SafeHouse.Logger.LogWarning(message);
            return Calculate("*", argument1, argument2);
        }

        public override object Divide(object argument1, object argument2)
        {
            Type type1 = argument1.GetType();
            Type type2 = argument2.GetType();
            MethodInfo method1 = type1.GetMethod("op_Division", flags, null, new Type[] { type1, type2 }, null);
            if (method1 != null)
            {
                return method1.Invoke(null, new object[] { argument1, argument2 });
            }
            MethodInfo method2 = type2.GetMethod("op_Division", flags, null, new Type[] { type1, type2 }, null);
            if (method2 != null)
            {
                return method2.Invoke(null, new object[] { argument1, argument2 });
            }
            return Calculate("/", argument1, argument2);
        }

        public override object Power(object argument1, object argument2)
        {
            Type type1 = argument1.GetType();
            Type type2 = argument2.GetType();
            MethodInfo method1 = type1.GetMethod("op_ExclusiveOr", flags, null, new Type[] { type1, type2 }, null);
            if (method1 != null)
            {
                return method1.Invoke(null, new object[] { argument1, argument2 });
            }
            MethodInfo method2 = type2.GetMethod("op_ExclusiveOr", flags, null, new Type[] { type1, type2 }, null);
            if (method2 != null)
            {
                return method2.Invoke(null, new object[] { argument1, argument2 });
            }
            return null;
        }

        public override object GreaterThan(object argument1, object argument2)
        {
            Type type1 = argument1.GetType();
            Type type2 = argument2.GetType();
            MethodInfo method1 = type1.GetMethod("op_GreaterThan", flags, null, new Type[] { type1, type2 }, null);
            if (method1 != null)
            {
                return method1.Invoke(null, new object[] { argument1, argument2 });
            }
            MethodInfo method2 = type2.GetMethod("op_GreaterThan", flags, null, new Type[] { type1, type2 }, null);
            if (method2 != null)
            {
                return method2.Invoke(null, new object[] { argument1, argument2 });
            }
            return Calculate(">", argument1, argument2);
        }

        public override object LessThan(object argument1, object argument2)
        {
            Type type1 = argument1.GetType();
            Type type2 = argument2.GetType();
            MethodInfo method1 = type1.GetMethod("op_LessThan", flags, null, new Type[] { type1, type2 }, null);
            if (method1 != null)
            {
                return method1.Invoke(null, new object[] { argument1, argument2 });
            }
            MethodInfo method2 = type2.GetMethod("op_LessThan", flags, null, new Type[] { type1, type2 }, null);
            if (method2 != null)
            {
                return method2.Invoke(null, new object[] { argument1, argument2 });
            }
            return Calculate("<", argument1, argument2);
        }

        public override object GreaterThanEqual(object argument1, object argument2)
        {
            Type type1 = argument1.GetType();
            Type type2 = argument2.GetType();
            MethodInfo method1 = type1.GetMethod("op_GreaterThanOrEqual", flags, null, new Type[] { type1, type2 }, null);
            if (method1 != null)
            {
                return method1.Invoke(null, new object[] { argument1, argument2 });
            }
            MethodInfo method2 = type2.GetMethod("op_GreaterThanOrEqual", flags, null, new Type[] { type1, type2 }, null);
            if (method2 != null)
            {
                return method2.Invoke(null, new object[] { argument1, argument2 });
            }
            return Calculate(">=", argument1, argument2);
        }

        public override object LessThanEqual(object argument1, object argument2)
        {
            Type type1 = argument1.GetType();
            Type type2 = argument2.GetType();
            MethodInfo method1 = type1.GetMethod("op_LessThanOrEqual", flags, null, new Type[] { type1, type2 }, null);
            if (method1 != null)
            {
                return method1.Invoke(null, new object[] { argument1, argument2 });
            }
            MethodInfo method2 = type2.GetMethod("op_LessThanOrEqual", flags, null, new Type[] { type1, type2 }, null);
            if (method2 != null)
            {
                return method2.Invoke(null, new object[] { argument1, argument2 });
            }
            return Calculate("<=", argument1, argument2);
        }

        public override object NotEqual(object argument1, object argument2)
        {
            Type type1 = argument1.GetType();
            Type type2 = argument2.GetType();
            MethodInfo method1 = type1.GetMethod("op_Inequality", flags, null, new Type[] { type1, type2 }, null);
            if (method1 != null)
            {
                return method1.Invoke(null, new object[] { argument1, argument2 });
            }
            MethodInfo method2 = type2.GetMethod("op_Inequality", flags, null, new Type[] { type1, type2 }, null);
            if (method2 != null)
            {
                return method2.Invoke(null, new object[] { argument1, argument2 });
            }
            return Calculate("<>", argument1, argument2);
        }

        public override object Equal(object argument1, object argument2)
        {
            Type type1 = argument1.GetType();
            Type type2 = argument2.GetType();
            MethodInfo method1 = type1.GetMethod("op_Equality", flags, null, new Type[] { type1, type2 }, null);
            if (method1 != null)
            {
                return method1.Invoke(null, new object[] { argument1, argument2 });
            }
            MethodInfo method2 = type2.GetMethod("op_Equality", flags, null, new Type[] { type1, type2 }, null);
            if (method2 != null)
            {
                return method2.Invoke(null, new object[] { argument1, argument2 });
            }
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
