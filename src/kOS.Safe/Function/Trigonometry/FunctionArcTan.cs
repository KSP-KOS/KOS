using System;

namespace kOS.Safe.Function.Trigonometry
{
    [Function("arctan")]
    public class FunctionArcTan : SafeFunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            double result = RadiansToDegrees(Math.Atan(argument));
            ReturnValue = result;
        }
    }
}