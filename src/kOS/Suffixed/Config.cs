﻿using System;
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
        public bool UseCompressedPersistence { get { return kOSCustomParameters.Instance.useCompressedPersistence; } set { kOSCustomParameters.Instance.useCompressedPersistence = value; } }
        public bool ShowStatistics { get { return kOSCustomParameters.Instance.showStatistics; } set { kOSCustomParameters.Instance.showStatistics = value; } }
        public bool StartOnArchive { get { return kOSCustomParameters.Instance.startOnArchive; } set { kOSCustomParameters.Instance.startOnArchive = value; } }
        public bool ObeyHideUI { get { return kOSCustomParameters.Instance.obeyHideUi; } set { kOSCustomParameters.Instance.obeyHideUi = value; } }
        public bool EnableSafeMode { get { return kOSCustomParameters.Instance.enableSafeMode; } set { kOSCustomParameters.Instance.enableSafeMode = value; } }
        public bool AudibleExceptions { get { return kOSCustomParameters.Instance.audibleExceptions; } set { kOSCustomParameters.Instance.audibleExceptions = value; } }
        public bool VerboseExceptions { get { return kOSCustomParameters.Instance.verboseExceptions; } set { kOSCustomParameters.Instance.verboseExceptions = value; } }
        public bool EnableTelnet { get { return GetPropValue<bool>(PropId.EnableTelnet); } set { SetPropValue(PropId.EnableTelnet, value); } }
        public int TelnetPort { get { return GetPropValue<int>(PropId.TelnetPort); } set { SetPropValue(PropId.TelnetPort, value); } }
        public bool TelnetLoopback { get { return GetPropValue<bool>(PropId.TelnetLoopback); } set { SetPropValue(PropId.TelnetLoopback, value); } }        
        public int TerminalFontDefaultSize {get { return GetPropValue<int>(PropId.TerminalFontDefaultSize); } set { SetPropValue(PropId.TerminalFontDefaultSize, value); } }
        public string TerminalFontName {get { return GetPropValue<string>(PropId.TerminalFontName); } set { SetPropValue(PropId.TerminalFontName, value); } }
        public bool UseBlizzyToolbarOnly { get { return kOSCustomParameters.Instance.useBlizzyToolbarOnly; } set { kOSCustomParameters.Instance.useBlizzyToolbarOnly = value; } }
        public bool DebugEachOpcode { get { return kOSCustomParameters.Instance.debugEachOpcode; } set { kOSCustomParameters.Instance.debugEachOpcode = value; } }

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
            AddSuffix("UCP", new SetSuffix<BooleanValue>(() => UseCompressedPersistence, value => UseCompressedPersistence = value));
            AddSuffix("STAT", new SetSuffix<BooleanValue>(() => ShowStatistics, value => ShowStatistics = value));
            AddSuffix("ARCH", new SetSuffix<BooleanValue>(() => StartOnArchive, value => StartOnArchive = value));
            AddSuffix("OBEYHIDEUI", new SetSuffix<BooleanValue>(() => ObeyHideUI, value => ObeyHideUI = value));
            AddSuffix("SAFE", new SetSuffix<BooleanValue>(() => EnableSafeMode, value => EnableSafeMode = value));
            AddSuffix("AUDIOERR", new SetSuffix<BooleanValue>(() => AudibleExceptions, value => AudibleExceptions = value));
            AddSuffix("VERBOSE", new SetSuffix<BooleanValue>(() => VerboseExceptions, value => VerboseExceptions = value));
            AddSuffix("DEBUGEACHOPCODE", new SetSuffix<BooleanValue>(() => DebugEachOpcode, value => DebugEachOpcode = value));
            AddSuffix("BLIZZY", new SetSuffix<BooleanValue>(() => UseBlizzyToolbarOnly, value => UseBlizzyToolbarOnly = value));
            AddSuffix("BRIGHTNESS", new ClampSetSuffix<ScalarValue>(() => TerminalBrightness, value => TerminalBrightness = value, 0f, 1f, 0.01f));
            AddSuffix("DEFAULTFONTSIZE", new ClampSetSuffix<ScalarValue>(() => TerminalFontDefaultSize, value => TerminalFontDefaultSize = value, 6f, 30f, 1f));
        }

        private void BuildValuesDictionary()
        {
            AddConfigKey(PropId.EnableTelnet, new ConfigKey("EnableTelnet", "TELNET", "Enable Telnet server", false, false, true, typeof(bool)));
            AddConfigKey(PropId.TelnetPort, new ConfigKey("TelnetPort", "TPORT", "Telnet port number (must restart telnet to take effect)", 5410, 1024, 65535, typeof(int)));
            AddConfigKey(PropId.TelnetLoopback, new ConfigKey("TelnetLoopback", "LOOPBACK", "Restricts telnet to 127.0.0.1 (must restart telnet to take effect)", true, false, true, typeof(bool)));
            AddConfigKey(PropId.TerminalFontDefaultSize, new ConfigKey("TerminalFontDefaultSize", "DEFAULTFONTSIZE", "Initial Terminal:CHARHEIGHT when a terminal is first opened", 12, 6, 20, typeof(int)));
            AddConfigKey(PropId.TerminalFontName, new ConfigKey("TerminalFontName", "FONTNAME", "Font Name for terminal window", "Courier New Bold", "n/a", "n/a", typeof(string)));
            AddConfigKey(PropId.TerminalBrightness, new ConfigKey("TerminalBrightness", "BRIGHTNESS", "Initial brightness setting for new terminals", 0.7d, 0d, 1d, typeof(double)));
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

            if (key == null) return base.SetSuffix(suffixName, value);

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
            ObeyHideUI = 6,
            EnableSafeMode = 7,
            AudibleExceptions = 8,
            VerboseExceptions = 9,
            EnableTelnet = 10,
            TelnetPort = 11,
            TelnetLoopback = 12,
            UseBlizzyToolbarOnly = 13,
            DebugEachOpcode = 14,
            TerminalFontDefaultSize = 15,
            TerminalFontName = 16,
            TerminalBrightness = 17
        }
    }
}
