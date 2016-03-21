using System;

namespace kOS.Safe.Function.Trigonometry
{
    [Function("arctan2")]
    public class FunctionArcTan2 : SafeFunctionBase
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
}