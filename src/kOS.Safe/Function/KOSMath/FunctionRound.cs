using kOS.Safe.Exceptions;
using System;

namespace kOS.Safe.Function.KOSMath
{
    [Function("round")]
    public class FunctionRound : SafeFunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            int decimals;
            int argCount = CountRemainingArgs(shared);

            switch (argCount)
            {
                case 1:
                    decimals = 0;
                    break;

                case 2:
                    decimals = GetInt(PopValueAssert(shared));
                    break;

                default:
                    throw new KOSArgumentMismatchException(new[] { 1, 2 }, argCount);
            }

            double argument = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            double result = Math.Round(argument, decimals);
            ReturnValue = result;
        }
    }
}