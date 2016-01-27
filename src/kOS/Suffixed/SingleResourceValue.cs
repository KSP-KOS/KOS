using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;

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
            AddSuffix("NAME", new Suffix<StringValue>(() => partResource.resourceName));
            AddSuffix("AMOUNT", new Suffix<ScalarDoubleValue>(() => partResource.amount));
            AddSuffix("DENSITY", new Suffix<ScalarDoubleValue>(() => partResource.info.density));
            AddSuffix("CAPACITY", new Suffix<ScalarDoubleValue>(() => partResource.maxAmount));
            AddSuffix("TOGGLEABLE", new Suffix<BooleanValue>(() => partResource.isTweakable));
            AddSuffix("ENABLED", new SetSuffix<BooleanValue>(() => partResource.flowState, value =>
            {
                if (partResource.isTweakable)
                {
                    partResource.flowState = value;
                }
            }));
        }

        public override string ToString()
        {
            return string.Format("RESOURCE({0},{1},{2})", partResource.resourceName, partResource.amount, partResource.maxAmount);
        }
    }
}