namespace kOS.Safe.Encapsulation
{
    public interface IConfig: ISuffixed, IOperable
    {
        int InstructionsPerUpdate { get; set; }
        bool UseCompressedPersistence { get; set; }
        bool ShowStatistics { get; set; }
        bool EnableRTIntegration { get; set; }
        bool StartOnArchive { get; set; }
        bool EnableSafeMode { get; set; }
        bool VerboseExceptions { get; set; }
        bool EnableTelnet { get; set; }
        int TelnetPort { get; set; }
        void SaveConfig();
    }
}