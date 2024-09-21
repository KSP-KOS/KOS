using kOS.Safe.Persistence;

namespace kOS.Safe.Module
{
    public interface IProcessor
    {
        void SetMode(ProcessorModes newProcessorMode);

        /// <summary>
        /// Gets or sets the boot file path. Has to be a valid path or null.
        /// </summary>
        VolumePath BootFilePath { get; }

        bool CheckCanBoot();
        string Tag { get; }
        
        string InterpreterLanguage { get; }

        /// <summary>Can be used as a unique ID of which processor this is, but unlike Guid,
        /// it doesn't remain unique across runs so you shouldn't use it for serialization.</summary>
        int KOSCoreId { get; }

        /// <summary>
        /// Has this CPU ever been booted yet? (Should only be false just after a scene switch.
        /// Once physics easing has finished, the FixedUpdate should have booted and set this true.)
        /// </summary>
        bool HasBooted { get; }
    }
    public enum ProcessorModes { READY, STARVED, OFF };
}