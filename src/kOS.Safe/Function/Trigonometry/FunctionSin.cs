using System;

namespace kOS.Safe.Function.Trigonometry
{
    [Function("sin")]
    public class FunctionSin : SafeFunctionBase
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
}