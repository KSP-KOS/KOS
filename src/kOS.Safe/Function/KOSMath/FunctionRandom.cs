using kOS.Safe.Encapsulation;
using System;

namespace kOS.Safe.Function.KOSMath
{
    [Function("random")]
    public class FunctionRandom : SafeFunctionBase
    {
        private readonly Random random = new Random();

        public override void Execute(SharedObjects shared)
        {
            AssertArgBottomAndConsume(shared);
            ReturnValue = ScalarValue.Create(random.NextDouble());
        }
    }
}