using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Suffixed.PartModuleField;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace kOS.AddOns.OrbitalScience
{
    [kOS.Safe.Utilities.KOSNomenclature("ScienceExperimentModule", KOSToCSharp = false)]
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

            // Logic is mostly copied from StockScienceExperimentFields, other than as noted below

            ScienceData[] data = container.GetData();
            ScienceData scienceData;
            for (int i = 0; i < data.Length; ++i)
            {
                scienceData = data[i];
                // By using ExperimentResultDialogPage we ensure that the logic calculating the value is exactly the same
                // as that used KSP's dialog.  The page type doesn't include any UI code itself, it just does the math to
                // confirm the values, and stores some callbacks for the UI to call when buttons are pressed.
                ExperimentResultDialogPage page = new ExperimentResultDialogPage(
                    partModule.part, scienceData, scienceData.baseTransmitValue, scienceData.transmitBonus, // the parameters with data we care aboue
                    false, "", false, // disable transmit warning and reset option, these are used for the UI only
                    new ScienceLabSearch(partModule.part.vessel, scienceData), // this is used to calculate the transmit bonus, I think...
                    null, null, null, null); // null callbacks, no sense in creating objects when we won't actually perform the callback.
                // The dialog page modifies the referenced object, so our reference has been updated.
            }

            // Logic pulled from ModuleScienceExperiment.sendDataToComms
            IScienceDataTransmitter bestTransmitter = ScienceUtil.GetBestTransmitter(partModule.vessel);
            if (bestTransmitter != null)
            {
                bestTransmitter.TransmitData(data.ToList());
                for (int i = 0; i < data.Length; ++i)
                {
                    container.DumpData(data[i]); // DumpData calls endExperiment, and handles setting as inoperable
                }
                // DMBasicScienceModule does not implement useCooldown, which is why this differs from StockScienceExperimentFields
            }
            else
            {
                ScreenMessages.PostScreenMessage("No transmitters available on this vessel or no data to transmit.", 4f, ScreenMessageStyle.UPPER_LEFT);
            }

        }
    }
}