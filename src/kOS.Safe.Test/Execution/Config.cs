using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using System;
using System.Collections.Generic;

namespace kOS.Safe.Test
{
    internal class Config : IConfig
    {
        public bool AudibleExceptions
        {
            get
            {
                return false;
            }

            set
            {
            }
        }

        public bool DebugEachOpcode
        {
            get
            {
                return false;
            }

            set
            {
            }
        }

        public bool EnableSafeMode
        {
            get
            {
                return true;
            }

            set
            {
            }
        }

        public bool EnableTelnet
        {
            get
            {
                return false;
            }

            set
            {
            }
        }

        public int InstructionsPerUpdate
        {
            get
            {
                return 10000; // high enough to run everything in one pass (unless it waits)
            }

            set
            {
            }
        }
        
        public int LuaInstructionsPerUpdate
        {
            get
            {
                return 10000; // high enough to run everything in one pass (unless it waits)
            }

            set
            {
            }
        }

        public bool ObeyHideUI
        {
            get
            {
                return true;
            }

            set
            {
            }
        }

        public bool ShowStatistics
        {
            get
            {
                return false;
            }

            set
            {
            }
        }

        public bool StartOnArchive
        {
            get
            {
                return true;
            }

            set
            {
            }
        }

        public string TelnetIPAddrString
        {
            get
            {
                return "";
            }

            set
            {
            }
        }

        public int TelnetPort
        {
            get
            {
                return 0;
            }

            set
            {
            }
        }

        public double TerminalBrightness
        {
            get
            {
                return 1;
            }

            set
            {
            }
        }

        public int TerminalFontDefaultSize
        {
            get
            {
                return 12;
            }

            set
            {
            }
        }

        public string TerminalFontName
        {
            get
            {
                return "";
            }

            set
            {
            }
        }

        public int TerminalDefaultWidth
        {
            get
            {
                return 0;
            }

            set
            {
            }
        }

        public int TerminalDefaultHeight
        {
            get
            {
                return 0;
            }

            set
            {
            }
        }

        public bool SuppressAutopilot
        {
            get
            {
                return true;
            }

            set
            {
            }
        }
        public DateTime TimeStamp
        {
            get
            {
                return new DateTime();
            }
        }

        public bool UseBlizzyToolbarOnly
        {
            get
            {
                return false;
            }

            set
            {
            }
        }

        public bool UseCompressedPersistence
        {
            get
            {
                return false;
            }

            set
            {
            }
        }

        public bool VerboseExceptions
        {
            get
            {
                return true;
            }

            set
            {
            }
        }

        public bool AllowClobberBuiltIns
        {
            get
            {
                return false;
            }

            set
            {
            }
        }

        public IList<ConfigKey> GetConfigKeys()
        {
            return new List<ConfigKey>();
        }

        public ISuffixResult GetSuffix(string suffixName, bool failOkay = false)
        {
            throw new NotImplementedException();
        }

        public void SaveConfig()
        {
            throw new NotImplementedException();
        }

        public bool SetSuffix(string suffixName, object value, bool failOkay = false)
        {
            throw new NotImplementedException();
        }
    }
}