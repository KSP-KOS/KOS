using kOS.Safe.Encapsulation;
using kOS.Safe.Utilities;
using System;
using System.Reflection;
using kOS.Safe.Exceptions;

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

            throw new KOSException(GetMessage("+", pair));
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

            throw new KOSException(GetMessage("-", pair));
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

            throw new KOSException(GetMessage("*", pair));
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

            throw new KOSException(GetMessage("/", pair));
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

            throw new KOSException(GetMessage("^", pair));
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

            throw new KOSException(GetMessage(">", pair));
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

            throw new KOSException(GetMessage("<", pair));
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

            throw new KOSException(GetMessage(">=", pair));
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

            throw new KOSException(GetMessage("<=", pair));
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

            return !pair.Left.Equals(pair.Right);
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

            return pair.Left.Equals(pair.Right);
        }

        private static string GetMessage(string op, OperandPair pair)
        {
            string t1 = pair.Left == null ? "<null>" : KOSNomenclature.GetKOSName(pair.Left.GetType());
            string t2 = pair.Right == null ? "<null>" : KOSNomenclature.GetKOSName(pair.Right.GetType());
            return string.Format("Cannot perform the operation: {0} On Structures {1} and {2}", op, t1, t2);
        }

        /// <summary>
        /// By default when you call MethodInfo.Invoke() it masks the exceptions
        /// the invoked method throws so the kOS user wouldn't see the real message.
        /// This fixes that for the operators we are trying to call here.
        /// </summary>
        private static object InvokeWithCorrectExceptions(MethodInfo meth, object obj, object [] parameters)
        {
            try
            {
                return meth.Invoke(obj, parameters);
            }
            catch (TargetInvocationException outerException)
            {
                // MethodInfo.Invoke() "helpfully" wraps the exceptions the method tries
                // to throw inside a TargetInvocationException so you get THAT instead of
                // the actual exception.  In order to let the user see the real exception
                // message, we have to unwrap this wrapper around it and re-throw it:
                throw outerException.InnerException;
            }
        }

        private bool TryInvokeExplicit(OperandPair pair, string methodName, out object result)
        {
            MethodInfo method1 = pair.LeftType.GetMethod(methodName, FLAGS, null, new[] { pair.LeftType, pair.RightType }, null);
            if (method1 != null)
            {
 
                result = InvokeWithCorrectExceptions(method1, null, new[] { pair.Left, pair.Right });
                return true;
            }
            MethodInfo method2 = pair.RightType.GetMethod(methodName, FLAGS, null, new[] { pair.LeftType, pair.RightType }, null);

            if (method2 != null)
            {
                result = InvokeWithCorrectExceptions(method2, null, new[] {pair.Left, pair.Right});
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
            if (pair.LeftType == pair.RightType)
            {
                resultPair = null;
                // Since the types are already the same, we can't coerce them to be the same.
                // Otherwise, some types will act as if they have been coerced because of 
                // other implict conversions.
                return false;
            }
            MethodInfo convert2 = pair.LeftType.GetMethod("op_Implicit", FLAGS | BindingFlags.ExactBinding, null, new[] { pair.RightType }, null);
            if (convert2 != null)
            {
                couldCoerce = true;
                newRight = InvokeWithCorrectExceptions(convert2, null, new[] { pair.Right });
            }
            else
            {
                newRight = pair.Right;
            }

            MethodInfo convert1 = pair.RightType.GetMethod("op_Implicit", FLAGS | BindingFlags.ExactBinding, null, new[] { pair.LeftType }, null);
            if (convert1 != null)
            {
                couldCoerce = true;
                newLeft = InvokeWithCorrectExceptions(convert1, null, new[] { pair.Left });
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