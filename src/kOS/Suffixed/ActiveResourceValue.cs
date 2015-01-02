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
            AddSuffix("NAME", new Suffix<string>(() => activeResource.info.name));
            AddSuffix("AMOUNT", new Suffix<double>(() => activeResource.amount));
            AddSuffix("CAPICITY", new Suffix<double>(() => activeResource.maxAmount));
            AddSuffix("PARTS", new Suffix<SuffixedList<PartValue>>(() => PartValue.PartsToList(activeResource.parts, shared)));
        }

        public override string ToString()
        {
            return string.Format("ACTIVERESOURCE({0},{1},{2})", activeResource.info.name, activeResource.amount, activeResource.maxAmount);
        }
    }
}