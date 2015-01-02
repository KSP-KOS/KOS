using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Suffixed.Part;

namespace kOS.Suffixed
{
    public class AggregateResourceValue : Structure
    {
        private readonly string name;
        private readonly SharedObjects shared;
        private double amount;
        private double capacity;
        private readonly ListValue<PartValue> parts;

        public AggregateResourceValue(string name, SharedObjects shared)
        {
            this.name = name;
            this.shared = shared;
            amount = 0;
            capacity = 0;
            parts = new ListValue<PartValue>();
            InitializeAggregateResourceSuffixes();
        }

        private void InitializeAggregateResourceSuffixes()
        {
            AddSuffix("NAME", new Suffix<string>(() => name));
            AddSuffix("AMOUNT", new Suffix<double>(() => amount));
            AddSuffix("CAPICITY", new Suffix<double>(() => capacity));
            AddSuffix("PARTS", new Suffix<ListValue<PartValue>>(() => parts));
        }

        public void AddResource(PartResource resource)
        {
            amount += resource.amount;
            capacity += resource.maxAmount;
            parts.Add(new PartValue(resource.part, shared));
        }

        public override string ToString()
        {
            return string.Format("SHIPRESOURCE({0},{1},{2})", name, amount, capacity);
        }

        public static ListValue PartsToList(IEnumerable<global::Part> parts, SharedObjects shared)
        {
            var list = new ListValue();
            var resources = new Dictionary<string, AggregateResourceValue>();
            foreach (var part in parts)
            {
                foreach (PartResource module in part.Resources)
                {
                    AggregateResourceValue aggregateResourceAmount;
                    if (!resources.TryGetValue(module.resourceName, out aggregateResourceAmount))
                    {
                        aggregateResourceAmount = new AggregateResourceValue(module.resourceName, shared);
                    }
                    aggregateResourceAmount.AddResource(module);
                    resources[module.resourceName] = aggregateResourceAmount;
                }
            }
            foreach (var resource in resources)
            {
                list.Add(resource.Value);
            }
            return list;
        }

        public static ListValue<AggregateResourceValue> FromVessel(Vessel vessel, SharedObjects shared)
        {
            var resources = new Dictionary<string, AggregateResourceValue>();

            foreach (var resource in vessel.parts.SelectMany(part => part.Resources.list))
            {
                AggregateResourceValue resourceValue;
                if (resources.TryGetValue(resource.info.name, out resourceValue))
                {
                    resourceValue.AddResource(resource);
                }
                else
                {
                    resources.Add(resource.info.name, new AggregateResourceValue(resource.info.name, shared));
                }
            }
            return ListValue<AggregateResourceValue>.CreateList(resources.Values);
        }
    }
}