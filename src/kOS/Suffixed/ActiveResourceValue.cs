﻿using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Suffixed.Part;

namespace kOS.Suffixed
{
    public class ActiveResourceValue : Structure
    {
        private readonly Vessel.ActiveResource activeResource;
        private readonly SharedObjects shared;

        public ActiveResourceValue(Vessel.ActiveResource activeResource, SharedObjects shared)
        {
            this.activeResource = activeResource;
            this.shared = shared;
            InitializeActiveResourceSuffixes();
        }

        private void InitializeActiveResourceSuffixes()
        {
            AddSuffix("NAME", new Suffix<string>(() => activeResource.info.name, "The name of the resource (eg LiguidFuel, ElectricCharge)"));
            AddSuffix("AMOUNT", new Suffix<double>(() => activeResource.amount, "The resources currently available"));
            AddSuffix("CAPACITY", new Suffix<double>(() => activeResource.maxAmount, "The total storage capacity currently available"));
            AddSuffix("PARTS", new Suffix<ListValue<PartValue>>(() => PartValueFactory.ConstructGeneric(activeResource.parts, shared), "The containers for this resource"));
        }

        public override bool KOSEquals(object other)
        {
            ActiveResourceValue that = other as ActiveResourceValue;
            if (that == null) return false;
            return this.activeResource.Equals(that.activeResource);
        } 

        public override string ToString()
        {
            return string.Format("ACTIVERESOURCE({0},{1},{2})", activeResource.info.name, activeResource.amount, activeResource.maxAmount);
        }
    }
}