using kOS.Safe.Encapsulation;
using System;
using System.Reflection;

namespace kOS.Safe.Compilation
{
    public class CalculatorStructure : Calculator
    {
        private const BindingFlags FLAGS = BindingFlags.FlattenHierarchy | BindingFlags.Static | BindingFlags.Public;

        public override object Add(OperandPair pair)
        {
            CheckPairForNull(pair, "Add");

            object result;
            if (TryInvokeExplicit(pair, "op_Addition", out result))
            {
                return result;
            }

            OperandPair resultPair;
            if (TryCoerceImplicit(pair, out resultPair))
            {
                return Add(resultPair);
            }

            return Calculate("+", pair);
        }

        public override object Subtract(OperandPair pair)
        {
            CheckPairForNull(pair, "Subtract");

            object result;
            if (TryInvokeExplicit(pair, "op_Subtraction", out result))
            {
                return result;
            }

            OperandPair resultPair;
            if (TryCoerceImplicit(pair, out resultPair))
            {
                return Subtract(resultPair);
            }

            return Calculate("-", pair);
        }

        public override object Multiply(OperandPair pair)
        {
            CheckPairForNull(pair, "Multiply");

            object result;
            if (TryInvokeExplicit(pair, "op_Multiply", out result))
            {
                return result;
            }

            OperandPair resultPair;
            if (TryCoerceImplicit(pair, out resultPair))
            {
                return Multiply(resultPair);
            }

            return Calculate("*", pair);
        }

        public override object Divide(OperandPair pair)
        {
            CheckPairForNull(pair, "Divide");

            object result;
            if (TryInvokeExplicit(pair, "op_Division", out result))
            {
                return result;
            }

            OperandPair resultPair;
            if (TryCoerceImplicit(pair, out resultPair))
            {
                return Divide(resultPair);
            }

            return Calculate("/", pair);
        }

        public override object Power(OperandPair pair)
        {
            CheckPairForNull(pair, "Power");

            object result;
            if (TryInvokeExplicit(pair, "op_ExclusiveOr", out result))
            {
                return result;
            }

            OperandPair resultPair;
            if (TryCoerceImplicit(pair, out resultPair))
            {
                return Power(resultPair);
            }

            return Calculate("^", pair);
        }

        public override object GreaterThan(OperandPair pair)
        {
            CheckPairForNull(pair, "GreaterThan");

            object result;
            if (TryInvokeExplicit(pair, "op_GreaterThan", out result))
            {
                return result;
            }

            OperandPair resultPair;
            if (TryCoerceImplicit(pair, out resultPair))
            {
                return GreaterThan(resultPair);
            }

            return Calculate(">", pair);
        }

        public override object LessThan(OperandPair pair)
        {
            CheckPairForNull(pair, "LessThan");

            object result;
            if (TryInvokeExplicit(pair, "op_LessThan", out result))
            {
                return result;
            }

            OperandPair resultPair;
            if (TryCoerceImplicit(pair, out resultPair))
            {
                return LessThan(resultPair);
            }

            return Calculate("<", pair);
        }

        public override object GreaterThanEqual(OperandPair pair)
        {
            CheckPairForNull(pair, "GreaterThanEqual");

            object result;
            if (TryInvokeExplicit(pair, "op_GreaterThanEqual", out result))
            {
                return result;
            }

            OperandPair resultPair;
            if (TryCoerceImplicit(pair, out resultPair))
            {
                return GreaterThanEqual(resultPair);
            }

            return Calculate(">=", pair);
        }

        public override object LessThanEqual(OperandPair pair)
        {
            CheckPairForNull(pair, "LessThanEqual");

            object result;
            if (TryInvokeExplicit(pair, "op_LessThanEqual", out result))
            {
                return result;
            }

            OperandPair resultPair;
            if (TryCoerceImplicit(pair, out resultPair))
            {
                return LessThanEqual(resultPair);
            }

            return Calculate("<=", pair);
        }

        public override object NotEqual(OperandPair pair)
        {
            CheckPairForNull(pair, "NotEqual");

            object result;
            if (TryInvokeExplicit(pair, "op_Inequality", out result))
            {
                return result;
            }

            OperandPair resultPair;
            if (TryCoerceImplicit(pair, out resultPair))
            {
                return NotEqual(resultPair);
            }

            return Calculate("<>", pair);
        }

        public override object Equal(OperandPair pair)
        {
            CheckPairForNull(pair, "Equal");

            object result;
            if (TryInvokeExplicit(pair, "op_Equality", out result))
            {
                return result;
            }

            OperandPair resultPair;
            if (TryCoerceImplicit(pair, out resultPair))
            {
                return Equal(pair);
            }

            return Calculate("==", pair);
        }

        public override object Min(OperandPair pair)
        {
            CheckPairForNull(pair, "min");
            return Calculate("min", pair);
        }

        public override object Max(OperandPair pair)
        {
            CheckPairForNull(pair, "max");
            return Calculate("max", pair);
        }

        private object Calculate(string op, OperandPair pair)
        {
            var operable = pair.Left as IOperable;
            if (operable == null)
            {
                return ((IOperable)pair.Right).TryOperation(op, pair.Left, true);
            }

            return operable.TryOperation(op, pair.Right, false);
        }

        private static string GetMessage(string op, OperandPair pair)
        {
            string t1 = pair.Left == null ? "<null>" : pair.Left.GetType().ToString();
            string t2 = pair.Right == null ? "<null>" : pair.Right.GetType().ToString();
            return string.Format("Cannot perform the operation: {0} On Structures {1} and {2}", op, t1, t2);
        }

        private bool TryInvokeExplicit(OperandPair pair, string methodName, out object result)
        {
            MethodInfo method1 = pair.LeftType.GetMethod(methodName, FLAGS, null, new[] { pair.LeftType, pair.RightType }, null);
            if (method1 != null)
            {
                result = method1.Invoke(null, new[] {pair.Left, pair.Right});
                return true;
            }
            MethodInfo method2 = pair.RightType.GetMethod(methodName, FLAGS, null, new[] { pair.LeftType, pair.RightType }, null);

            if (method2 != null)
            {
                result = method2.Invoke(null, new[] {pair.Left, pair.Right});
                return true;
            }

            result = null;
            return false;
        }

        private void CheckPairForNull(OperandPair pair, string opName)
        {
            if (pair.Left == null || pair.Right == null)
            {
                throw new InvalidOperationException(GetMessage(opName, pair));
            }
        }

        private bool TryCoerceImplicit(OperandPair pair, out OperandPair resultPair)
        {
            bool couldCoerce = false;
            object newLeft;
            object newRight;
            MethodInfo convert2 = pair.LeftType.GetMethod("op_Implicit", FLAGS | BindingFlags.ExactBinding, null, new[] { pair.RightType }, null);
            if (convert2 != null)
            {
                couldCoerce = true;
                newRight = convert2.Invoke(null, new[] { pair.Right });
            }
            else
            {
                newRight = pair.Right;
            }

            MethodInfo convert1 = pair.RightType.GetMethod("op_Implicit", FLAGS | BindingFlags.ExactBinding, null, new[] { pair.LeftType }, null);
            if (convert1 != null)
            {
                couldCoerce = true;
                newLeft = convert1.Invoke(null, new[] { pair.Left });
            }
            else
            {
                newLeft = pair.Left;
            }

            resultPair = new OperandPair(newLeft, newRight);

            return couldCoerce;
        }

    }
}