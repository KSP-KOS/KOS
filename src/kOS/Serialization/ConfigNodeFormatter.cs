using System;
using kOS.Safe.Serialization;
using System.Collections.Generic;
using kOS.Safe;
using kOS.Safe.Utilities;

namespace kOS.Serialization
{
    public class ConfigNodeFormatter : IFormatWriter, IFormatReader
    {
        private const string ParentNode = "";
        private static readonly ConfigNodeFormatter instance = new ConfigNodeFormatter();

        public static ConfigNodeFormatter Instance
        {
            get 
            {
                return instance; 
            }
        }

        private ConfigNodeFormatter()
        {
        }

        public ConfigNode ToConfigNode(Dump value)
        {
            ConfigNode configNode = new ConfigNode();

            foreach (KeyValuePair<object, object> entry in value)
            {
                string key = entry.Key.ToString();

                if (entry.Value is Dictionary<object, object>)
                {
                    ConfigNode subNode = ToConfigNode(entry.Value as Dump);

                    subNode.name = key;

                    configNode.AddNode(subNode);
                } else
                {
                    configNode.AddValue(key, entry.Value);
                }
            }

            return configNode;
        }

        public Dump FromConfigNode(ConfigNode configNode)
        {
            Dump result = new Dump();

            foreach (ConfigNode.Value val in configNode.values)
            {
                result[val.name] = val.value;
            }

            foreach (ConfigNode subNode in configNode.GetNodes())
            {
                result[subNode.name] = FromConfigNode(subNode);
            }

            return result;
        }

        public string Write(Dump value)
        {
            ConfigNode configNode = ToConfigNode(value);
            configNode.name = ParentNode;
            return configNode.ToString();
        }

        public Dump Read(string input)
        {
            ConfigNode configNode = ConfigNode.Parse(input);

            return FromConfigNode(configNode.GetNode(ParentNode));
        }
    }
}

