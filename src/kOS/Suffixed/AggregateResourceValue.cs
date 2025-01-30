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
        protected readonly SharedObjects shared;
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
            AddSuffix("NAME", new Suffix<StringValue>(GetName, "The name of the resource (eg LiguidFuel, ElectricCharge)"));
            AddSuffix("DENSITY", new Suffix<ScalarValue>(GetDensity, "The density of the resource"));
            AddSuffix("AMOUNT", new Suffix<ScalarValue>(GetAmount, "The resources currently available"));
            AddSuffix("CAPACITY", new Suffix<ScalarValue>(GetCapacity, "The total storage capacity currently available"));
            AddSuffix("PARTS", new Suffix<ListValue>(GetParts, "The containers for this resource"));
        }

        public virtual StringValue GetName()
        {
            return name;
        }

        public ScalarValue GetDensity()
        {
            return density;
        }

        public virtual ListValue GetParts()
        {
            return PartValueFactory.Construct(resources.Select(r => r.part), shared);
        }

        public virtual ScalarValue GetCapacity()
        {
            return resources.Sum(r => r.maxAmount);
        }

        public virtual ScalarValue GetAmount()
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

        public static List<AggregateResourceValue> FromVessel(Vessel vessel, SharedObjects shared)
        {
            var resources = ProspectResources(vessel.parts, shared);
            return resources.Values.ToList();
        }
    }
}