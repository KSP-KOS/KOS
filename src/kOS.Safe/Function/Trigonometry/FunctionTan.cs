using System;

namespace kOS.Safe.Function.Trigonometry
{
    [Function("tan")]
    public class FunctionTan : SafeFunctionBase
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
}