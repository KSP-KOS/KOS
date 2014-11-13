namespace kOS.Safe.Encapsulation
{
    public interface IConfig: ISuffixed, IOperable
    {
        int InstructionsPerUpdate { get; set; }
        bool UseCompressedPersistence { get; set; }
        bool ShowStatistics { get; set; }
        bool EnableRT2Integration { get; set; }
        bool StartOnArchive { get; set; }
        bool EnableSafeMode { get; set; }
        bool VerboseExceptions { get; set; }
        void SaveConfig();
    }
}