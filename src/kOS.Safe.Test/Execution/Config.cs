using System;
using System.Collections.Generic;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Safe.Test
{
    class Config : IConfig
    {
        public bool AudibleExceptions {
            get {
                return false;
            }

            set {
            }
        }

        public bool DebugEachOpcode {
            get {
                return false;
            }

            set {
            }
        }

        public bool EnableSafeMode {
            get {
                return true;
            }

            set {
            }
        }

        public bool EnableTelnet {
            get {
                return false;
            }

            set {
            }
        }

        public int InstructionsPerUpdate {
            get {
                return 10000; // high enough to run everything in one pass (unless it waits)
            }

            set {
            }
        }

        public bool ObeyHideUI {
            get {
                return true;
            }

            set {
            }
        }

        public bool ShowStatistics {
            get {
                return false;
            }

            set {
            }
        }

        public bool StartOnArchive {
            get {
                return true;
            }

            set {
            }
        }

        public string TelnetIPAddrString {
            get {
                return "";
            }

            set {
            }
        }

        public int TelnetPort {
            get {
                return 0;
            }

            set {
            }
        }

        public double TerminalBrightness {
            get {
                return 1;
            }

            set {
            }
        }

        public int TerminalFontDefaultSize {
            get {
                return 12;
            }

            set {
            }
        }

        public string TerminalFontName {
            get {
                return "";
            }

            set {
            }
        }

        public DateTime TimeStamp {
            get {
                return new DateTime();
            }
        }

        public bool UseBlizzyToolbarOnly {
            get {
                return false;
            }

            set {
            }
        }

        public bool UseCompressedPersistence {
            get {
                return false;
            }

            set {
            }
        }

        public bool VerboseExceptions {
            get {
                return true;
            }

            set {
            }
        }

        public IList<ConfigKey> GetConfigKeys ()
        {
            return new List<ConfigKey>();
        }

        public ISuffixResult GetSuffix (string suffixName)
        {
            throw new NotImplementedException ();
        }

        public void SaveConfig ()
        {
            throw new NotImplementedException ();
        }

        public bool SetSuffix (string suffixName, object value)
        {
            throw new NotImplementedException ();
        }
    }
}
