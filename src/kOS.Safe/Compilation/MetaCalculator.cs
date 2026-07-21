using System;
using System.Collections.Generic;
using System.Reflection;
using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;

namespace kOS.Safe.Compilation
{
    // Ensure that this class mirrors the results and structure from Calculator!

    // Negatable (subtraction equals addition with negation) ->
    // True if a class implements subtraction, addition, and negation,
    // where substitution unless otherwise flagged.

    // Commutative (operands can be swapped) ->
    // Standard unless otherwise flagged.
    // Standard is true for addition, multiplication, and comparisons;
    // IsNegatable for subtraction; and false for division and exponentiation.

    // Reversible (addition is reversible with subtraction(and vice versa),
    // and multiplication is reversible with division(and vice versa)) ->
    // Standard unless otherwise flagged on the method in question.
    // Standard is true if a class implements both operations, but false for comparisons.

    public static class MetaCalculator
    {
        private const BindingFlags FLAGS = BindingFlags.FlattenHierarchy | BindingFlags.Static | BindingFlags.Public;

        public static Type GetResultType(Type leftType, Type rightType, BinaryOpcode operation)
        {
            if (typeof(ScalarValue).IsAssignableFrom(leftType) && typeof(ScalarValue).IsAssignableFrom(rightType))
            {
                if (IsComparison(operation))
                    return typeof(BooleanValue);
                return typeof(ScalarValue);
            }
            else if (typeof(StringValue).IsAssignableFrom(leftType) || typeof(StringValue).IsAssignableFrom(rightType))
            {
                if (IsComparison(operation))
                    return typeof(BooleanValue);
                switch (operation)
                {
                    case OpcodeMathAdd _:
                        return typeof(StringValue);
                    case OpcodeMathSubtract _:
                        throw new KOSBinaryOperandTypeException(leftType, rightType, "subtract", "from");
                    case OpcodeMathMultiply _:
                        throw new KOSBinaryOperandTypeException(leftType, rightType, "multiply", "by");
                    case OpcodeMathDivide _:
                        throw new KOSBinaryOperandTypeException(leftType, rightType, "divide", "by");
                    case OpcodeMathPower _:
                        throw new KOSBinaryOperandTypeException(leftType, rightType, "exponentiate", "by");
                }
            }
            else if (typeof(BooleanValue).IsAssignableFrom(leftType) || typeof(BooleanValue).IsAssignableFrom(rightType))
            {
                if (IsComparison(operation))
                    return typeof(BooleanValue);
                switch (operation)
                {
                    case OpcodeMathAdd _:
                        throw new KOSBinaryOperandTypeException(leftType, rightType, "add", "to");
                    case OpcodeMathSubtract _:
                        throw new KOSBinaryOperandTypeException(leftType, rightType, "subtract", "from");
                    case OpcodeMathMultiply _:
                        throw new KOSBinaryOperandTypeException(leftType, rightType, "multiply", "by");
                    case OpcodeMathDivide _:
                        throw new KOSBinaryOperandTypeException(leftType, rightType, "divide", "by");
                    case OpcodeMathPower _:
                        throw new KOSBinaryOperandTypeException(leftType, rightType, "exponentiate", "by");
                }
            }
            else if (typeof(ISuffixed).IsAssignableFrom(leftType) || typeof(ISuffixed).IsAssignableFrom(rightType))
            {
                switch (operation)
                {
                    case OpcodeMathAdd _:
                        return GetTypeForOperation(leftType, rightType, "op_Addition", "+");
                    case OpcodeMathSubtract _:
                        return GetTypeForOperation(leftType, rightType, "op_Subtraction", "-");
                    case OpcodeMathMultiply _:
                        return GetTypeForOperation(leftType, rightType, "op_Multiply", "*");
                    case OpcodeMathDivide _:
                        return GetTypeForOperation(leftType, rightType, "op_Division", "/");
                    case OpcodeMathPower _:
                        return GetTypeForOperation(leftType, rightType, "op_ExclusiveOr", "^");
                    case OpcodeCompareGT _:
                        return GetTypeForOperation(leftType, rightType, "op_GreaterThan", ">");
                    case OpcodeCompareLT _:
                        return GetTypeForOperation(leftType, rightType, "op_LessThan", "<");
                    case OpcodeCompareGTE _:
                        return GetTypeForOperation(leftType, rightType, "op_GreaterThanEqual", ">=");
                    case OpcodeCompareLTE _:
                        return GetTypeForOperation(leftType, rightType, "op_LessThanEqual", "<=");
                    case OpcodeCompareNE _:
                        return GetTypeForOperation(leftType, rightType, "op_Inequality", "!=");
                    case OpcodeCompareEqual _:
                        return GetTypeForOperation(leftType, rightType, "op_Equality", "==");
                }
            }
            throw new NotImplementedException(string.Format("Can't operate types {0} and {1}", leftType, rightType));
        }

