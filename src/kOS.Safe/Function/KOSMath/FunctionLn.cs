using System;

namespace kOS.Safe.Function.KOSMath
{
    [Function("ln")]
    public class FunctionLn : SafeFunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            double result = Math.Log(argument);
            ReturnValue = result;
        }
    }
}