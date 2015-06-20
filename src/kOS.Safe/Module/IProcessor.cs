namespace kOS.Safe.Module
{
    public interface IProcessor
    {
        void SetMode(ProcessorModes newProcessorMode);
        string GetBootFileName();
        void SetBootFileName(string name);
    }
    public enum ProcessorModes { READY, STARVED, OFF };
}