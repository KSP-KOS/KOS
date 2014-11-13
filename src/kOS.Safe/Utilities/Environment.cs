using kOS.Safe.Encapsulation;

namespace kOS.Safe.Utilities
{
    public static class Environment
    {
        public static IConfig Config { get; private set; }
        public static bool IsWindows { get; private set; }
        public static string ArchiveFolder { get; private set; }

        public static void Init(IConfig config, bool isWindows, string archiveFolder)
        {
            Config = config;
            IsWindows = isWindows;
            ArchiveFolder = archiveFolder;
        }
    }
}
