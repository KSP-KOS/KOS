using System.Collections.Generic;

namespace kOS.Suffixed
{
    public class EngineValue : PartValue
    {
        private readonly ModuleEngines engines;

        public EngineValue(Part part, ModuleEngines engines) : base(part)
        {
            this.engines = engines;
        }

        public override bool SetSuffix(string suffixName, object value)
        {
            switch (suffixName)
            {
                case "ACTIVE":
                    var activate = (bool) value;
                    if (activate)
                    {
                        engines.Activate();
                    }
                    else
                    {
                        engines.Shutdown();
                    }
                    return true;
                case "THRUSTLIMIT":
                    var throttlePercent = (float) value;
                    engines.thrustPercentage = throttlePercent;
                    return false;
            }
            return base.SetSuffix(suffixName, value);
        }
        public override object GetSuffix(string suffixName)
        {
            switch (suffixName)
            {
                case "MAXTHRUST":
                    return engines.maxThrust;
                case "THRUST":
                    return engines.finalThrust;
                case "FUELFLOW":
                    return engines.fuelFlowGui;
                case "ISP":
                    return engines.realIsp;
                case "FLAMEOUT":
                    return engines.getFlameoutState;
                case "IGNITION":
                    return engines.getIgnitionState;
                case "ALLOWRESTART":
                    return engines.allowRestart;
                case "ALLOWSHUTDOWN":
                    return engines.allowShutdown;
                case "THROTTLELOCK":
                    return engines.throttleLocked;
                case "THRUSTLIMIT":
                    return engines.thrustPercentage;

            }
            return base.GetSuffix(suffixName);
        }

        public new static ListValue PartsToList(IEnumerable<Part> parts)
        {
            var toReturn = new ListValue();
            foreach (var part in parts)
            {
                foreach (PartModule module in part.Modules)
                {
                    var engineModule = module as ModuleEngines;
                    if (engineModule == null) continue;
                    toReturn.Add(new EngineValue(part, engineModule));
                }
            }
            return toReturn;
        }
    }
}