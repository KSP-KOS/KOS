namespace kOS.Safe.Module
{
    public interface IProcessor
    {
        void SetMode(ProcessorModes newProcessorMode);
        //ProcessorModes GetMode();
        string BootFilename { get; set; }

        bool CheckCanBoot();

        void FixedUpdate();
    }
    public enum ProcessorModes { READY, STARVED, OFF };
}