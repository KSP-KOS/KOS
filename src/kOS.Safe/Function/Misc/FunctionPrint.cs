using kOS.Safe.Utilities;

namespace kOS.Safe.Function.Misc
{
    [Function("print")]
    public class FunctionPrint : SafeFunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string textToPrint = PopValueAssert(shared).ToString();
            AssertArgBottomAndConsume(shared);
            if (shared.Screen == null)
            {
                SafeHouse.Logger.Log(textToPrint);
            }
            else
            {
                shared.Screen.Print(textToPrint);
            }
        }
    }
}