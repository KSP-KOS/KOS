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