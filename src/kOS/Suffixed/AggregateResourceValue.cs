using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Part;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Suffixed.Part;

namespace kOS.Suffixed
{
    public class AggregateResourceValue : Structure, IPropellant
    {
        private readonly IList<IPropellant> propellants;

        public AggregateResourceValue(IPropellant propellant) :this( new List<IPropellant>{propellant})
        {
            if (propellant == null) throw new ArgumentNullException("propellant");
        }

        public AggregateResourceValue(IList<IPropellant> propellants )
        {
            if (propellants == null) throw new ArgumentNullException("propellants");
            if (!propellants.Any()) throw new ArgumentOutOfRangeException("propellants");

            this.propellants = propellants;
            InitializeAggregateResourceSuffixes();
        }

        private void InitializeAggregateResourceSuffixes()
        {
            AddSuffix("NAME", new Suffix<string>(() => Name, "The name of the resource (eg LiguidFuel, ElectricCharge)"));
            AddSuffix("DENSITY", new Suffix<float>(() => Density, "The density of the resource"));
            AddSuffix("AMOUNT", new Suffix<double>(() => Amount, "The resources currently available"));
            AddSuffix("CAPACITY", new Suffix<double>(() => Capacity, "The total storage capacity currently available"));
            AddSuffix("PARTS", new Suffix<ListValue>(() => Parts, "The containers for this resource"));
        }

        private ListValue GetParts()
        {
            var allParts = propellants.SelectMany(p => p.Parts).Cast<PartValue>();
            return ListValue.CreateList(allParts);
        }

        public override string ToString()
        {
            return string.Format("SHIPRESOURCE({0},{1},{2})", Name, Amount, Capacity);
        }

        public void AddPropellant(IPropellant propellantAddendum)
        {
            if (propellantAddendum.Name != Name)
            {
                throw new ArgumentOutOfRangeException("propellantAddendum");
            }
            propellants.Add(propellantAddendum);
        }

        public string Name
        {
            get { return propellants.First().Name; }
        }

        public float Density
        {
            get { return propellants.First().Density; }
        }

        public double Amount
        {
            get { return propellants.Sum(p => p.Amount); }
        }

        public double Capacity
        {
            get { return propellants.Sum(p => p.Capacity); }
        }

        public ListValue Parts
        {
            get { return GetParts(); }
        }
    }
}