using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Suffixed.Part;

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("AggregateResource")]
    public class AggregateResourceValue : Structure
    {
        private readonly string name;
        private readonly SharedObjects shared;
        private readonly float density;
        private readonly List<PartResource> resources;

        public AggregateResourceValue(PartResourceDefinition definition, SharedObjects shared)
        {
            name = definition.name;
            density = definition.density;
            this.shared = shared;
            resources = new List<PartResource>();
            InitializeAggregateResourceSuffixes();
        }

        private void InitializeAggregateResourceSuffixes()
        {
            AddSuffix("NAME", new Suffix<StringValue>(() => name, "The name of the resource (eg LiguidFuel, ElectricCharge)"));
            AddSuffix("DENSITY", new Suffix<ScalarValue>(() => density, "The density of the resource"));
            AddSuffix("AMOUNT", new Suffix<ScalarValue>(GetAmount, "The resources currently available"));
            AddSuffix("CAPACITY", new Suffix<ScalarValue>(GetCapacity, "The total storage capacity currently available"));
            AddSuffix("PARTS", new Suffix<ListValue<PartValue>>(GetParts, "The containers for this resource"));
        }

        private ListValue<PartValue> GetParts()
        {
            var parts = PartValueFactory.Construct(resources.Select(r => r.part), shared);
            return ListValue<PartValue>.CreateList(parts);
        }

        private ScalarValue GetCapacity()
        {
            return resources.Sum(r => r.maxAmount);
        }

        private ScalarValue GetAmount()
        {
            return resources.Sum(r => r.amount);
        }

        public void AddResource(PartResource resource)
        {
            resources.Add(resource);
        }

        public override string ToString()
        {
            return string.Format("SHIPRESOURCE({0},{1},{2})", name, GetAmount(), GetCapacity());
        }

        private static Dictionary<string, AggregateResourceValue> ProspectResources(IEnumerable<global::Part> parts, SharedObjects shared)
        {
            var resources = new Dictionary<string, AggregateResourceValue>();
            foreach (var part in parts)
            {
                PartResource resource;
                for (int i = 0; i < part.Resources.Count; ++i)
                {
                    resource = part.Resources[i];
                    AggregateResourceValue aggregateResourceAmount;
                    if (!resources.TryGetValue(resource.resourceName, out aggregateResourceAmount))
                    {
                        aggregateResourceAmount = new AggregateResourceValue(resource.info, shared);
                    }
                    aggregateResourceAmount.AddResource(resource);
                    resources[resource.resourceName] = aggregateResourceAmount;
                }
            }
            return resources;
        }

        public static ListValue PartsToList(IEnumerable<global::Part> parts, SharedObjects shared)
        {
            var list = new ListValue();
            var resources = ProspectResources(parts, shared);
            foreach (var resource in resources)
            {
                list.Add(resource.Value);
            }
            return list;
        }

        public static ListValue<AggregateResourceValue> FromVessel(Vessel vessel, SharedObjects shared)
        {
            var resources = ProspectResources(vessel.parts, shared);
            return ListValue<AggregateResourceValue>.CreateList(resources.Values);
        }
    }
}