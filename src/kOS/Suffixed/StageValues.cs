using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

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
            PartResourceDefinition resourceDef = PartResourceLibrary.Instance.resourceDefinitions.FirstOrDefault(pr => string.Equals(pr.name, resourceName, StringComparison.CurrentCultureIgnoreCase));

            if (resourceDef == null)
            {
                throw new KOSInvalidArgumentException("STAGE", resourceName, "The resource definition could not be found");
            }

            var list = new List<PartResource>();
            if (resourceDef.resourceFlowMode == ResourceFlowMode.STACK_PRIORITY_SEARCH)
            {
                var engines = VesselUtils.GetListOfActivatedEngines(shared.Vessel);
                foreach (var engine in engines)
                {
                    engine.GetConnectedResources(resourceDef.id, resourceDef.resourceFlowMode, list);
                }
            }
            else if (resourceDef.resourceFlowMode == ResourceFlowMode.NO_FLOW) {
                var engines = VesselUtils.GetListOfActivatedEngines(shared.Vessel);
                foreach (var engine in engines)
                {
                    list.AddRange(engine.Resources.GetAll(resourceDef.id));
                }
            }
            else
            {
                shared.Vessel.rootPart.GetConnectedResources(resourceDef.id, resourceDef.resourceFlowMode, list);
            }
            if (list.Count == 0)
            {
                return null;
            }
            double available = 0.0;
            double capacity = 0.0;
            foreach (var resource in list)
            {
                available += resource.amount;
                capacity += resource.maxAmount;
            }
            return Math.Round(available, 2);
        }

        public override string ToString()
        {
            return string.Format("{0} Stage", base.ToString());
        }
    }
}
