using kOS.Safe.Encapsulation;
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
            AddSuffix("NAME", new Suffix<StringValue>(() => activeResource.info.name, "The name of the resource (eg LiguidFuel, ElectricCharge)"));
            AddSuffix("AMOUNT", new Suffix<ScalarDoubleValue>(() => activeResource.amount, "The resources currently available"));
            AddSuffix("CAPACITY", new Suffix<ScalarDoubleValue>(() => activeResource.maxAmount, "The total storage capacity currently available"));
            AddSuffix("PARTS", new Suffix<ListValue<PartValue>>(() => PartValueFactory.ConstructGeneric(activeResource.parts, shared), "The containers for this resource"));
        }

        public override string ToString()
        {
            return string.Format("ACTIVERESOURCE({0},{1},{2})", activeResource.info.name, activeResource.amount, activeResource.maxAmount);
        }
    }
}