using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using kOS.Safe.Encapsulation;
using System;

namespace kOS.Suffixed.PartModuleField
{
    [kOS.Safe.Utilities.KOSNomenclature("ScienceExperimentModule", KOSToCSharp=false)]
    public class StockScienceExperimentFields : ScienceExperimentFields
    {
        protected readonly ModuleScienceExperiment module;

        public StockScienceExperimentFields(ModuleScienceExperiment module, SharedObjects sharedObj) : base(module, sharedObj)
        {
            this.module = module;
        }

        public override bool Deployed()
        {
            return module.Deployed;
        }

        public override void DeployExperiment()
        {
            if (HasData())
            {
                throw new KOSException("Experiment already contains data");
            }

            if (Inoperable())
            {
                throw new KOSException("Experiment is inoperable");
            }
                
            Deploy();
        }

        protected virtual void Deploy()
        {
            ThrowIfNotCPUVessel();

            var gatherDataMethod = module.GetType().GetMethod("gatherData",
                BindingFlags.NonPublic | BindingFlags.Instance);

            object result = gatherDataMethod.Invoke(module, new object[] { false });

            IEnumerator e = result as IEnumerator;

            module.StartCoroutine(e);
        }
            
        public override bool Inoperable()
        {
            return module.Inoperable;
        }

        public override void ResetExperiment()
        {
            ThrowIfNotCPUVessel();

            if (Inoperable())
            {
                throw new KOSException("Experiment is inoperable");
            }

            module.ResetExperiment();
        }

        public override void TransmitData()
        {
            ThrowIfNotCPUVessel();

            // This logic is mostly copied to DMScienceExperimentFields, make sure that changes here are copied there

            ScienceData[] data = container.GetData();
            ScienceData scienceData;
            for (int i = 0; i < data.Length; ++i)
            {
                scienceData = data[i];
                // By using ExperimentResultDialogPage we ensure that the logic calculating the value is exactly the same
                // as that used KSP's dialog.  The page type doesn't include any UI code itself, it just does the math to
                // confirm the values, and stores some callbacks for the UI to call when buttons are pressed.
                ExperimentResultDialogPage page = new ExperimentResultDialogPage(
                    module.part, scienceData, scienceData.baseTransmitValue, scienceData.transmitBonus, // the parameters with data we care aboue
                    false, "", false, // disable transmit warning and reset option, these are used for the UI only
                    new ScienceLabSearch(module.part.vessel, scienceData), // this is used to calculate the transmit bonus, I think...
                    null, null, null, null); // null callbacks, no sense in creating objects when we won't actually perform the callback.
                // The dialog page modifies the referenced object, so our reference has been updated.
            }

            // Logic pulled from ModuleScienceExperiment.sendDataToComms
            IScienceDataTransmitter bestTransmitter = ScienceUtil.GetBestTransmitter(module.vessel);
            if (bestTransmitter != null)
            {
                bestTransmitter.TransmitData(data.ToList());
                for (int i = 0; i < data.Length; ++i)
                {
                    container.DumpData(data[i]); // DumpData calls endExperiment, and handles setting as inoperable
                }
                if (module.useCooldown)
                {
                    module.cooldownToGo = module.cooldownTimer;
                }
            }
            else
            {
                ScreenMessages.PostScreenMessage("No transmitters available on this vessel or no data to transmit.", 4f, ScreenMessageStyle.UPPER_LEFT);
            }
        }
    }
}