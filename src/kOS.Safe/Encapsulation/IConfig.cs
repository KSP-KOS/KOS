using System;
using System.Collections.Generic;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Safe.Encapsulation
{
    public interface IConfig: ISuffixed
    {
        int InstructionsPerUpdate { get; set; }
        bool UseCompressedPersistence { get; set; }
        bool ShowStatistics { get; set; }
        bool StartOnArchive { get; set; }
        bool ObeyHideUI { get; set; }
        bool EnableSafeMode { get; set; }
        bool VerboseExceptions { get; set; }
        bool EnableTelnet { get; set; }
        int TelnetPort { get; set; }
        bool AudibleExceptions { get; set; }
        bool TelnetLoopback { get; set; }
        bool UseBlizzyToolbarOnly { get; set; }

        /// <summary>
        /// Return the moment in time when the most recent change to any of the
        /// config values happened.  Used by KOSTollBarWindow to decide whether or not
        /// it needs to assume its cached values are stale and need re-loading.         
        /// </summary>
        DateTime TimeStamp { get; }

        bool DebugEachOpcode { get; set; }

        void SaveConfig();
        IList<ConfigKey> GetConfigKeys();
    }
}