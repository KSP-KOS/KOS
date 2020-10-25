using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;

namespace kOS.Safe.Function
{
    [Function("range")]
    public class FunctionRange : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
        {
            // Default values for parameters
            double from = RangeValue.DEFAULT_START;
            double to = RangeValue.DEFAULT_STOP;
            double step = RangeValue.DEFAULT_STEP;

            int argCount = CountRemainingArgs(shared);
            // assign parameter values from the stack, pop them in reverse order
            switch (argCount)
            {
                case 1:
                    to = GetDouble(PopStructureAssertEncapsulated(shared));
                    break;
                case 2:
                    to = GetDouble(PopStructureAssertEncapsulated(shared));
                    from = GetDouble(PopStructureAssertEncapsulated(shared));
                    break;
                case 3:
                    step = GetDouble(PopStructureAssertEncapsulated(shared));
                    to = GetDouble(PopStructureAssertEncapsulated(shared));
                    from = GetDouble(PopStructureAssertEncapsulated(shared));
                    break;
                default:
                    throw new KOSArgumentMismatchException(new int[] { 1, 2, 3 }, argCount, "Thrown from function RANGE()");
            }
            AssertArgBottomAndConsume(shared);

            ReturnValue = new RangeValue(from, to, step);
        }
    }

    [Function("constant")]
    public class FunctionConstant : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
        {
            AssertArgBottomAndConsume(shared); // no args
            ReturnValue = new ConstantValue();
        }
    }
}