using kOS.Safe.Encapsulation;
using System;
using System.Collections.Generic;
using kOS.Safe.Encapsulation.Part;

namespace kOS.Suffixed.Part
{
    public class EngineValue : PartValue
    {
        private readonly IModuleEngine engine;

        public EngineValue(global::Part part, IModuleEngine engine, SharedObjects sharedObj)
            : base(part, sharedObj)
        {
            this.engine = engine;
        }

        public override bool SetSuffix(string suffixName, object value)
        {
            switch (suffixName)
            {
                case "ACTIVE":
                    var activate = Convert.ToBoolean(value);
                    if (activate)
                    {
                        engine.Activate();
                    }
                    else
                    {
                        engine.Shutdown();
                    }
                    return true;

                case "THRUSTLIMIT":
                    var throttlePercent = (float)Convert.ToDouble(value);
                    engine.ThrustPercentage = throttlePercent;
                    return true;
            }
            return base.SetSuffix(suffixName, value);
        }

        public override object GetSuffix(string suffixName)
        {
            switch (suffixName)
            {
                case "MAXTHRUST":
                    return engine.MaxThrust;

                case "THRUST":
                    return engine.FinalThrust;

                case "FUELFLOW":
                    return engine.FuelFlow;

                case "ISP":
                    return engine.SpecificImpulse;

                case "FLAMEOUT":
                    return engine.Flameout;

                case "IGNITION":
                    return engine.Ignition;

                case "ALLOWRESTART":
                    return engine.AllowRestart;

                case "ALLOWSHUTDOWN":
                    return engine.AllowShutdown;

                case "THROTTLELOCK":
                    return engine.ThrottleLock;

                case "THRUSTLIMIT":
                    return engine.ThrustPercentage;
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
                        toReturn.Add(new EngineValue(part, new ModuleEngineAdapter(engineModuleFx), sharedObj));
                    }
                    else if (engineModule != null)
                    {
                        toReturn.Add(new EngineValue(part, new ModuleEngineAdapter(engineModule), sharedObj));
                    }
                }
            }
            return toReturn;
        }
    }
}