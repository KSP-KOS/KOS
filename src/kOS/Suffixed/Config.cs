using System;
using System.Collections.Generic;
using System.Linq;
using KSP.IO;
using kOS.Safe.Encapsulation;

namespace kOS.Suffixed
{
    public class Config : Structure, IConfig
    {
        private static Config instance;
        private readonly Dictionary<string, ConfigKey> keys;
        private readonly Dictionary<string, ConfigKey> alias;
        private readonly Dictionary<PropId, ConfigKey> properties;
        private DateTime lastChangeTime;

        public int InstructionsPerUpdate { get { return GetPropValue<int>(PropId.InstructionsPerUpdate); } set { SetPropValue(PropId.InstructionsPerUpdate, value); } }
        public bool UseCompressedPersistence { get { return GetPropValue<bool>(PropId.UseCompressedPersistence); } set { SetPropValue(PropId.UseCompressedPersistence, value); } }
        public bool ShowStatistics { get { return GetPropValue<bool>(PropId.ShowStatistics); } set { SetPropValue(PropId.ShowStatistics, value); } }
        public bool EnableRT2Integration { get { return GetPropValue<bool>(PropId.EnableRT2Integration); } set { SetPropValue(PropId.EnableRT2Integration, value); } }
        public bool StartOnArchive { get { return GetPropValue<bool>(PropId.StartOnArchive); } set { SetPropValue(PropId.StartOnArchive, value); } }
        public bool EnableSafeMode { get { return GetPropValue<bool>(PropId.EnableSafeMode); } set { SetPropValue(PropId.EnableSafeMode, value); } }
        public bool VerboseExceptions { get { return GetPropValue<bool>(PropId.VerboseExceptions); } set { SetPropValue(PropId.VerboseExceptions, value); } }
        public bool EnableTelnet { get { return GetPropValue<bool>(PropId.EnableTelnet); } set { SetPropValue(PropId.EnableTelnet, value); } }
        public int TelnetPort { get { return GetPropValue<int>(PropId.TelnetPort); } set { SetPropValue(PropId.TelnetPort, value); } }
        public bool TelnetLoopback { get { return GetPropValue<bool>(PropId.TelnetLoopback); } set { SetPropValue(PropId.TelnetLoopback, value); } }
        
        private Config()
        {
            keys = new Dictionary<string, ConfigKey>();
            alias = new Dictionary<string, ConfigKey>();
            properties = new Dictionary<PropId, ConfigKey>();
            BuildValuesDictionary();
            LoadConfig();
            lastChangeTime = DateTime.Now;
        }

        private void BuildValuesDictionary()
        {
            AddConfigKey(PropId.InstructionsPerUpdate, new ConfigKey("InstructionsPerUpdate", "IPU", "Instructions per update", 150, 50, 2000, typeof(int)));
            AddConfigKey(PropId.UseCompressedPersistence, new ConfigKey("UseCompressedPersistence", "UCP", "Use compressed persistence", false, false, true, typeof(bool)));
            AddConfigKey(PropId.ShowStatistics, new ConfigKey("ShowStatistics", "STAT", "Show execution statistics", false, false, true, typeof(bool)));
            AddConfigKey(PropId.EnableRT2Integration, new ConfigKey("EnableRT2Integration", "RT2", "Enable RT2 integration", false, false, true, typeof(bool)));
            AddConfigKey(PropId.StartOnArchive, new ConfigKey("StartOnArchive", "ARCH", "Start on Archive volume", false, false, true, typeof(bool)));
            AddConfigKey(PropId.EnableSafeMode, new ConfigKey("EnableSafeMode", "SAFE", "Enable safe mode", true, false, true, typeof(bool)));
            AddConfigKey(PropId.VerboseExceptions, new ConfigKey("VerboseExceptions", "VERBOSE", "Enable verbose exception msgs", true, false, true, typeof(bool)));
            AddConfigKey(PropId.EnableTelnet, new ConfigKey("EnableTelnet", "TELNET", "Enable Telnet server", false, false, true, typeof(bool)));
            AddConfigKey(PropId.TelnetPort, new ConfigKey("TelnetPort", "TPORT", "Telnet port number (must restart telnet to take effect)", 5410, 1024, 65535, typeof(int)));
            AddConfigKey(PropId.TelnetLoopback, new ConfigKey("TelnetLoopback", "LOOPBACK", "Restricts telnet to 127.0.0.1 (must restart telnet to take effect)", true, false, true, typeof(bool)));
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
                var config = PluginConfiguration.CreateForType<Config>();
                config.load();

                foreach (var key in keys.Values)
                {
                    var value = config[key.StringKey];
                    if (value != null)
                    {
                        key.Value = value;
                        Safe.Utilities.Debug.Logger.Log(string.Format("kOS: Loading Config: {0} Value: {1}", key.StringKey, value));
                    }
                }
            }
            catch (Exception ex)
            {
                Safe.Utilities.Debug.Logger.Log("kOS: Exception Loading Config: " + ex.Message);
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
                lastChangeTime = DateTime.Now;
            properties[id].Value = value;
        }
        
