using kOS.Safe.Encapsulation;

namespace kOS.Safe.Function.Misc
{
    [Function("makebuiltindelegate")]
    public class MakeBuiltinDelegate : SafeFunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string name = PopValueAssert(shared).ToString();
            AssertArgBottomAndConsume(shared);

            ReturnValue = new BuiltinDelegate(shared.Cpu, name);
        }
    }
}