using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Suffixed.Part;
using System;
using System.Linq;

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("ActiveResource")]
    public class ActiveResourceValue : AggregateResourceValue
    {
        private readonly int resourceId;
        private readonly PartSet partSet;
        private double amount;
        private double capacity;
        private WeakReference stageValRef;

        public ActiveResourceValue(PartResourceDefinition definition, SharedObjects shared, StageValues stageValues, PartSet partSet) :
            base(definition, shared)
        {
            stageValRef = new WeakReference(stageValues);
            resourceId = definition.id;
            this.partSet = partSet;
        }

        public override string ToString()
        {
            return string.Format("ACTIVERESOURCE({0},{1},{2})", GetName(), GetAmount(), GetCapacity());
        }

        public override ScalarValue GetAmount()
        {
            CreatePartSet();
            partSet.GetConnectedResourceTotals(resourceId, out amount, out capacity, true);
            return amount;
        }

        public override ScalarValue GetCapacity()
        {
            CreatePartSet();
            partSet.GetConnectedResourceTotals(resourceId, out amount, out capacity, true);
            return capacity;
        }

        public override ListValue GetParts()
        {
            CreatePartSet();
            return PartValueFactory.Construct(partSet.GetParts().Where(
                e => e.Resources.Any(
                    e2 => e2.info.id == resourceId)), shared);
        }

        public void CreatePartSet()
        {
            if (stageValRef.IsAlive)
            {
                var stage = stageValRef.Target as StageValues;
                if (stage != null)
                {
                    stage.CreatePartSet();
                }
            }
        }
    }
}