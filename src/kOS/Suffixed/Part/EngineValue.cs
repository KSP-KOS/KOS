using System;
using System.Collections.Generic;
using kOS.Safe.Encapsulation;

namespace kOS.Suffixed.Part
{
    public class EngineValue : PartValue
    {
        private readonly ModuleEnginesFX enginefFx;
        private readonly ModuleEngines engine;

        public EngineValue(global::Part part, ModuleEngines engine, SharedObjects sharedObj) : base(part, sharedObj)
        {
            this.engine = engine;
        }

        public EngineValue(global::Part part, ModuleEnginesFX enginefFx, SharedObjects sharedObj) : base(part, sharedObj)
        {
            this.enginefFx = enginefFx;
        }

        public override bool SetSuffix(string suffixName, object value)
        {
            if (engine != null)
            {
                return SetEngineSuffix(suffixName, value, engine);
            }
            if (enginefFx != null)
            {
                return SetEngineFxSuffix(suffixName, value, enginefFx);
            }
            return base.SetSuffix(suffixName, value);
        }

        private bool SetEngineFxSuffix(string suffixName, object value, ModuleEnginesFX moduleEnginesFx)
        {
            switch (suffixName)
            {
                case "ACTIVE":
                    var activate = Convert.ToBoolean(value);
                    if (activate)
                    {
                        moduleEnginesFx.Activate();
                    }
                    else
                    {
                        moduleEnginesFx.Shutdown();
                    }
                    return true;
                case "THRUSTLIMIT":
                    var throttlePercent = (float)Convert.ToDouble(value);
                    moduleEnginesFx.thrustPercentage = throttlePercent;
                    return true;
            }
            return base.SetSuffix(suffixName, value);
        }

        private bool SetEngineSuffix(string suffixName, object value, ModuleEngines moduleEngines)
        {
            switch (suffixName)
            {
                case "ACTIVE":
                    var activate = Convert.ToBoolean(value);
                    if (activate)
                    {
                        moduleEngines.Activate();
                    }
                    else
                    {
                        moduleEngines.Shutdown();
                    }
                    return true;
                case "THRUSTLIMIT":
                    var throttlePercent = (float)Convert.ToDouble(value);
                    moduleEngines.thrustPercentage = throttlePercent;
                    return true;
            }
            return base.SetSuffix(suffixName, value);
        }


        public override object GetSuffix(string suffixName)
        {
            if (engine != null)
            {
                return GetEngineSuffix(suffixName, engine);
            }
            if (enginefFx != null)
            {
                return GetEngineFxSuffix(suffixName, enginefFx);
            }
            return base.GetSuffix(suffixName);
        }

        private object GetEngineSuffix(string suffixName, ModuleEngines moduleEngines)
        {
            switch (suffixName)
            {
                case "MAXTHRUST":
                    return moduleEngines.maxThrust;
                case "THRUST":
                    return moduleEngines.finalThrust;
                case "FUELFLOW":
                    return moduleEngines.fuelFlowGui;
                case "ISP":
                    return moduleEngines.realIsp;
                case "FLAMEOUT":
                    return moduleEngines.getFlameoutState;
                case "IGNITION":
                    return moduleEngines.getIgnitionState;
                case "ALLOWRESTART":
                    return moduleEngines.allowRestart;
                case "ALLOWSHUTDOWN":
                    return moduleEngines.allowShutdown;
                case "THROTTLELOCK":
                    return moduleEngines.throttleLocked;
                case "THRUSTLIMIT":
                    return moduleEngines.thrustPercentage;
            }
            return base.GetSuffix(suffixName);
        }

        private object GetEngineFxSuffix(string suffixName, ModuleEnginesFX moduleEngines)
        {
            switch (suffixName)
            {
                case "MAXTHRUST":
                    return moduleEngines.maxThrust;
                case "THRUST":
                    return moduleEngines.finalThrust;
                case "FUELFLOW":
                    return moduleEngines.fuelFlowGui;
                case "ISP":
                    return moduleEngines.realIsp;
                case "FLAMEOUT":
                    return moduleEngines.getFlameoutState;
                case "IGNITION":
                    return moduleEngines.getIgnitionState;
                case "ALLOWRESTART":
                    return moduleEngines.allowRestart;
                case "ALLOWSHUTDOWN":
                    return moduleEngines.allowShutdown;
                case "THROTTLELOCK":
                    return moduleEngines.throttleLocked;
                case "THRUSTLIMIT":
                    return moduleEngines.thrustPercentage;
            }
            return base.GetSuffix(suffixName);
        }

        public new static ListValue PartsToList(IEnumerable<global::Part> parts, SharedObjects sharedObj)
        {
            var toReturn = new ListValue();
            foreach (var part in parts)
            {
                foreach (PartModule module in part.Modules)
                {
                    var engineModule = module as ModuleEngines;
                    var engineModuleFx = module as ModuleEnginesFX;
                    if (engineModuleFx != null)
                    {
                        toReturn.Add(new EngineValue(part, engineModuleFx, sharedObj));
                    }
                    else if (engineModule != null)
                    {
                        toReturn.Add(new EngineValue(part, engineModule, sharedObj));
                    }
                }
            }
            return toReturn;
        }
    }
}