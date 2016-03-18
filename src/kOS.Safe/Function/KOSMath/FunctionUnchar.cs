using kOS.Safe.Encapsulation;

namespace kOS.Safe.Function.KOSMath
{
    [Function("unchar")]
    public class FunctionUnchar : SafeFunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string argument = PopValueAssert(shared).ToString();
            AssertArgBottomAndConsume(shared);
            char result = argument.ToCharArray()[0];
            ReturnValue = ScalarValue.Create(result);
        }
    }
}