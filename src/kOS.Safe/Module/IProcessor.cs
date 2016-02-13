namespace kOS.Safe.Module
{
    public interface IProcessor
    {
        void SetMode(ProcessorModes newProcessorMode);
        string BootFilename { get; set; }

        bool CheckCanBoot();
    }
    public enum ProcessorModes { READY, STARVED, OFF };
}