using System;

namespace kOS.Safe.Function.KOSMath
{
    [Function("ceiling")]
    public class FunctionCeiling : SafeFunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            double result = Math.Ceiling(argument);
            ReturnValue = result;
        }
    }
}