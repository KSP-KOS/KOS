namespace kOS.Safe.Module
{
    public interface IProcessor
    {
        void SetMode(ProcessorModes newProcessorMode);
        string BootFilename { get; set; }
    }
    public enum ProcessorModes { READY, STARVED, OFF };
}