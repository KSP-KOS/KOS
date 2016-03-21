using kOS.Safe.Compilation;

namespace kOS.Safe.Function.KOSMath
{
    [Function("min")]
    public class FunctionMin : SafeFunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            object argument1 = PopValueAssert(shared);
            object argument2 = PopValueAssert(shared);
            AssertArgBottomAndConsume(shared);

            var pair = new OperandPair(argument1, argument2);
            Calculator calculator = Calculator.GetCalculator(pair);
            object result = calculator.Min(pair);
            ReturnValue = result;
        }
    }
}