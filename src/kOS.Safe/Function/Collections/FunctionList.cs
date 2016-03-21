using kOS.Safe.Encapsulation;
using System.Linq;

namespace kOS.Safe.Function.Collections
{
    [Function("list")]
    public class FunctionList : SafeFunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            Structure[] argArray = new Structure[CountRemainingArgs(shared)];
            for (int i = argArray.Length - 1; i >= 0; --i)
                argArray[i] = PopStructureAssertEncapsulated(shared); // fill array in reverse order because .. stack args.
            AssertArgBottomAndConsume(shared);
            var listValue = new ListValue(argArray.ToList());
            ReturnValue = listValue;
        }
    }
}