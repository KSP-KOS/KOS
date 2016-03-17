using kOS.Safe.Encapsulation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.CommandLine.Properties;

namespace kOS.CommandLine.Encapsulation
{
    public class Config : Structure, IConfig
    {
        private static Config fetch;
        public static Config Fetch
        {
            get
            {
                if (fetch == null) fetch = new Config();
                return fetch;
            }
        }

        public int InstructionsPerUpdate
        {
            get
            {
                return 2000;
                return Settings.Default.ipu;
            }
            set
            {
                Settings.Default.ipu = value;
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
                throw new NotImplementedException();
            }
        }

        public bool ShowStatistics
        {
            get
            {
                return true;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public bool EnableRTIntegration
        {
            get
            {
                return false;
            }
            set
            {
                throw new NotImplementedException();
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
                throw new NotImplementedException();
            }
        }

        public bool ObeyHideUI
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
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
                throw new NotImplementedException();
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
                throw new NotImplementedException();
            }
        }

        public bool EnableTelnet
        {
            get
            {
                return true;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public int TelnetPort
        {
            get
            {
                return 5410;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public bool AudibleExceptions
        {
            get
            {
                return false;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public bool TelnetLoopback
        {
            get
            {
                return true;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public bool UseBlizzyToolbarOnly
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public DateTime TimeStamp
        {
            get { throw new NotImplementedException(); }
        }

        public bool DebugEachOpcode
        {
            get
            {
                return false;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public void SaveConfig()
        {
            Settings.Default.Save();
        }

        public IList<Safe.Encapsulation.Suffixes.ConfigKey> GetConfigKeys()
        {
            throw new NotImplementedException();
        }
    }
}
