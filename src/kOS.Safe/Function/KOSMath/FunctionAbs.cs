using System;

namespace kOS.Safe.Function.KOSMath
{
    [Function("abs")]
    public class FunctionAbs : SafeFunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            double result = Math.Abs(argument);
            ReturnValue = result;
        }
    }
}