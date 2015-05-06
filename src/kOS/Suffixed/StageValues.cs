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
            AddSuffix("NUMBER", new Suffix<int>(() => Staging.CurrentStage));
            AddSuffix("READY", new Suffix<bool>(() => shared.Vessel.isActiveVessel && Staging.separate_ready));
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
            if (!IsResource(suffixName))
            {
                return base.GetSuffix(suffixName);
            }

            var resourceAmount = GetResourceOfCurrentStage(suffixName);
            return resourceAmount.HasValue ? resourceAmount.Value : 0.0;
        }

        private bool IsResource(string suffixName)
        {
            return PartResourceLibrary.Instance.resourceDefinitions.Any(
                pr => string.Equals(pr.name, suffixName, StringComparison.CurrentCultureIgnoreCase));
        }

        private double? GetResourceOfCurrentStage(string resourceName)
        {
            var resource = shared.Vessel.GetActiveResources();
            var match = resource.FirstOrDefault(r => string.Equals(r.info.name, resourceName, StringComparison.InvariantCultureIgnoreCase));
            return match == null ? null : (double?) Math.Round(match.amount, 2);
        }

        public override string ToString()
        {
            return string.Format("{0} Stage", base.ToString());
        }
    }
}
