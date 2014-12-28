using System.Collections.Generic;
using kOS.Safe.Encapsulation;

namespace kOS.Suffixed
{
    public class AggregateResourceValue : Structure
    {
        private readonly string name;
        private double amount;
        private double capacity;
        private int containerCount;

        public AggregateResourceValue(string name)
        {
            this.name = name;
            amount = 0;
            capacity = 0;
            containerCount = 0;
        }

        public override object GetSuffix(string suffixName)
        {
            switch (suffixName)
            {
                case "NAME":
                    return name;
                case "AMOUNT":
                    return amount;
                case "CAPACITY":
                    return capacity;
                case "CONTAINERCOUNT":
                    return containerCount;
            }
            return base.GetSuffix(suffixName);
        }

        public void AddResource(PartResource resource)
        {
            amount += resource.amount;
            capacity += resource.maxAmount;
            containerCount++;
        }

        public override string ToString()
        {
            return string.Format("SHIPRESOURCE({0},{1},{2}", name, amount, capacity);
        }

        public static ListValue PartsToList(IEnumerable<global::Part> parts)
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
                        aggregateResourceAmount = new AggregateResourceValue(module.resourceName);
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
    }
}