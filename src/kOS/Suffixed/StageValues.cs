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
            AddSuffix("RESOURCES", new Suffix<ListValue<ActiveResourceValue>>(GetResourceManifest));
        }

        private ListValue<ActiveResourceValue> GetResourceManifest()
        {
            var resources = shared.Vessel.GetActiveResources();
            var toReturn = new ListValue<ActiveResourceValue>();

            foreach (var resource in resources)
            {
                toReturn.Add(new ActiveResourceValue(resource, shared));
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
            var resource = shared.Vessel.GetActiveResources().FirstOrDefault(r => string.Equals(r.info.name, resourceName, StringComparison.OrdinalIgnoreCase));
            return resource == null ? null : (double?) Math.Round(resource.amount, 2);
        }

        public override string ToString()
        {
            return string.Format("{0} Stage", base.ToString());
        }
    }
}
