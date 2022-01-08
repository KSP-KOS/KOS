using System;
using kOS.Safe.Serialization;
using System.Collections.Generic;
using kOS.Safe;
using kOS.Safe.Utilities;
using System.Linq;
using System.Collections;
using kOS.Safe.Encapsulation;
using kOS.Safe.Persistence;

namespace kOS.Serialization
{
    public class ConfigNodeFormatter
    {
        private const string ParentNode = "";
        private const string ListKey = "$list";
        private const string BooleanKey = "$boolean";
        private const string ScalarKey = "$scalar";
        private const string ValueKey = "$value";

        public static ConfigNode ToConfigNode(JsonObject dump)
        {
            return ToConfigNode(ParentNode, dump);
        }

        private static ConfigNode ToConfigNode(string name, List<object> list)
        {
            ConfigNode configNode = new ConfigNode();
            configNode.name = name;

            // ConfigNode doesn't handle arrays so we wrap everything in an extra node
            // and give it a special name to mark this data set as an array
            ConfigNode innerNode = new ConfigNode();
            innerNode.name = ListKey;

            var indexedList = list.Select((value, index) => new { Value = value, Index = index.ToString() });

            foreach (var item in indexedList)
            {
                HandleValue(innerNode, item.Index, item.Value);
            }

            configNode.AddNode(innerNode);

            return configNode;
        }

        private static ConfigNode ScalarToConfigNode(string name, string type, string value)
        {
            ConfigNode configNode = new ConfigNode();
            configNode.name = name;

            // ConfigNode stores all values as strings so we have to store non-string values as nodes, otherwise
            // the type information would be lost
            ConfigNode innerNode = new ConfigNode();
            innerNode.name = type;

            innerNode.AddValue(ValueKey, value);

            configNode.AddNode(innerNode);

            return configNode;
        }

        private static ConfigNode ToConfigNode(string name, JsonObject dump)
        {
            ConfigNode configNode = new ConfigNode();

            configNode.name = name;

            foreach (var entry in dump)
            {
                HandleValue(configNode, entry.Key.ToString(), entry.Value);
            }

            return configNode;
        }

        private static void HandleValue(ConfigNode configNode, string key, object value)
        {
            if (value is JsonObject)
            {
                configNode.AddNode(key, ToConfigNode(key, value as JsonObject));
            } else if (value is List<object>)
            {
                configNode.AddNode(key, ToConfigNode(key, value as List<object>));
            } else if (value is int)
            {
                configNode.AddNode(key, ScalarToConfigNode(key, ScalarKey, Convert.ToString(value)));
            } else if (value is double)
            {
                configNode.AddNode(key, ScalarToConfigNode(key, ScalarKey, ((double)value).ToString("R")));

            } else if (value is bool)
            {
                configNode.AddNode(key, ScalarToConfigNode(key, BooleanKey, Convert.ToString(value)));
            } else
            {
                configNode.AddValue(key, PersistenceUtilities.EncodeLine(value.ToString()));
            }
        }

        private static JsonArray ListFromConfigNode(ConfigNode configNode)
        {
            JsonArray result = new JsonArray();

            bool hasMoreValues = true;

            for (int i = 0; hasMoreValues; i++)
            {
                if (configNode.HasValue(i.ToString()))
                {
                    result.Add(PersistenceUtilities.DecodeLine(configNode.GetValue(i.ToString())));
                } else if (configNode.HasNode(i.ToString()))
                {
                    result.Add(ObjectFromConfigNode(configNode.GetNode(i.ToString())));
                } else
                {
                    hasMoreValues = false;
                }
            }

            return result;
        }

        private static object ObjectFromConfigNode(ConfigNode configNode)
        {
            if (configNode.HasNode(ListKey))
            {
                return ListFromConfigNode(configNode.GetNode(ListKey));
            }

            if (configNode.HasNode(ScalarKey))
            {
                return Structure.FromPrimitiveWithAssert(Convert.ToDouble(configNode.GetNode(ScalarKey).GetValue(ValueKey)));
            }

            if (configNode.HasNode(BooleanKey))
            {
                return Structure.FromPrimitiveWithAssert(Convert.ToBoolean(configNode.GetNode(BooleanKey).GetValue(ValueKey)));
            }

            return FromConfigNode(configNode);
        }

        public static JsonObject FromConfigNode(ConfigNode configNode)
        {
            JsonObject result = new JsonObject();

            foreach (ConfigNode.Value val in configNode.values)
            {
                result[val.name] = PersistenceUtilities.DecodeLine(val.value);
            }

            foreach (ConfigNode subNode in configNode.GetNodes())
            {
                result[subNode.name] = ObjectFromConfigNode(subNode);
            }

            return result;
        }

        //public static string Write(Dump value)
        //{
        //    return ToConfigNode(value).ToString();
        //}

        //public static Dump Read(string input)
        //{
        //    ConfigNode configNode = ConfigNode.Parse(input);

        //    return Dump.FromJson(FromConfigNode(configNode.GetNode(ParentNode)));
        //}
    }
}

