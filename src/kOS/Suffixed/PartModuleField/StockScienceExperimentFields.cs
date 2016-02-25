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

            IScienceDataContainer container = module as IScienceDataContainer;

            ScienceData[] data = container.GetData();

            List<IScienceDataTransmitter> tranList = module.vessel.FindPartModulesImplementing<IScienceDataTransmitter>();
            if (tranList.Count() > 0 && data.Count() > 0)
            {
                tranList.OrderBy(ScienceUtil.GetTransmitterScore).First().TransmitData(new List<ScienceData>(data));

                if (!container.IsRerunnable())
                {
                    module.SetInoperable();
                }

                DumpData();
            } else
                ScreenMessages.PostScreenMessage("No transmitters available on this vessel or no data to transmit.", 4f, ScreenMessageStyle.UPPER_LEFT);

        }
            
    }
}