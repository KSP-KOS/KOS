using kOS.Safe.Encapsulation;
using System.Collections.Generic;
using System.Linq;

namespace kOS.Suffixed.Part
{
    public class PartValueFactory
    {
        public static ListValue Construct(IEnumerable<global::Part> parts, SharedObjects shared)
        {
            var partList = parts.Select(part => Construct(part, shared)).ToList();
            return ListValue.CreateList(partList);
        }

        public static ListValue<PartValue> ConstructGeneric(IEnumerable<global::Part> parts, SharedObjects shared)
        {
            var partList = parts.Select(part => Construct(part, shared)).ToList();
            return ListValue<PartValue>.CreateList(partList);
        }

        public static PartValue Construct(global::Part part, SharedObjects shared)
        {
            PartValue ret = null;
            for (int i = 0; i < part.Modules.Count; ++i)
            {
                if (part.Modules[i] is MultiModeEngine)
                {
                    return new EngineValue(part, (MultiModeEngine)part.Modules[i], shared);
                }
                if (part.Modules[i] is ModuleEngines)
                {
                    ret = new EngineValue(part, new ModuleEngineAdapter((ModuleEngines)part.Modules[i]), shared);
                }
                else if (part.Modules[i] is ModuleDockingNode)
                {
                    ret = new DockingPortValue((ModuleDockingNode)part.Modules[i], shared);
                }
                else if (part.Modules[i] is ModuleEnviroSensor)
                {
                    ret = new SensorValue(part, (ModuleEnviroSensor)part.Modules[i], shared);
                }
            }
            if (ret != null)
                return ret;

            // Fallback if none of the above: then just a normal part:
            return new PartValue(part, shared);
        }
    }
}