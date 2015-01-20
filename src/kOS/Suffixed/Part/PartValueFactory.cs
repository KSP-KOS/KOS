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
            foreach (PartModule module in part.Modules)
            {
                ModuleEngines mEng = module as ModuleEngines;
                if (mEng != null)
                    return new EngineValue(part, new ModuleEngineAdapter(mEng), shared);
                ModuleEnginesFX mEngFX = module as ModuleEnginesFX;
                if (mEngFX != null)
                    return new EngineValue(part, new ModuleEngineAdapter(mEngFX), shared);
                ModuleDockingNode mDock = module as ModuleDockingNode;
                if (mDock != null)
                    return new DockingPortValue(mDock, shared);
                ModuleEnviroSensor mSense = module as ModuleEnviroSensor;
                if (mSense != null)
                    return new SensorValue(part, mSense, shared);
                var gimbalModule  = module as ModuleGimbal;
                if (gimbalModule != null)
                    return new GimbalValue(gimbalModule,shared);
                
            }
            
            // Fallback if none of the above: then just a normal part:
            return new PartValue(part, shared);
        }
    }
}