        private static Dictionary<(Type, Type, string), Type> typeCache = new Dictionary<(Type, Type, string), Type>();
        private static Type GetTypeForOperation(Type leftType, Type rightType, string methodName, string opAbbreviation)
        {
            if (typeCache.TryGetValue((leftType, rightType, methodName), out Type result))
                return result;
            if (CheckTypesForNull(leftType, rightType))
                return null;
            if (TryTypingExplicit(leftType, rightType, methodName, out result))
            {
                typeCache[(leftType, rightType, methodName)] = result;
                return result;
            }

            if (TryTypingImplicit(leftType, rightType, out Type newLeftType, out Type newRightType))
            {
                result = GetTypeForOperation(newLeftType, newRightType, methodName, opAbbreviation);
                typeCache[(leftType, rightType, methodName)] = result;
                return result;
            }

            typeCache[(leftType, rightType, methodName)] = typeof(Structure);
            return typeof(Structure);
        }
        private static bool CheckTypesForNull(Type left, Type right)
        {
            return left == null || right == null;
        }
        private static bool TryTypingExplicit(Type left, Type right, string methodName, out Type result)
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
        private static bool TryTypingImplicit(Type left, Type right, out Type newLeft, out Type newRight)
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

        public static bool IsCommutative(Type leftType, Type rightType, BinaryOpcode operation)
        {
            if (typeof(ScalarValue).IsAssignableFrom(leftType) && typeof(ScalarValue).IsAssignableFrom(rightType))
            {
                switch (operation)
                {
                    case OpcodeMathAdd _:
                    case OpcodeMathMultiply _:
                        return true;
                    case OpcodeMathSubtract _:
                        return IsNegatable(leftType, rightType);
                    case OpcodeMathDivide _:
                    case OpcodeMathPower _:
                        return false;
                }
                return IsComparison(operation);
            }
            else if (typeof(StringValue).IsAssignableFrom(leftType) || typeof(StringValue).IsAssignableFrom(rightType))
            {
                return IsComparison(operation);
            }
            else if (typeof(BooleanValue).IsAssignableFrom(leftType) || typeof(BooleanValue).IsAssignableFrom(rightType))
            {
                return IsComparison(operation);
            }
            else if (typeof(ISuffixed).IsAssignableFrom(leftType) || typeof(ISuffixed).IsAssignableFrom(rightType))
            {
                switch (operation)
                {
                    case OpcodeMathAdd _:
                        return GetCommutativityForOperation(leftType, rightType, "op_Addition", "+");
                    case OpcodeMathSubtract _:
                        return GetCommutativityForOperation(leftType, rightType, "op_Subtraction", "-");
                    case OpcodeMathMultiply _:
                        return GetCommutativityForOperation(leftType, rightType, "op_Multiply", "*");
                    case OpcodeMathDivide _:
                        return GetCommutativityForOperation(leftType, rightType, "op_Division", "/");
                    case OpcodeMathPower _:
                        return GetCommutativityForOperation(leftType, rightType, "op_ExclusiveOr", "^");
                    case OpcodeCompareGT _:
                        return GetCommutativityForOperation(leftType, rightType, "op_GreaterThan", ">");
                    case OpcodeCompareLT _:
                        return GetCommutativityForOperation(leftType, rightType, "op_LessThan", "<");
                    case OpcodeCompareGTE _:
                        return GetCommutativityForOperation(leftType, rightType, "op_GreaterThanEqual", ">=");
                    case OpcodeCompareLTE _:
                        return GetCommutativityForOperation(leftType, rightType, "op_LessThanEqual", "<=");
                    case OpcodeCompareNE _:
                        return GetCommutativityForOperation(leftType, rightType, "op_Inequality", "!=");
                    case OpcodeCompareEqual _:
                        return GetCommutativityForOperation(leftType, rightType, "op_Equality", "==");
                }
            }
            throw new NotImplementedException(string.Format("Can't operate types {0} and {1}", leftType, rightType));
        }

