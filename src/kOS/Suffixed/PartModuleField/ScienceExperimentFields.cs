using System;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using kOS.Safe.Encapsulation;

namespace kOS.Suffixed.PartModuleField
{
    [kOS.Safe.Utilities.KOSNomenclature("ScienceExperimentModule")]
    public abstract class ScienceExperimentFields : PartModuleFields
    {
        protected global::Part part;
        protected IScienceDataContainer container;
        public ScienceExperimentFields(PartModule module, SharedObjects shared) : base(module, shared)
        {
            this.container = module as IScienceDataContainer;
            part = module.part;

            if (container == null)
            {
                throw new KOSException("This module is not a science data container");
            }

            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("DEPLOY", new NoArgsVoidSuffix(DeployExperiment, "Deploy and run this experiment"));
            AddSuffix("RESET", new NoArgsVoidSuffix(ResetExperiment, "Reset this experiment"));
            AddSuffix("TRANSMIT", new NoArgsVoidSuffix(TransmitData, "Transmit experiment data back to Kerbin"));
            AddSuffix("DUMP", new NoArgsVoidSuffix(DumpData, "Dump experiment data"));
            AddSuffix("INOPERABLE", new Suffix<BooleanValue>(() => Inoperable(), "Is this experiment inoperable"));
            AddSuffix("DEPLOYED", new Suffix<BooleanValue>(() => Deployed(), "Is this experiment deployed"));
            AddSuffix("RERUNNABLE", new Suffix<BooleanValue>(() => Rerunnable(), "Is this experiment rerunnable"));
            AddSuffix("HASDATA", new Suffix<BooleanValue>(() => HasData(), "Does this experiment have any data stored"));
            AddSuffix("DATA", new Suffix<ListValue>(Data, "Does this experiment have any data stored"));
        }

        public abstract bool Deployed();
        public abstract bool Inoperable();
        public abstract void DeployExperiment();
        public abstract void ResetExperiment();

        public virtual bool Rerunnable()
        {
            return container.IsRerunnable();
        }

        public virtual bool HasData()
        {
            return container.GetData().Any();
        }

        public virtual ListValue Data()
        {
            return new ListValue(container.GetData().Select(s => new ScienceDataValue(s, part)).Cast<Structure>());
        }

        public virtual void DumpData()
        {
            ThrowIfNotCPUVessel();

            Array.ForEach(container.GetData(), (d) => container.DumpData(d));
        }

        public abstract void TransmitData();

        public new string ToString()
        {
            return "SCIENCE EXPERIMENT";
        }
    }
}

