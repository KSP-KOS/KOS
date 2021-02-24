using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Utilities;

namespace kOS.Safe.Function
{
    [Function("sin", Contexts = new string[] { "ksp", "archive" })]
    public class FunctionSin : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
        {
            double degrees = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            double radians = DegreesToRadians(degrees);
            double result = Math.Sin(radians);
            ReturnValue = result;
        }
    }

    [Function("cos", Contexts = new string[] { "ksp", "archive" })]
    public class FunctionCos : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
        {
            double degrees = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            double radians = DegreesToRadians(degrees);
            double result = Math.Cos(radians);
            ReturnValue = result;
        }
    }

    [Function("tan", Contexts = new string[] { "ksp", "archive" })]
    public class FunctionTan : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
        {
            double degrees = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            double radians = DegreesToRadians(degrees);
            double result = Math.Tan(radians);
            ReturnValue = result;
        }
    }

    [Function("arcsin", Contexts = new string[] { "ksp", "archive" })]
    public class FunctionArcSin : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
        {
            double argument = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            double result = RadiansToDegrees(Math.Asin(argument));
            ReturnValue = result;
        }
    }

    [Function("arccos", Contexts = new string[] { "ksp", "archive" })]
    public class FunctionArcCos : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
        {
            double argument = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            double result = RadiansToDegrees(Math.Acos(argument));
            ReturnValue = result;
        }
    }

    [Function("arctan", Contexts = new string[] { "ksp", "archive" })]
    public class FunctionArcTan : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
        {
            double argument = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            double result = RadiansToDegrees(Math.Atan(argument));
            ReturnValue = result;
        }
    }

    [Function("arctan2", Contexts = new string[] { "ksp", "archive" })]
    public class FunctionArcTan2 : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
        {
            double x = GetDouble(PopValueAssert(shared));
            double y = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            double result = RadiansToDegrees(Math.Atan2(y, x));
            ReturnValue = result;
        }
    }

    [Function("anglediff", Contexts = new string[] { "ksp", "archive" })]
    public class FunctionAngleDiff : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
        {
            double ang2 = GetDouble(PopValueAssert(shared));
            double ang1 = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            double result = KOSMath.DegreeFix( ang2 - ang1, -180 );
            ReturnValue = result;
        }
    }
}
