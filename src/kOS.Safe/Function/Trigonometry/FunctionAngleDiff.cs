namespace kOS.Safe.Function.Trigonometry
{
    [Function("anglediff")]
    public class FunctionAngleDiff : SafeFunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double ang2 = GetDouble(PopValueAssert(shared));
            double ang1 = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            double result = kOS.Safe.Utilities.Math.DegreeFix(ang2 - ang1, -180);
            ReturnValue = result;
        }
    }
}