        public void SaveConfig()
        {
            var config = PluginConfiguration.CreateForType<Config>();
            config.load();

            foreach (var key in keys.Values)
            {
                SaveConfigKey(key, config);
            }

            config.save();
        }

        private void SaveConfigKey(ConfigKey key)
        {
            var config = PluginConfiguration.CreateForType<Config>();
            config.load();
            SaveConfigKey(key, config);
            config.save();
        }

        private void SaveConfigKey(ConfigKey key, PluginConfiguration config)
        {
            config.SetValue(key.StringKey, keys[key.StringKey.ToUpper()].Value);
        }

        public override object GetSuffix(String suffixName)
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

            return key != null ? key.Value : base.GetSuffix(suffixName);
        }

        public override bool SetSuffix(String suffixName, object value)
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
        public DateTime TimeStamp()
        {
            return lastChangeTime;
        }

        public List<ConfigKey> GetConfigKeys()
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
            EnableRT2Integration = 4,
            StartOnArchive = 5,
            EnableSafeMode = 6,
            VerboseExceptions = 7,
            EnableTelnet = 8,
            TelnetPort = 9,
            TelnetLoopback = 10
        }
    }

    public class ConfigKey
    {
        private object val;
        public string StringKey {get;private set;}
        public string Alias {get;set;}
        public string Name {get;set;}
        public Type ValType {get;set;}
        public object Value {get{return val;} set{ val = SafeSetValue(value); } }
        public object MinValue {get;set;}
        public object MaxValue {get;set;}

        public ConfigKey(string stringKey, string alias, string name, object defaultValue, object min, object max, Type type)
        {
            StringKey = stringKey;
            Alias = alias;
            Name = name;
            val = defaultValue;
            MinValue = min;
            MaxValue = max;
            ValType = type;
        }
        
        /// <summary>
        /// Return the new value after it's been altered or the change was denied.
        /// </summary>
        /// <param name="newValue">attempted new value</param>
        /// <returns>new value to actually use, maybe constrained or even unchanged if the attempted value is disallowed</returns>
        private object SafeSetValue(object newValue)
        {
            object returnValue = Value;
            if (newValue==null || (! ValType.IsInstanceOfType(newValue)))
                return returnValue;

            if (Value is int)
            {
                if ((int)newValue < (int)MinValue)
                    returnValue = MinValue;
                else if ((int)newValue > (int)MaxValue)
                    returnValue = MaxValue;
                else
                    returnValue = newValue;
                
                // TODO: If and when we end up making warning-level exceptions that don't break
                // the execution but still get logged, then log such a warning here mentioning
                // if the value attempted was denied and changed if it was.
            }
            else if (Value is bool)
            {
                returnValue = newValue;
            }
            else
            {
                throw new Exception( "kOS CONFIG has new type that wasn't supported yet:  contact kOS developers" );
            }
            return returnValue;
        }
    }
}
