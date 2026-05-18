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
        public override Type GetAddResultType(Type leftType, Type rightType)
            => GetTypeForOperation(leftType, rightType, "Add", "op_Addition", "+");
        public override bool IsAdditionCommutative(Type leftType, Type rightType)
            => GetCommutativityForOperation(leftType, rightType, "Add", "op_Addition", "+");

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
        public override Type GetSubtractResultType(Type leftType, Type rightType)
            => GetTypeForOperation(leftType, rightType, "Subtract", "op_Subtraction", "-");
        public override bool IsSubtractionCommutativeWithNegation(Type leftType, Type rightType)
            => GetCommutativityForOperation(leftType, rightType, "Subtract", "op_Subtraction", "-");

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
        public override Type GetMultiplyResultType(Type leftType, Type rightType)
            => GetTypeForOperation(leftType, rightType, "Multiply", "op_Multiply", "*");
        public override bool IsMultiplicationCommmutative(Type leftType, Type rightType)
            => GetCommutativityForOperation(leftType, rightType, "Multiply", "op_Multiply", "*");

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
        public override Type GetDivideResultType(Type leftType, Type rightType)
            => GetTypeForOperation(leftType, rightType, "Divide", "op_Division", "/");
        public override bool IsDivisionCommutative(Type leftType, Type rightType)
            => GetCommutativityForOperation(leftType, rightType, "Divide", "op_Division", "/");

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
        public override Type GetPowerResultType(Type leftType, Type rightType)
            => GetTypeForOperation(leftType, rightType, "Power", "op_ExclusiveOr", "^");

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
        public override Type GetGreaterThanResultType(Type leftType, Type rightType)
            => GetTypeForOperation(leftType, rightType, "GreaterThan", "op_GreaterThan", ">");

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
        public override Type GetLessThanResultType(Type leftType, Type rightType)
            => GetTypeForOperation(leftType, rightType, "LessThan", "op_LessThan", "<");

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
        public override Type GetGreaterThanEqualResultType(Type leftType, Type rightType)
            => GetTypeForOperation(leftType, rightType, "GreaterThanEqual", "op_GreaterThanEqual", ">=");

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
        public override Type GetLessThanEqualResultType(Type leftType, Type rightType)
            => GetTypeForOperation(leftType, rightType, "LessThanEqual", "op_LessThanEqual", "<=");

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
        public override Type GetNotEqualResultType(Type leftType, Type rightType)
        {
            if (CheckTypesForNull(leftType, rightType))
                return null;
            if (TryTypingExplicit(leftType, rightType, "op_Inequality", out Type result))
                return result;

            if (TryTypingImplicit(leftType, rightType, out Type newLeftType, out Type newRightType))
                return GetNotEqualResultType(newLeftType, newRightType);

            return typeof(BooleanValue);
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
        public override Type GetEqualResultType(Type leftType, Type rightType)
        {
            if (CheckTypesForNull(leftType, rightType))
                return null;
            if (TryTypingExplicit(leftType, rightType, "op_Equality", out Type result))
                return result;

            if (TryTypingImplicit(leftType, rightType, out Type newLeftType, out Type newRightType))
                return GetNotEqualResultType(newLeftType, newRightType);

            return typeof(BooleanValue);
        }

        private Type GetTypeForOperation(Type leftType, Type rightType, string opName, string methodName, string opAbbreviation)
        {
            if (CheckTypesForNull(leftType, rightType))
                return null;
            if (TryTypingExplicit(leftType, rightType, methodName, out Type result))
                return result;

            if (TryTypingImplicit(leftType, rightType, out Type newLeftType, out Type newRightType))
                return GetTypeForOperation(newLeftType, newRightType, opName, methodName, opAbbreviation);

            return typeof(Structure);
        }

        private bool GetCommutativityForOperation(Type leftType, Type rightType, string opName, string methodName, string opAbbreviation)
        {
            if (CheckTypesForNull(leftType, rightType))
                return false;
            if (CheckCommutativityExplicit(leftType, rightType, methodName, out bool result))
                return result;

            if (TryTypingImplicit(leftType, rightType, out Type newLeftType, out Type newRightType))
                return GetCommutativityForOperation(newLeftType, newRightType, opName, methodName, opAbbreviation);

            return false;
        }

        private static string GetMessage(string op, OperandPair pair)
        {
            string t1 = pair.Left == null ? "<null>" : KOSNomenclature.GetKOSName(pair.Left.GetType());
            string t2 = pair.Right == null ? "<null>" : KOSNomenclature.GetKOSName(pair.Right.GetType());
            return string.Format("Cannot perform the operation: {0} On Structures {1} and {2}", op, t1, t2);
        }
        private static string GetMessage(string op, Type left, Type right)
        {
            string t1 = left == null ? "<null>" : KOSNomenclature.GetKOSName(left);
            string t2 = right == null ? "<null>" : KOSNomenclature.GetKOSName(right);
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

        private bool TryTypingExplicit(Type left, Type right, string methodName, out Type result)
        {
            MethodInfo method1 = left.GetMethod(methodName, FLAGS, null, new[] { left, right }, null);
            if (method1 != null)
            {
                result = method1.ReturnType;
                return true;
            }
            MethodInfo method2 = right.GetMethod(methodName, FLAGS, null, new[] { left, right }, null);
            if (method2 != null)
            {
                result = method2.ReturnType;
                return true;
            }
            result = null;
            return false;
        }

        private bool CheckCommutativityExplicit(Type left, Type right, string methodName, out bool result)
        {
            MethodInfo method1 = left.GetMethod(methodName, FLAGS, null, new[] { left, right }, null);
            CommutativeAttribute commutativeAttribute = method1?.GetCustomAttribute<CommutativeAttribute>();
            if (method1 != null)
            {
                if (commutativeAttribute != null)
                    result = commutativeAttribute.IsCommutative;
                else
                    result = DefaultCommutativity(methodName, right);
                return true;
            }
            MethodInfo method2 = right.GetMethod(methodName, FLAGS, null, new[] { left, right }, null);
            commutativeAttribute = method2?.GetCustomAttribute<CommutativeAttribute>();
            if (method2 != null)
            {
                if (commutativeAttribute != null)
                    result = commutativeAttribute.IsCommutative;
                else
                    result = DefaultCommutativity(methodName, right);
                return true;
            }
            result = false;
            return false;
        }
        private bool DefaultCommutativity(string methodName, Type _)
        {
            switch (methodName)
            {
                case "op_Addition":
                case "op_Multiply":
                    return true;
                case "op_GreaterThan":
                case "op_LessThan":
                case "op_GreaterThanEqual":
                case "op_LessThanEqual":
                    return true;
                case "op_Equality":
                case "op_Inequality":
                    return true;
                case "op_Subtraction":
                    return true;
                    //return right.GetMethod("op_UnaryNegation", FLAGS, null, new[] { right }, null) != null;
                case "op_Division":
                case "op_ExclusiveOr":
                default:
                    return false;
            }
        }

        private void CheckPairForNull(OperandPair pair, string opName)
        {
            if (pair.Left == null || pair.Right == null)
            {
                throw new InvalidOperationException(GetMessage(opName, pair));
            }
        }
        private bool CheckTypesForNull(Type left, Type right)
        {
            return left == null || right == null;
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

        private bool TryTypingImplicit(Type left, Type right, out Type newLeft, out Type newRight)
        {
            bool couldCoerce = false;
            if (left == right)
            {
                newLeft = null;
                newRight = null;
                return false;
            }
            MethodInfo convert2 = left.GetMethod("op_Implicit", FLAGS | BindingFlags.ExactBinding, null, new[] { right }, null);
            if (convert2 != null)
            {
                couldCoerce = true;
                newRight = convert2.ReturnType;
            }
            else
            {
                newRight = right;
            }

            MethodInfo convert1 = right.GetMethod("op_Implicit", FLAGS | BindingFlags.ExactBinding, null, new[] { left }, null);
            if (convert1 != null)
            {
                couldCoerce = true;
                newLeft = convert1.ReturnType;
            }
            else
            {
                newLeft = left;
            }

            return couldCoerce;
        }

    }
}