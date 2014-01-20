using System;
using kOS.Value;

namespace kOS
{
    public class StageValues : SpecialValue
    {
        readonly Vessel vessel;

        public StageValues(Vessel vessel)
        {
            this.vessel = vessel;
        }

        public override object GetSuffix(string suffixName)
        {
            return GetResourceOfCurrentStage(suffixName);
        }

        private object GetResourceOfCurrentStage(String resourceName)
        {
            var activeEngines = VesselUtils.GetListOfActivatedEngines(vessel);
            return Utils.ProspectForResource(resourceName, activeEngines);
        }
    }
}
