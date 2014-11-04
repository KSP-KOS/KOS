namespace kOS.Safe.Module
{
    public interface IProcessor
    {
        void SetMode(ProcessorModes newProcessorMode);
    }
    public enum ProcessorModes { READY, STARVED, OFF };
}