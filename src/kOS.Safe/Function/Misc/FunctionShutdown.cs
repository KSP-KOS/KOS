using kOS.Safe.Module;

namespace kOS.Safe.Function.Misc
{
    [Function("shutdown")]
    public class FunctionShutdown : SafeFunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            AssertArgBottomAndConsume(shared); // not sure if this matters when shutting down anwyway.
            if (shared.Processor != null) shared.Processor.SetMode(ProcessorModes.OFF);
            shared.Cpu.GetCurrentOpcode().AbortProgram = true;
        }
    }
}