        private static Dictionary<(Type, Type, string), bool> commutativityCache = new Dictionary<(Type, Type, string), bool>();
        private static bool GetCommutativityForOperation(Type leftType, Type rightType, string methodName, string opAbbreviation)
        {
            if (commutativityCache.TryGetValue((leftType, rightType, methodName), out bool result))
                return result;
            if (CheckTypesForNull(leftType, rightType))
                return false;
            if (CheckCommutativityExplicit(leftType, rightType, methodName, out result))
            {
                commutativityCache[(leftType, rightType, methodName)] = result;
                return result;
            }

            if (TryTypingImplicit(leftType, rightType, out Type newLeftType, out Type newRightType))
            {
                result = GetCommutativityForOperation(newLeftType, newRightType, methodName, opAbbreviation);
                commutativityCache[(leftType, rightType, methodName)] = result;
                return result;
            }

            commutativityCache[(leftType, rightType, methodName)] = false;
            return false;
        }

        private static bool CheckCommutativityExplicit(Type left, Type right, string methodName, out bool result)
        {
            MethodInfo method1 = left.GetMethod(methodName, FLAGS, null, new[] { left, right }, null);
            CommutativeAttribute commutativeAttribute = method1?.GetCustomAttribute<CommutativeAttribute>();
            if (method1 != null)
            {
                if (commutativeAttribute != null)
                    result = commutativeAttribute.IsCommutative;
                else
                    result = DefaultCommutativity(methodName, left, right);
                return true;
            }
            MethodInfo method2 = right.GetMethod(methodName, FLAGS, null, new[] { left, right }, null);
            commutativeAttribute = method2?.GetCustomAttribute<CommutativeAttribute>();
            if (method2 != null)
            {
                if (commutativeAttribute != null)
                    result = commutativeAttribute.IsCommutative;
                else
                    result = DefaultCommutativity(methodName, left, right);
                return true;
            }
            result = false;
            return false;
        }
        private static bool DefaultCommutativity(string methodName, Type leftType, Type rightType)
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
                    return IsNegatable(leftType, rightType);
                case "op_Division":
                case "op_ExclusiveOr":
                default:
                    return false;
            }
        }

        public static bool IsNegatable(Type leftType, Type rightType)
        {
            NegatableAttribute negatableAttribute = rightType.GetCustomAttribute<NegatableAttribute>();
            if (negatableAttribute != null &&
                !negatableAttribute.IsNegatable)
                return false;

            MethodInfo methodNegation = rightType.GetMethod("op_UnaryNegation", FLAGS, null, new[] { rightType }, null);
            if (methodNegation == null)
                return false;
            Type negatedType = methodNegation.ReturnType;
            MethodInfo methodAddition = leftType.GetMethod("op_Addition", FLAGS, null, new[] { negatedType, leftType }, null) ??
                rightType.GetMethod("op_Addition", FLAGS, null, new[] { negatedType, leftType }, null);
            MethodInfo methodSubtraction = leftType.GetMethod("op_Subtraction", FLAGS, null, new[] { leftType, rightType }, null) ??
                rightType.GetMethod("op_Subtraction", FLAGS, null, new[] { leftType, rightType }, null);
            return methodNegation != null &&
                methodAddition != null &&
                methodSubtraction != null;
        }

        public static bool IsReversible(Type leftType, Type rightType, BinaryOpcode operation)
        {
            if (typeof(ScalarValue).IsAssignableFrom(leftType) && typeof(ScalarValue).IsAssignableFrom(rightType))
            {
                if (IsComparison(operation))
                    return false;
                return true;
            }
            else if (typeof(StringValue).IsAssignableFrom(leftType) || typeof(StringValue).IsAssignableFrom(rightType))
            {
                return false;
            }
            else if (typeof(BooleanValue).IsAssignableFrom(leftType) || typeof(BooleanValue).IsAssignableFrom(rightType))
            {
                if (IsComparison(operation))
                    return false;
                return true;
            }
            else if (typeof(ISuffixed).IsAssignableFrom(leftType) || typeof(ISuffixed).IsAssignableFrom(rightType))
            {
                switch (operation)
                {
                    case OpcodeMathAdd _:
                        return GetReversabilityForOperation(leftType, rightType, "op_Addition", "+");
                    case OpcodeMathSubtract _:
                        return GetReversabilityForOperation(leftType, rightType, "op_Subtraction", "-");
                    case OpcodeMathMultiply _:
                        return GetReversabilityForOperation(leftType, rightType, "op_Multiply", "*");
                    case OpcodeMathDivide _:
                        return GetReversabilityForOperation(leftType, rightType, "op_Division", "/");
                    case OpcodeMathPower _:
                        return GetReversabilityForOperation(leftType, rightType, "op_ExclusiveOr", "^");
                    case OpcodeCompareGT _:
                        return GetReversabilityForOperation(leftType, rightType, "op_GreaterThan", ">");
                    case OpcodeCompareLT _:
                        return GetReversabilityForOperation(leftType, rightType, "op_LessThan", "<");
                    case OpcodeCompareGTE _:
                        return GetReversabilityForOperation(leftType, rightType, "op_GreaterThanEqual", ">=");
                    case OpcodeCompareLTE _:
                        return GetReversabilityForOperation(leftType, rightType, "op_LessThanEqual", "<=");
                    case OpcodeCompareNE _:
                        return GetReversabilityForOperation(leftType, rightType, "op_Inequality", "!=");
                    case OpcodeCompareEqual _:
                        return GetReversabilityForOperation(leftType, rightType, "op_Equality", "==");
                }
            }
            throw new NotImplementedException(string.Format("Can't operate types {0} and {1}", leftType, rightType));
        }
        private static Dictionary<(Type, Type, string), bool> reversabilityCache = new Dictionary<(Type, Type, string), bool>();
        private static bool GetReversabilityForOperation(Type leftType, Type rightType, string methodName, string opAbbreviation)
        {
            if (reversabilityCache.TryGetValue((leftType, rightType, methodName), out bool result))
                return result;
            if (CheckTypesForNull(leftType, rightType))
                return false;
            if (CheckReversabilityExplicit(leftType, rightType, methodName, out result))
            {
                reversabilityCache[(leftType, rightType, methodName)] = result;
                return result;
            }

            if (TryTypingImplicit(leftType, rightType, out Type newLeftType, out Type newRightType))
            {
                result = GetReversabilityForOperation(newLeftType, newRightType, methodName, opAbbreviation);
                reversabilityCache[(leftType, rightType, methodName)] = result;
                return result;
            }

            commutativityCache[(leftType, rightType, methodName)] = false;
            return false;
        }

        private static bool CheckReversabilityExplicit(Type left, Type right, string methodName, out bool result)
        {
            string reversedName;
            switch (methodName)
            {
                case "op_Addition":
                    reversedName = "op_Subtraction";
                    break;
                case "op_Subtraction":
                    reversedName = "op_Addition";
                    break;
                case "op_Multiply":
                    reversedName = "op_Division";
                    break;
                case "op_Division":
                    reversedName = "op_Multiply";
                    break;
                default:
                    result = false;
                    return true;
            }
            MethodInfo reversedMethod = left.GetMethod(reversedName, FLAGS, null, new[] { left, right }, null) ??
                right.GetMethod(reversedName, FLAGS, null, new[] { left, right }, null);

            MethodInfo method1 = left.GetMethod(methodName, FLAGS, null, new[] { left, right }, null);
            IrreversibleAttribute reversibleAttribute = method1?.GetCustomAttribute<IrreversibleAttribute>();
            if (method1 != null)
            {
                if (reversibleAttribute != null)
                    result = !reversibleAttribute.IsIrreversible;
                else
                    result = reversedMethod != null && DefaultReversability(methodName, left, right);
                return true;
            }

            MethodInfo method2 = right.GetMethod(methodName, FLAGS, null, new[] { left, right }, null);
            reversibleAttribute = method2?.GetCustomAttribute<IrreversibleAttribute>();
            if (method2 != null)
            {if (reversibleAttribute != null)
                    result = !reversibleAttribute.IsIrreversible;
                else
                    result = reversedMethod != null && DefaultReversability(methodName, left, right);
                return true;
            }
            result = false;
            return false;
        }
        private static bool DefaultReversability(string methodName, Type leftType, Type rightType)
        {
            switch (methodName)
            {
                case "op_Addition":
                case "op_Multiply":
                case "op_Subtraction":
                case "op_Division":
                    return true;
                case "op_GreaterThan":
                case "op_LessThan":
                case "op_GreaterThanEqual":
                case "op_LessThanEqual":
                    return false;
                case "op_Equality":
                case "op_Inequality":
                    return false;
                case "op_ExclusiveOr":
                default:
                    return false;
            }
        }

        private static bool IsComparison(Opcode operation)
        {
            switch (operation)
            {
                case OpcodeCompareEqual _:
                case OpcodeCompareNE _:
                case OpcodeCompareGT _:
                case OpcodeCompareGTE _:
                case OpcodeCompareLT _:
                case OpcodeCompareLTE _:
                    return true;
                default:
                    return false;
            }
        }
    }
}
