using System;
using kOS.Safe.Function;

namespace kOS.Function
{
    [Function("sin")]
    public class FunctionSin : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double degrees = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            double radians = DegreesToRadians(degrees);
            double result = Math.Sin(radians);
            ReturnValue = result;
        }
    }

    [Function("cos")]
    public class FunctionCos : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double degrees = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            double radians = DegreesToRadians(degrees);
            double result = Math.Cos(radians);
            ReturnValue = result;
        }
    }

    [Function("tan")]
    public class FunctionTan : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double degrees = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            double radians = DegreesToRadians(degrees);
            double result = Math.Tan(radians);
            ReturnValue = result;
        }
    }

    [Function("arcsin")]
    public class FunctionArcSin : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            double result = RadiansToDegrees(Math.Asin(argument));
            ReturnValue = result;
        }
    }

    [Function("arccos")]
    public class FunctionArcCos : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            double result = RadiansToDegrees(Math.Acos(argument));
            ReturnValue = result;
        }
    }

    [Function("arctan")]
    public class FunctionArcTan : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            double result = RadiansToDegrees(Math.Atan(argument));
            ReturnValue = result;
        }
    }

    [Function("arctan2")]
    public class FunctionArcTan2 : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double x = GetDouble(PopValueAssert(shared));
            double y = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            double result = RadiansToDegrees(Math.Atan2(y, x));
            ReturnValue = result;
        }
    }

    [Function("anglediff")]
    public class FunctionAngleDiff : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double ang2 = GetDouble(PopValueAssert(shared));
            double ang1 = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            double result = kOS.Utilities.Utils.DegreeFix( ang2 - ang1, -180 );
            ReturnValue = result;
        }
    }
}
