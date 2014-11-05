/*
 * Created by SharpDevelop.
 * User: Dunbaratu
 * Date: 11/5/2014
 * Time: 3:13 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Linq;

namespace kOS.Suffixed.Part
{
    /// <summary>
    /// Description of PartFactory.
    /// </summary>
    public class PartFactory 
    {        
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
                
            }
            
            // Fallback if none of the above: then just a normal part:
            return new PartValue(part, shared);
        }
    }
}
