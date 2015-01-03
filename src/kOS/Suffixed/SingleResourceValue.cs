using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Suffixed.Part;

namespace kOS.Suffixed
{
    public class SingleResourceValue : Structure
    {
        private readonly PartResource partResource;

        public SingleResourceValue(PartResource partResource)
        {
            this.partResource = partResource;
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("NAME", new Suffix<string>(() => partResource.resourceName));
            AddSuffix("AMOUNT", new Suffix<double>(() => partResource.amount));
            AddSuffix("DENSITY", new Suffix<double>(() => partResource.info.density));
            AddSuffix("CAPACITY", new Suffix<double>(() => partResource.maxAmount));
            AddSuffix("TOGGLEABLE", new Suffix<bool>(() => partResource.isTweakable));
            AddSuffix("ENABLED", new SetSuffix<bool>(() => partResource.flowState, value =>
            {
                if (partResource.isTweakable)
                {
                    partResource.flowState = value;
                }
            }));
        }

        public override string ToString()
        {
            return string.Format("RESOURCE({0},{1},{2}", partResource.resourceName, partResource.amount, partResource.maxAmount);
        }
    }
}