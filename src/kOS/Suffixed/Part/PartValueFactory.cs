using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Encapsulation;

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
            var multiModeEngines = part.Modules.GetModules<MultiModeEngine>();
            if (multiModeEngines.Count > 0)
                return new EngineValue(part, multiModeEngines.First(), shared);
            
            var moduleEngines = part.Modules.GetModules<ModuleEngines>();
            if (moduleEngines.Count > 0)
                return new EngineValue(part, new ModuleEngineAdapter(moduleEngines.First()), shared);
            
            var moduleDockingNodes = part.Modules.GetModules<ModuleDockingNode>();
            if (moduleDockingNodes.Count > 0)
                return new DockingPortValue(moduleDockingNodes.First(), shared);
            
            var moduleEnviroSensors = part.Modules.GetModules<ModuleEnviroSensor>();
            if (moduleEnviroSensors.Count > 0)
                return new SensorValue(part, moduleEnviroSensors.First(), shared);

            // Fallback if none of the above: then just a normal part:
            return new PartValue(part, shared);
        }
    }
}
