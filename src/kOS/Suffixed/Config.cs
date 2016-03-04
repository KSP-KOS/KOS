using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Screen;
using KSP.IO;

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("Config")]
    public class Config : Structure, IConfig
    {
        private static Config instance;
        private readonly Dictionary<string, ConfigKey> keys;
        private readonly Dictionary<string, ConfigKey> alias;
        private readonly Dictionary<PropId, ConfigKey> properties;

        public int InstructionsPerUpdate { get { return GetPropValue<int>(PropId.InstructionsPerUpdate); } set { SetPropValue(PropId.InstructionsPerUpdate, value); } }
        public bool UseCompressedPersistence { get { return GetPropValue<bool>(PropId.UseCompressedPersistence); } set { SetPropValue(PropId.UseCompressedPersistence, value); } }
        public bool ShowStatistics { get { return GetPropValue<bool>(PropId.ShowStatistics); } set { SetPropValue(PropId.ShowStatistics, value); } }
        public bool EnableRTIntegration { get { return GetPropValue<bool>(PropId.EnableRTIntegration); } set { SetPropValue(PropId.EnableRTIntegration, value); } }
        public bool StartOnArchive { get { return GetPropValue<bool>(PropId.StartOnArchive); } set { SetPropValue(PropId.StartOnArchive, value); } }
        public bool EnableSafeMode { get { return GetPropValue<bool>(PropId.EnableSafeMode); } set { SetPropValue(PropId.EnableSafeMode, value); } }
        public bool AudibleExceptions { get { return GetPropValue<bool>(PropId.AudibleExceptions); } set { SetPropValue(PropId.AudibleExceptions, value); } }
        public bool VerboseExceptions { get { return GetPropValue<bool>(PropId.VerboseExceptions); } set { SetPropValue(PropId.VerboseExceptions, value); } }
        public bool EnableTelnet { get { return GetPropValue<bool>(PropId.EnableTelnet); } set { SetPropValue(PropId.EnableTelnet, value); } }
        public int TelnetPort { get { return GetPropValue<int>(PropId.TelnetPort); } set { SetPropValue(PropId.TelnetPort, value); } }
        public bool TelnetLoopback { get { return GetPropValue<bool>(PropId.TelnetLoopback); } set { SetPropValue(PropId.TelnetLoopback, value); } }        
        public bool UseBlizzyToolbarOnly { get { return GetPropValue<bool>(PropId.UseBlizzyToolbarOnly); } set { SetPropValue(PropId.UseBlizzyToolbarOnly, value); } }
        public bool DebugEachOpcode { get { return GetPropValue<bool>(PropId.DebugEachOpcode); } set { SetPropValue(PropId.DebugEachOpcode, value); } }

        private Config()
        {
            keys = new Dictionary<string, ConfigKey>();
            alias = new Dictionary<string, ConfigKey>();
            properties = new Dictionary<PropId, ConfigKey>();
            BuildValuesDictionary();
            LoadConfig();
            TimeStamp = DateTime.Now;
        }

        private void BuildValuesDictionary()
        {
            AddConfigKey(PropId.InstructionsPerUpdate, new ConfigKey("InstructionsPerUpdate", "IPU", "Instructions per update", 200, 50, 2000, typeof(int)));
            AddConfigKey(PropId.UseCompressedPersistence, new ConfigKey("UseCompressedPersistence", "UCP", "Use compressed persistence", false, false, true, typeof(bool)));
            AddConfigKey(PropId.ShowStatistics, new ConfigKey("ShowStatistics", "STAT", "Show execution statistics", false, false, true, typeof(bool)));
            AddConfigKey(PropId.EnableRTIntegration, new ConfigKey("EnableRTIntegration", "RT", "Enable RT integration", true, false, true, typeof(bool)));
            AddConfigKey(PropId.StartOnArchive, new ConfigKey("StartOnArchive", "ARCH", "Start on Archive volume", false, false, true, typeof(bool)));
            AddConfigKey(PropId.EnableSafeMode, new ConfigKey("EnableSafeMode", "SAFE", "Enable safe mode", true, false, true, typeof(bool)));
            AddConfigKey(PropId.AudibleExceptions, new ConfigKey("AudibleExceptions", "AUDIOERR", "Sound effect when KOS gives an error", true, false, true, typeof(bool)));
            AddConfigKey(PropId.VerboseExceptions, new ConfigKey("VerboseExceptions", "VERBOSE", "Enable verbose exception msgs", true, false, true, typeof(bool)));
            AddConfigKey(PropId.EnableTelnet, new ConfigKey("EnableTelnet", "TELNET", "Enable Telnet server", false, false, true, typeof(bool)));
            AddConfigKey(PropId.TelnetPort, new ConfigKey("TelnetPort", "TPORT", "Telnet port number (must restart telnet to take effect)", 5410, 1024, 65535, typeof(int)));
            AddConfigKey(PropId.TelnetLoopback, new ConfigKey("TelnetLoopback", "LOOPBACK", "Restricts telnet to 127.0.0.1 (must restart telnet to take effect)", true, false, true, typeof(bool)));
            AddConfigKey(PropId.DebugEachOpcode , new ConfigKey("DebugEachOpcode", "DEBUGEACHOPCODE", "Unholy debug spam used by the kOS developers", false, false, true, typeof(bool)));
            if(ToolbarManager.ToolbarAvailable)
                AddConfigKey(PropId.UseBlizzyToolbarOnly, new ConfigKey("UseBlizzyToolbarOnly", "BLIZZY", "Use Blizzy toolbar only. Takes effect on new scene.", false, false, true, typeof(bool)));
        }

        private void AddConfigKey(PropId id, ConfigKey key)
        {
            keys.Add(key.StringKey.ToUpper(), key);
            alias.Add(key.Alias.ToUpper(), key);
            properties.Add(id, key);
        }

        private void LoadConfig()
        {
            try
            {
                PluginConfiguration config = PluginConfiguration.CreateForType<Config>();
                config.load();

                foreach (ConfigKey key in keys.Values)
                {
                    object value = config[key.StringKey];
                    if (value != null)
                    {
                        key.Value = value;
                        UnityEngine.Debug.Log(string.Format("kOS: Loading Config: {0} Value: {1}", key.StringKey, value));
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.Log("kOS: Exception Loading Config: " + ex.Message);
            }
        }

        public static Config Instance
        {
            get
            {
                return instance ?? (instance = new Config());
            }
        }

        private T GetPropValue<T>(PropId id)
        {
            return (T)properties[id].Value;
        }

        private void SetPropValue(PropId id, object value)
        {
            if (! value.Equals(properties[id].Value))
                TimeStamp = DateTime.Now;
            properties[id].Value = value;
        }
        
        public void SaveConfig()
        {
            PluginConfiguration config = PluginConfiguration.CreateForType<Config>();
            config.load();

            foreach (ConfigKey key in keys.Values)
            {
                SaveConfigKey(key, config);
            }

            config.save();
        }

        private void SaveConfigKey(ConfigKey key)
        {
            PluginConfiguration config = PluginConfiguration.CreateForType<Config>();
            config.load();
            SaveConfigKey(key, config);
            config.save();
        }

        private void SaveConfigKey(ConfigKey key, PluginConfiguration config)
        {
            config.SetValue(key.StringKey, keys[key.StringKey.ToUpper()].Value);
        }

        public override ISuffixResult GetSuffix(string suffixName)
        {
            ConfigKey key = null;

            if (keys.ContainsKey(suffixName))
            {
                key = keys[suffixName];
            }
            else if (alias.ContainsKey(suffixName))
            {
                key = alias[suffixName];
            }

            return key != null ? new SuffixResult(FromPrimitiveWithAssert(key.Value)) : base.GetSuffix(suffixName);
        }

        public override bool SetSuffix(string suffixName, object value)
        {
            ConfigKey key = null;

            if (keys.ContainsKey(suffixName))
            {
                key = keys[suffixName];
            }
            else if (alias.ContainsKey(suffixName))
            {
                key = alias[suffixName];
            }

            if (key == null) return false;

            if (value.GetType() == key.ValType)
            {
                key.Value = value;
                SaveConfigKey(key);
                return true;
            }
            throw new Exception(string.Format("The value of the configuration key '{0}' has to be of type '{1}'", key.Name, key.ValType));
        }
        
        /// <summary>
        /// Return the moment in time when the most recent change to any of the
        /// config values happened.  Used by KOSTollBarWindow to decide whether or not
        /// it needs to assume its cached values are stale and need re-loading.         
        /// </summary>
        public DateTime TimeStamp { get; private set; }

        public IList<ConfigKey> GetConfigKeys()
        {
            return keys.Values.ToList();
        }
                
        public override string ToString()
        {
            return "Use \"list config.\" to view all configurations";
        }

        private enum PropId
        {
            InstructionsPerUpdate = 1,
            UseCompressedPersistence = 2,
            ShowStatistics = 3,
            EnableRTIntegration = 4,
            StartOnArchive = 5,
            EnableSafeMode = 6,
            AudibleExceptions = 7,
            VerboseExceptions = 8,
            EnableTelnet = 9,
            TelnetPort = 10,
            TelnetLoopback = 11,
            UseBlizzyToolbarOnly = 12,
            DebugEachOpcode = 13
        }
    }
}
