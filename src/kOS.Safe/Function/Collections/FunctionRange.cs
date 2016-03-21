using kOS.Safe.Exceptions;

namespace kOS.Safe.Function.Collections
{
    [Function("range")]
    public class FunctionRange : SafeFunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            // Default values for parameters
            int from = RangeValue.DEFAULT_START;
            int to = RangeValue.DEFAULT_STOP;
            int step = RangeValue.DEFAULT_STEP;

            int argCount = CountRemainingArgs(shared);
            // assign parameter values from the stack, pop them in reverse order
            switch (argCount)
            {
                case 1:
                    to = GetInt(PopStructureAssertEncapsulated(shared));
                    break;

                case 2:
                    to = GetInt(PopStructureAssertEncapsulated(shared));
                    from = GetInt(PopStructureAssertEncapsulated(shared));
                    break;

                case 3:
                    step = GetInt(PopStructureAssertEncapsulated(shared));
                    to = GetInt(PopStructureAssertEncapsulated(shared));
                    from = GetInt(PopStructureAssertEncapsulated(shared));
                    break;

                default:
                    throw new KOSArgumentMismatchException(new int[] { 1, 2, 3 }, argCount, "Thrown from function RANGE()");
            }
            AssertArgBottomAndConsume(shared);

            ReturnValue = new RangeValue(from, to, step);
        }
    }
}