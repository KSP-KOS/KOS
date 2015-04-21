using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Part;

namespace kOS.Suffixed.Part
{
    public class PropellantFactory
    {
        public static ListValue PartsToList(IEnumerable<global::Part> parts, SharedObjects shared)
        {
            var list = new ListValue();
            var resources = GetPropellants(parts, shared);
            foreach (var resource in resources)
            {
                list.Add(resource);
            }
            return list;
        }

        public static ListValue<AggregateResourceValue> FromVessel(Vessel vessel, SharedObjects shared)
        {
            var propellants = GetPropellants(vessel.parts, shared);
            IEnumerable<IGrouping<string, IPropellant>> groups = propellants.GroupBy(p => p.Name);
            var toReturn = new ListValue<AggregateResourceValue>();

            foreach (var group in groups)
            {
                toReturn.Add(new AggregateResourceValue(group.ToList()));
            }

            return toReturn;
        }

        private static ListValue GetPropellantParts(Propellant propellant, SharedObjects shared)
        {
            return PartValueFactory.Construct(propellant.connectedResources.Select(cr => cr.part), shared);
        }

        public static IList<IPropellant> GetPropellants(IEnumerable<global::Part> parts, SharedObjects shared)
        {
            var resources = new Dictionary<string, PartPropellants>();
            foreach (var part in parts)
            {
                foreach (PartResource resource in part.Resources)
                {
                    PartPropellants propellants;
                    if (!resources.TryGetValue(resource.resourceName, out propellants))
                    {
                        var definition = PartResourceLibrary.Instance.GetDefinition(resource.resourceName);
                        propellants = new PartPropellants(definition, shared);
                    }
                    propellants.AddResource(resource);
                    resources[resource.resourceName] = propellants;
                }
            }
            return resources.Values.Cast<IPropellant>().ToList();
        }

        public static IList<IPropellant> GetPropellants(IEnumerable<Propellant> propellants, SharedObjects shared)
        {
            return propellants.Select(propellant => GetPropellant(propellant, shared)).ToList();
        }

        public static IPropellant GetPropellant(Propellant propellant, SharedObjects shared)
        {
            return new PropellantInfo(propellant, shared);
        }

        private class PartPropellants : IPropellant
        {
            private readonly IList<PartResource> resources;
            private readonly PartResourceDefinition definition;
            private readonly SharedObjects shared;

            public PartPropellants(PartResourceDefinition definition, SharedObjects shared)
            {
                this.definition = definition;
                this.shared = shared;
                resources = new List<PartResource>();
            }

            public string Name
            {
                get { return definition.name; }
            }

            public float Density
            {
                get { return definition.density; }
            }

            public double Amount
            {
                get { return resources.Sum(p => p.amount); }
            }

            public double Capacity
            {
                get { return resources.Sum(p => p.maxAmount); }
            }

            public ListValue Parts
            {
                get { return PartValueFactory.Construct(resources.Select(r => r.part), shared); }
            }

            public void AddResource(PartResource resource)
            {
                resources.Add(resource);
            }
        }

        private class PropellantInfo : IPropellant
        {
            private readonly Propellant propellant;
            private readonly SharedObjects shared;

            public PropellantInfo(Propellant propellant, SharedObjects shared)
            {
                this.propellant = propellant;
                this.shared = shared;
            }

            public string Name
            {
                get { return propellant.name; }
            }

            public float Density
            {
                get { return PartResourceLibrary.Instance.GetDefinition(Name).density; }
            }

            public double Amount
            {
                get { return propellant.totalResourceAvailable; }
            }

            public double Capacity
            {
                get { return propellant.totalResourceCapacity; }
            }

            public ListValue Parts
            {
                get { return GetPropellantParts(propellant, shared); }
            }

        }
    }
}