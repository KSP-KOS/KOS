using System.Collections.Generic;
using kOS.Safe.Encapsulation;

namespace kOS.Suffixed
{
    public class ResourceValue : Structure
    {
        private readonly string name;
        private double amount;
        private double capacity;

        public ResourceValue(PartResource partResource) : this(partResource.resourceName)
        {
            amount = partResource.amount;
            capacity = partResource.maxAmount;
        }

        public ResourceValue(string name)
        {
            this.name = name;
            amount = 0;
            capacity = 0;
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
            }
            return base.GetSuffix(suffixName);
        }

        public void AddResource(PartResource resource)
        {
            amount += resource.amount;
            capacity += resource.maxAmount;
        }

        public override string ToString()
        {
            return string.Format("RESOURCE({0},{1},{2}", name, amount, capacity);
        }

        public static ListValue PartsToList(IEnumerable<global::Part> parts)
        {
            var list = new ListValue();
            var resources = new Dictionary<string, ResourceValue>();
            foreach (var part in parts)
            {
                foreach (PartResource module in part.Resources)
                {
                    ResourceValue resourceAmount;
                    if (!resources.TryGetValue(module.resourceName, out resourceAmount))
                    {
                        resourceAmount = new ResourceValue(module.resourceName);
                    }
                    resourceAmount.AddResource(module);
                    resources[module.resourceName] = resourceAmount;
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