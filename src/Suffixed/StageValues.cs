using System;
using kOS.Utilities;

namespace kOS.Suffixed
{
    public class StageValues : SpecialValue
    {
        private readonly Vessel vessel;

        public StageValues(Vessel vessel)
        {
            this.vessel = vessel;
        }

        public override object GetSuffix(string suffixName)
        {
            return GetResourceOfCurrentStage(suffixName);
        }

        private object GetResourceOfCurrentStage(string resourceName)
        {
            var activeEngines = VesselUtils.GetListOfActivatedEngines(vessel);
            var total = Utils.ProspectForResource(resourceName, activeEngines);
            return Math.Round(total, 2);
        }
    }
}
