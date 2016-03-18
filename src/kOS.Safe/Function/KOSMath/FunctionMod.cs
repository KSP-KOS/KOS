namespace kOS.Safe.Function.KOSMath
{
    [Function("mod")]
    public class FunctionMod : SafeFunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double divisor = GetDouble(PopValueAssert(shared));
            double dividend = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            double result = dividend % divisor;
            ReturnValue = result;
        }
    }
}