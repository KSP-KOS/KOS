using System;
using System.Collections.Generic;
using System.Linq;
using KSP.IO;
using kOS.Safe.Encapsulation;

namespace kOS.Suffixed
{
    public class Config : Structure
    {
        private static Config instance;
        private readonly Dictionary<string, ConfigKey> keys;
        private readonly Dictionary<string, ConfigKey> alias;
        private readonly Dictionary<PropId, ConfigKey> properties;

        public int InstructionsPerUpdate { get { return GetPropValue<int>(PropId.InstructionsPerUpdate); } set { SetPropValue(PropId.InstructionsPerUpdate, value); } }
        public bool UseCompressedPersistence { get { return GetPropValue<bool>(PropId.UseCompressedPersistence); } set { SetPropValue(PropId.UseCompressedPersistence, value); } }
        public bool ShowStatistics { get { return GetPropValue<bool>(PropId.ShowStatistics); } set { SetPropValue(PropId.ShowStatistics, value); } }
        public bool EnableRT2Integration { get { return GetPropValue<bool>(PropId.EnableRT2Integration); } set { SetPropValue(PropId.EnableRT2Integration, value); } }
        public bool StartOnArchive { get { return GetPropValue<bool>(PropId.StartOnArchive); } set { SetPropValue(PropId.StartOnArchive, value); } }
        public bool EnableSafeMode { get { return GetPropValue<bool>(PropId.EnableSafeMode); } set { SetPropValue(PropId.EnableSafeMode, value); } }
        
        private Config()
        {
            keys = new Dictionary<string, ConfigKey>();
            alias = new Dictionary<string, ConfigKey>();
            properties = new Dictionary<PropId, ConfigKey>();
            BuildValuesDictionary();
            LoadConfig();
        }

        private void BuildValuesDictionary()
        {
            AddConfigKey(PropId.InstructionsPerUpdate, new ConfigKey("InstructionsPerUpdate", "IPU", "Instructions per update", 150, typeof(int)));
            AddConfigKey(PropId.UseCompressedPersistence, new ConfigKey("UseCompressedPersistence", "UCP", "Use compressed persistence", false, typeof(bool)));
            AddConfigKey(PropId.ShowStatistics, new ConfigKey("ShowStatistics", "STAT", "Show execution statistics", false, typeof(bool)));
            AddConfigKey(PropId.EnableRT2Integration, new ConfigKey("EnableRT2Integration", "RT2", "Enable RT2 integration", false, typeof(bool)));
            AddConfigKey(PropId.StartOnArchive, new ConfigKey("StartOnArchive", "ARCH", "Start on Archive volume", false, typeof(bool)));
            AddConfigKey(PropId.EnableSafeMode, new ConfigKey("EnableSafeMode", "SAFE", "Enable safe mode", true, typeof(bool)));
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
                        UnityEngine.Debug.LogError(string.Format("kOS: Loading Config: {0} Value: {1}", key.StringKey, value));
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("kOS: Exception Loading Config: " + ex.Message);
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

            if (value.GetType() == key.Type)
            {
                key.Value = value;
                SaveConfigKey(key);
                return true;
            }
            throw new Exception(string.Format("The value of the configuration key '{0}' has to be of type '{1}'", key.Name, key.Type));
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
            EnableSafeMode = 6
        }
    }

    public class ConfigKey
    {
        public string StringKey;
        public string Alias;
        public string Name;
        public Type Type;
        public object Value;

        public ConfigKey(string stringKey, string alias, string name, object defaultValue, Type type)
        {
            StringKey = stringKey;
            Alias = alias;
            Name = name;
            Value = defaultValue;
            Type = type;
        }
    }
}
