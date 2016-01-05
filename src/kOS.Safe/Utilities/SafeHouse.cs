using kOS.Safe.Encapsulation;

namespace kOS.Safe.Utilities
{
    public static class SafeHouse
    {
        public static IConfig Config { get; private set; }
        public static bool IsWindows { get; private set; }
        public static string ArchiveFolder { get; private set; }
        public static VersionInfo Version { get; private set; }

        public static void Init(IConfig config, VersionInfo versionInfo, bool isWindows, string archiveFolder)
        {
            Config = config;
            IsWindows = isWindows;
            ArchiveFolder = archiveFolder;
            Version = versionInfo;
        }

        public static ILogger Logger { get; set; }
    }
}
