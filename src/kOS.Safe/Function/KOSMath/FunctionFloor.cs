using System;

namespace kOS.Safe.Function.KOSMath
{
    [Function("floor")]
    public class FunctionFloor : SafeFunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            double result = Math.Floor(argument);
            ReturnValue = result;
        }
    }
}