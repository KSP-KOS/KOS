using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using KSP.IO;
using kOS.Module;

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("Config")]
    public class Config : Structure, IConfig
    {
        private static Config instance;
        private readonly Dictionary<string, ConfigKey> keys;
        private readonly Dictionary<string, ConfigKey> alias;
        private readonly Dictionary<PropId, ConfigKey> properties;

        public int InstructionsPerUpdate { get { return kOSCustomParameters.Instance.InstructionsPerUpdate; } set { kOSCustomParameters.Instance.InstructionsPerUpdate = value; } }
        public int LuaInstructionsPerUpdate { get { return kOSCustomParameters.Instance.LuaInstructionsPerUpdate; } set { kOSCustomParameters.Instance.LuaInstructionsPerUpdate = value; } }
        public bool UseCompressedPersistence { get { return kOSCustomParameters.Instance.useCompressedPersistence; } set { kOSCustomParameters.Instance.useCompressedPersistence = value; } }
        public bool ShowStatistics { get { return kOSCustomParameters.Instance.showStatistics; } set { kOSCustomParameters.Instance.showStatistics = value; } }
        public bool StartOnArchive { get { return kOSCustomParameters.Instance.startOnArchive; } set { kOSCustomParameters.Instance.startOnArchive = value; } }
        public bool ObeyHideUI { get { return kOSCustomParameters.Instance.obeyHideUi; } set { kOSCustomParameters.Instance.obeyHideUi = value; } }
        public bool EnableSafeMode { get { return kOSCustomParameters.Instance.enableSafeMode; } set { kOSCustomParameters.Instance.enableSafeMode = value; } }
        public bool AudibleExceptions { get { return kOSCustomParameters.Instance.audibleExceptions; } set { kOSCustomParameters.Instance.audibleExceptions = value; } }
        public bool VerboseExceptions { get { return kOSCustomParameters.Instance.verboseExceptions; } set { kOSCustomParameters.Instance.verboseExceptions = value; } }
        public bool AllowClobberBuiltIns { get { return kOSCustomParameters.Instance.clobberBuiltIns; } set { kOSCustomParameters.Instance.clobberBuiltIns = value; } }
        public bool EnableTelnet { get { return GetPropValue<bool>(PropId.EnableTelnet); } set { SetPropValue(PropId.EnableTelnet, value); } }
        public int TelnetPort { get { return GetPropValue<int>(PropId.TelnetPort); } set { SetPropValue(PropId.TelnetPort, value); } }
        public string TelnetIPAddrString { get { return GetPropValue<string>(PropId.TelnetIPAddrString); } set { SetPropValue(PropId.TelnetIPAddrString, value); } }        
        public int TerminalFontDefaultSize {get { return GetPropValue<int>(PropId.TerminalFontDefaultSize); } set { SetPropValue(PropId.TerminalFontDefaultSize, value); } }
        public string TerminalFontName {get { return GetPropValue<string>(PropId.TerminalFontName); } set { SetPropValue(PropId.TerminalFontName, value); } }
        public int TerminalDefaultWidth { get { return GetPropValue<int>(PropId.TerminalDefaultWidth); } set { SetPropValue(PropId.TerminalDefaultWidth, value); } }
        public int TerminalDefaultHeight { get { return GetPropValue<int>(PropId.TerminalDefaultHeight); } set { SetPropValue(PropId.TerminalDefaultHeight, value); } }
        public bool UseBlizzyToolbarOnly { get { return kOSCustomParameters.Instance.useBlizzyToolbarOnly; } set { kOSCustomParameters.Instance.useBlizzyToolbarOnly = value; } }
        public bool DebugEachOpcode { get { return kOSCustomParameters.Instance.debugEachOpcode; } set { kOSCustomParameters.Instance.debugEachOpcode = value; } }
        public bool SuppressAutopilot { get { return GetPropValue<bool>(PropId.SuppressAutopilot); } set { SetPropValue(PropId.SuppressAutopilot, value); } }

        // NOTE TO FUTURE MAINTAINERS:  If it looks like overkill to use a double instead of a float for this next field, you're right.
        // But KSP seems to have a bug where single-precision floats don't get saved in the config XML file.  Doubles seem to work, though.
        public double TerminalBrightness {get { return GetPropValue<double>(PropId.TerminalBrightness); } set { SetPropValue(PropId.TerminalBrightness, value); } }

        private Config()
        {
            keys = new Dictionary<string, ConfigKey>(StringComparer.OrdinalIgnoreCase);
            alias = new Dictionary<string, ConfigKey>(StringComparer.OrdinalIgnoreCase);
            properties = new Dictionary<PropId, ConfigKey>();
            InitializeSuffixes();
            BuildValuesDictionary();
            LoadConfig();
            TimeStamp = DateTime.Now;
        }

        private void InitializeSuffixes()
        {
            AddSuffix("IPU", new SetSuffix<ScalarValue>(() => InstructionsPerUpdate, value => InstructionsPerUpdate = value));
            AddSuffix("LUAIPU", new SetSuffix<ScalarValue>(() => LuaInstructionsPerUpdate, value => LuaInstructionsPerUpdate = value));
            AddSuffix("UCP", new SetSuffix<BooleanValue>(() => UseCompressedPersistence, value => UseCompressedPersistence = value));
            AddSuffix("STAT", new SetSuffix<BooleanValue>(() => ShowStatistics, value => ShowStatistics = value));
            AddSuffix("ARCH", new SetSuffix<BooleanValue>(() => StartOnArchive, value => StartOnArchive = value));
            AddSuffix("OBEYHIDEUI", new SetSuffix<BooleanValue>(() => ObeyHideUI, value => ObeyHideUI = value));
            AddSuffix("SAFE", new SetSuffix<BooleanValue>(() => EnableSafeMode, value => EnableSafeMode = value));
            AddSuffix("AUDIOERR", new SetSuffix<BooleanValue>(() => AudibleExceptions, value => AudibleExceptions = value));
            AddSuffix("VERBOSE", new SetSuffix<BooleanValue>(() => VerboseExceptions, value => VerboseExceptions = value));
            AddSuffix("CLOBBERBUILTINS", new SetSuffix<BooleanValue>(() => AllowClobberBuiltIns, value => AllowClobberBuiltIns = value));
            AddSuffix("DEBUGEACHOPCODE", new SetSuffix<BooleanValue>(() => DebugEachOpcode, value => DebugEachOpcode = value));
            AddSuffix("BLIZZY", new SetSuffix<BooleanValue>(() => UseBlizzyToolbarOnly, value => UseBlizzyToolbarOnly = value));
            AddSuffix("BRIGHTNESS", new ClampSetSuffix<ScalarValue>(() => TerminalBrightness, value => TerminalBrightness = value, 0f, 1f, 0.01f));
            AddSuffix("DEFAULTFONTSIZE", new ClampSetSuffix<ScalarValue>(() => TerminalFontDefaultSize, value => TerminalFontDefaultSize = value, TerminalStruct.MINCHARPIXELS, TerminalStruct.MAXCHARPIXELS, 1f));
            AddSuffix("DEFAULTWIDTH", new ClampSetSuffix<ScalarValue>(() => TerminalDefaultWidth, value => TerminalDefaultWidth = value, 15f, 255f, 1f));
            AddSuffix("DEFAULTHEIGHT", new ClampSetSuffix<ScalarValue>(() => TerminalDefaultHeight, value => TerminalDefaultHeight = value, 3f, 160f, 1f));
            AddSuffix("SUPPRESSAUTOPILOT", new SetSuffix<BooleanValue>(() => SuppressAutopilot, value => SuppressAutopilot = value));
        }

        private void BuildValuesDictionary()
        {
            AddConfigKey(PropId.EnableTelnet,
                new ConfigKey("EnableTelnet", "TELNET", "Enable Telnet server", false, false, true, typeof(bool)));
            AddConfigKey(PropId.TelnetPort,
                new ConfigKey("TelnetPort", "TPORT", "Telnet port number (must restart telnet to take effect)", 5410, 1024, 65535, typeof(int)));
            AddConfigKey(PropId.TelnetIPAddrString,
                new ConfigKey("TelnetIPAddrString", "IPADDRESS", "Telnet IP address string (must restart telnet to take effect)", "127.0.0.1", "n/a", "n/a", typeof(string)));
            AddConfigKey(PropId.TerminalFontDefaultSize,
                new ConfigKey("TerminalFontDefaultSize", "DEFAULTFONTSIZE", "Initial Terminal:CHARHEIGHT when a terminal is first opened",
                        12, TerminalStruct.MINCHARPIXELS, TerminalStruct.MAXCHARPIXELS, typeof(int)));
            AddConfigKey(PropId.TerminalFontName,
                new ConfigKey("TerminalFontName", "FONTNAME", "Font Name for terminal window", "_not_chosen_yet_", "n/a", "n/a", typeof(string)));
            AddConfigKey(PropId.TerminalBrightness,
                new ConfigKey("TerminalBrightness", "BRIGHTNESS", "Initial brightness setting for new terminals", 0.7d, 0d, 1d, typeof(double)));
            AddConfigKey(PropId.TerminalDefaultWidth,
                new ConfigKey("TerminalDefaultWidth", "DEFAULTWIDTH", "Initial Terminal:WIDTH when a terminal is first opened", 50, 15, 255, typeof(int)));
            AddConfigKey(PropId.TerminalDefaultHeight,
                new ConfigKey("TerminalDefaultHeight", "DEFAULTHEIGHT", "Initial Terminal:HEIGHT when a terminal is first opened", 36, 3, 160, typeof(int)));
            AddConfigKey(PropId.SuppressAutopilot,
                new ConfigKey("SuppressAutopilot", "SUPPRESSAUTOPILOT", "Suppress all kOS autopiloting for emergency manual control", false, false, true, typeof(bool)));
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

        public override ISuffixResult GetSuffix(string suffixName, bool failOkay = false)
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

            return key != null ? new SuffixResult(FromPrimitiveWithAssert(key.Value)) : base.GetSuffix(suffixName, failOkay);
        }

        /// <summary>
        /// same as Structure.SetSuffix, but it has the extra logic to alter the config keys
        /// that the game auto-saves every so often.
        /// </summary>
        /// <param name="suffixName"></param>
        /// <param name="value"></param>
        /// <param name="failOkay"></param>
        /// <returns></returns>
        public override bool SetSuffix(string suffixName, object value, bool failOkay = false)
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

            if (key == null) return base.SetSuffix(suffixName, value, failOkay);

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
            LuaInstructionsPerUpdate = 2,
            UseCompressedPersistence = 3,
            ShowStatistics = 4,
            EnableRTIntegration = 5,
            StartOnArchive = 6,
            ObeyHideUI = 7,
            EnableSafeMode = 8,
            AudibleExceptions = 9,
            VerboseExceptions = 10,
            EnableTelnet = 11,
            TelnetPort = 12,
            TelnetIPAddrString = 13,
            UseBlizzyToolbarOnly = 14,
            DebugEachOpcode = 15,
            TerminalFontDefaultSize = 16,
            TerminalFontName = 17,
            TerminalBrightness = 18,
            TerminalDefaultWidth = 19,
            TerminalDefaultHeight = 20,
            SuppressAutopilot = 21
        }
    }
}
