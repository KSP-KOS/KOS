
using System.Collections.Generic;

namespace kOS.Suffixed
{
    public class ResourceValue : SpecialValue
    {
        private readonly string name;
        private double amount;
        private double capacity;

        public ResourceValue(PartResource partResource) :this(partResource.name)
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

        public static ListValue PartsToList(IEnumerable<Part> parts)
        {
            var list = new ListValue();
            var resources = new Dictionary<string, ResourceValue>();
            foreach (var part in parts)
            {
                foreach (PartResource resource in part.Resources)
                {
                    ResourceValue resourceAmount;
                    if (!resources.TryGetValue(resource.name, out resourceAmount))
                    {
                        resourceAmount = new ResourceValue(resource.name);
                    }
                    resourceAmount.AddResource(resource);
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
