using System;
using System.Linq;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Suffixed
{
    public class StageValues : Structure
    {
        private readonly SharedObjects shared;

        public StageValues(SharedObjects shared)
        {
            this.shared = shared;

            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("RESOURCES", new Suffix<SuffixedList<AggregateResourceValue>>(GetResourceManifest));
        }

        private SuffixedList<AggregateResourceValue> GetResourceManifest()
        {
            var resources = shared.Vessel.GetActiveResources();
            var toReturn = new SuffixedList<AggregateResourceValue>();

            foreach (var resource in resources)
            {
                toReturn.Add(new AggregateResourceValue(resource, shared));
            }

            return toReturn;
        }

        public override object GetSuffix(string suffixName)
        {
            var resourceAmount = GetResourceOfCurrentStage(suffixName);
            if (resourceAmount.HasValue)
            {
                return resourceAmount.Value;
            }

            return base.GetSuffix(suffixName);
        }

        private double? GetResourceOfCurrentStage(string resourceName)
        {
            Safe.Utilities.Debug.Logger.Log("GetResourceOfCurrentStage: " + resourceName);
            var resource = shared.Vessel.GetActiveResources().FirstOrDefault(r => r.info.name == resourceName);

            Safe.Utilities.Debug.Logger.Log("GetResourceOfCurrentStage: " + resourceName);

            return resource == null ? null : (double?) Math.Round(resource.amount, 2);
        }

        public override string ToString()
        {
            return string.Format("{0} Stage", base.ToString());
        }
    }
}
