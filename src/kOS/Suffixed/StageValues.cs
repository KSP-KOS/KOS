using System;
using System.Linq;
using kOS.Safe.Encapsulation;
using kOS.Utilities;

namespace kOS.Suffixed
{
    public class StageValues : Structure
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
            var resource = vessel.GetActiveResources().First(r => r.info.name == resourceName);
            return Math.Round(resource.amount, 2);
        }

        public override string ToString()
        {
            return string.Format("{0} Stage", base.ToString());
        }
    }
}
