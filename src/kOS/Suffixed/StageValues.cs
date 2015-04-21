using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Suffixed.Part;

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
            AddSuffix("RESOURCES", new Suffix<ListValue<AggregateResourceValue>>(GetResourceManifest));

        }

        private ListValue<AggregateResourceValue> GetResourceManifest()
        {
            var engines = EngineValue.PartsToEngineAdapters(shared.Vessel.parts, shared);
            var activeEngines = engines.Values.Where(e => e.Ignition);

            var resources = new Dictionary<string, AggregateResourceValue>();

            foreach (var propellant in activeEngines.SelectMany(engine => engine.Propellants))
            {
                AggregateResourceValue aggreate;
                if (!resources.TryGetValue(propellant.Name, out aggreate))
                {
                    resources.Add(propellant.Name, new AggregateResourceValue(propellant));
                    continue;
                }
                aggreate.AddPropellant(propellant);
            }

            return ListValue<AggregateResourceValue>.CreateList(resources.Values);
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
