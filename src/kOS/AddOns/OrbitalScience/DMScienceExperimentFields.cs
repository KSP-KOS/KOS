using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Suffixed.PartModuleField;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace kOS
{
    public class DMScienceExperimentFields : ScienceExperimentFields
    {
        public DMScienceExperimentFields(PartModule mod, SharedObjects shared)
            : base(mod, shared)
        {
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("TOGGLE", new NoArgsVoidSuffix(ToggleExperiment, "Activate/deactivate the experiment"));
        }

        public void ToggleExperiment()
        {
            ThrowIfNotCPUVessel();

            var toggleMethod = partModule.GetType().GetMethod("toggleEvent",
                BindingFlags.Public | BindingFlags.Instance);

            toggleMethod.Invoke(partModule, new object[] { });
        }

        public override bool Deployed()
        {
            var deployedField = partModule.GetType().GetField("IsDeployed");

            return (bool)deployedField.GetValue(partModule);
        }

        public override bool Inoperable()
        {
            var inoperableField = partModule.GetType().GetField("Inoperable");

            return (bool)inoperableField.GetValue(partModule);
        }

        public override void DeployExperiment()
        {
            ThrowIfNotCPUVessel();

            if (HasData())
            {
                throw new KOSException("Experiment already contains data");
            }

            if (Inoperable())
            {
                throw new KOSException("Experiment is inoperable");
            }

            var gatherDataMethod = partModule.GetType().GetMethod("gatherScienceData",
                BindingFlags.Public | BindingFlags.Instance);

            gatherDataMethod.Invoke(partModule, new object[] { true });
        }

        public override void ResetExperiment()
        {
            ThrowIfNotCPUVessel();

            if (Inoperable())
            {
                throw new KOSException("Experiment is inoperable");
            }

            var gatherDataMethod = partModule.GetType().GetMethod("ResetExperiment",
                BindingFlags.Public | BindingFlags.Instance);

            gatherDataMethod.Invoke(partModule, new object[] { });
        }

        public override void TransmitData()
        {
            ThrowIfNotCPUVessel();

            IScienceDataContainer container = partModule as IScienceDataContainer;

            ScienceData[] data = container.GetData();

            List<IScienceDataTransmitter> tranList = partModule.vessel.FindPartModulesImplementing<IScienceDataTransmitter>();
            if (tranList.Count() > 0 && data.Count() > 0)
            {
                tranList.OrderBy(ScienceUtil.GetTransmitterScore).First().TransmitData(new List<ScienceData>(data));

                DumpData();
            }
            else
                ScreenMessages.PostScreenMessage("No transmitters available on this vessel or no data to transmit.", 4f, ScreenMessageStyle.UPPER_LEFT);
        }
    }
}