using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;

namespace kOS.Safe.Function.Suffixed
{
    [Function("pidloop")]
    public class PIDLoopConstructor : SafeFunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            int args = CountRemainingArgs(shared);
            double kd;
            double ki;
            double kp;
            double maxoutput;
            double minoutput;
            switch (args)
            {
                case 0:
                    this.ReturnValue = new PIDLoop();
                    break;

                case 1:
                    kp = GetDouble(PopValueAssert(shared));
                    this.ReturnValue = new PIDLoop(kp, 0, 0);
                    break;

                case 3:
                    kd = GetDouble(PopValueAssert(shared));
                    ki = GetDouble(PopValueAssert(shared));
                    kp = GetDouble(PopValueAssert(shared));
                    this.ReturnValue = new PIDLoop(kp, ki, kd);
                    break;

                case 5:
                    maxoutput = GetDouble(PopValueAssert(shared));
                    minoutput = GetDouble(PopValueAssert(shared));
                    kd = GetDouble(PopValueAssert(shared));
                    ki = GetDouble(PopValueAssert(shared));
                    kp = GetDouble(PopValueAssert(shared));
                    this.ReturnValue = new PIDLoop(kp, ki, kd, maxoutput, minoutput);
                    break;

                default:
                    throw new KOSArgumentMismatchException(new[] { 0, 1, 3, 5 }, args);
            }
            AssertArgBottomAndConsume(shared);
        }
    }
}