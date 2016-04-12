using kOS.Safe.Persistence;

namespace kOS.Safe.Module
{
    public interface IProcessor
    {
        void SetMode(ProcessorModes newProcessorMode);

        /// <summary>
        /// Gets or sets the boot file path. Has to be a valid path or null.
        /// </summary>
        GlobalPath BootFilePath { get; }

        bool CheckCanBoot();
        string Tag { get; }
    }
    public enum ProcessorModes { READY, STARVED, OFF };
}