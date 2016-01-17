using System;
using kOS.Safe.Function;
using kOS.Safe.Encapsulation;

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
            ReturnValue = Structure.FromPrimitive(result);
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
            ReturnValue = Structure.FromPrimitive(result);
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
            ReturnValue = Structure.FromPrimitive(result);
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
            ReturnValue = Structure.FromPrimitive(result);
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
            ReturnValue = Structure.FromPrimitive(result);
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
            ReturnValue = Structure.FromPrimitive(result);
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
            ReturnValue = Structure.FromPrimitive(result);
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
            ReturnValue = Structure.FromPrimitive(result);
        }
    }
}
