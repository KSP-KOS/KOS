using System;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using kOS.Safe.Encapsulation;

namespace kOS.Suffixed.PartModuleField {
    [kOS.Safe.Utilities.KOSNomenclature("ScienceContainerModule")]
    public class ScienceContainerFields : PartModuleFields {
        protected global::Part part;
        protected IScienceDataContainer container;
        protected ModuleScienceContainer scienceContainer;
        public ScienceContainerFields(PartModule module, SharedObjects shared) : base(module, shared) {
            this.container = module as IScienceDataContainer;
            this.scienceContainer = container as ModuleScienceContainer;
            part = module.part;

            if (container == null) {
                throw new KOSException("This module is not a science data container");
            }

            InitializeSuffixes();
        }

        private void InitializeSuffixes() {
            AddSuffix("HASDATA", new Suffix<BooleanValue>(() => HasData(), "Does this experiment have any data stored"));
            AddSuffix("DATA", new Suffix<ListValue>(Data, "Does this experiment have any data stored"));
            AddSuffix("DUMPDATA", new OneArgsSuffix<ScalarIntValue>(DumpData));
            AddSuffix("COLLECTALL", new NoArgsVoidSuffix(CollectAll, "Collect all experiments"));
        }

        public virtual void CollectAll() {
            scienceContainer.CollectAllAction(new KSPActionParam(new KSPActionGroup(), new KSPActionType()));
        }

        public virtual void DumpData(ScalarIntValue index) {
            container.DumpData(container.GetData()[index]);
        }

        public virtual bool HasData() {
            return container.GetData().Any();
        }

        public virtual ListValue Data() {
            return new ListValue(container.GetData().Select(s => new ScienceDataValue(s, part)).Cast<Structure>());
        }

        public new string ToString() {
            return "SCIENCE CONTAINER";
        }
    }
}

