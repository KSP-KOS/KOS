using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Suffixed.Part;

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("ActiveResource")]
    public class ActiveResourceValue : Structure
    {
        private readonly Vessel vessel;
        private readonly int resourceId;
        private readonly SharedObjects shared;
        private double amount;
        private double capacity;

        public ActiveResourceValue(Vessel ves, int resId, SharedObjects shared)
        {
            vessel = ves;
            resourceId = resId;
            this.shared = shared;
            InitializeActiveResourceSuffixes();
        }

        private void InitializeActiveResourceSuffixes()
        {
            AddSuffix("NAME", new Suffix<StringValue>(GetName, "The name of the resource (eg LiguidFuel, ElectricCharge)"));
            AddSuffix("AMOUNT", new Suffix<ScalarValue>(GetAmount, "The resources currently available"));
            AddSuffix("CAPACITY", new Suffix<ScalarValue>(GetCapacity, "The total storage capacity currently available"));
            AddSuffix("DENSITY", new Suffix<ScalarValue>(GetDensity, "The density of this resource"));
            // TODO: IMPORTANT fix this before release, though it isn't a documented suffix and there may be value to unifying the resource classes
            //AddSuffix("PARTS", new Suffix<ListValue<PartValue>>(() => PartValueFactory.ConstructGeneric(activeResource.parts, shared), "The containers for this resource"));
        }

        public override string ToString()
        {
            return string.Format("ACTIVERESOURCE({0},{1},{2})", GetName(), GetAmount(), GetCapacity());
        }

        public StringValue GetName()
        {
            return PartResourceLibrary.Instance.resourceDefinitions[resourceId].name;
        }

        public ScalarValue GetAmount()
        {
            vessel.GetConnectedResourceTotals(resourceId, out amount, out capacity, true);
            return amount;
        }

        public ScalarValue GetCapacity()
        {
            vessel.GetConnectedResourceTotals(resourceId, out amount, out capacity, true);
            return capacity;
        }

        public ScalarValue GetDensity()
        {
            return PartResourceLibrary.Instance.resourceDefinitions[resourceId].density;
        }
    }
}