using kOS.Safe.Encapsulation;

namespace kOS.Safe.Function.KOSMath
{
    [Function("char")]
    public class FunctionChar : SafeFunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            string result = new string((char)argument, 1);
            ReturnValue = new StringValue(result);
        }
    }
}