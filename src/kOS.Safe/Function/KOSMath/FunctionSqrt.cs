using System;

namespace kOS.Safe.Function.KOSMath
{
    [Function("sqrt")]
    public class FunctionSqrt : SafeFunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            double result = Math.Sqrt(argument);
            ReturnValue = result;
        }
    }
}