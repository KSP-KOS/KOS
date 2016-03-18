using System;

namespace kOS.Safe.Function.KOSMath
{
    [Function("log10")]
    public class FunctionLog10 : SafeFunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            double result = Math.Log10(argument);
            ReturnValue = result;
        }
    }
}