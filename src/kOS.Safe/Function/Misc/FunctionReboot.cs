using kOS.Safe.Module;

namespace kOS.Safe.Function.Misc
{
    [Function("reboot")]
    public class FunctionReboot : SafeFunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            if (shared.Processor != null)
            {
                AssertArgBottomAndConsume(shared); // not sure if this matters when rebooting anwyway.
                shared.Processor.SetMode(ProcessorModes.OFF);
                shared.Processor.SetMode(ProcessorModes.READY);
                shared.Cpu.GetCurrentOpcode().AbortProgram = true;
            }
        }
    }
}