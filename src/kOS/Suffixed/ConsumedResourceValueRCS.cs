using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using UnityEngine;

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("ConsumedResourceRCS")]
    public class ConsumedResourceValueRCS : Structure
    {
        private readonly string name;
        protected readonly SharedObjects shared;
        private readonly float density;
        private readonly ModuleEngines engine;
        private readonly Propellant propellant;

        public ConsumedResourceValueRCS(Propellant prop, SharedObjects shared)
        {
            this.shared = shared;
            name = prop.displayName;
            density = prop.resourceDef.density;
            propellant = prop;
            InitializeConsumedResourceRCSSuffixes();
        }

        private void InitializeConsumedResourceRCSSuffixes()
        {
            AddSuffix("NAME", new Suffix<StringValue>(() => name, "The name of the resource (eg LiguidFuel, ElectricCharge)"));
            AddSuffix("DENSITY", new Suffix<ScalarValue>(() => density, "The density of the resource"));
            AddSuffix("RATIO", new Suffix<ScalarValue>(() => propellant.ratio, "The volumetric flow ratio of the resource"));
            AddSuffix("AMOUNT", new Suffix<ScalarValue>(() => propellant.actualTotalAvailable, "The resources currently available"));
            AddSuffix("CAPACITY", new Suffix<ScalarValue>(() => propellant.totalResourceCapacity, "The total storage capacity currently available"));
        }

        public override string ToString()
        {
            return string.Format("CONSUMEDRESOURCERCS({0})", name);
        }
    }
}