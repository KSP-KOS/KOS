using System;

namespace kOS.Safe.Function.Trigonometry
{
    [Function("arcsin")]
    public class FunctionArcSin : SafeFunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            double result = RadiansToDegrees(Math.Asin(argument));
            ReturnValue = result;
        }
    }
}