using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using System.Collections;
using System.Linq;
using System.Reflection;
using kOS.Safe.Encapsulation;

namespace kOS.Suffixed.PartModuleField
{
    public class ScienceExperimentFields : PartModuleFields
    {
        private readonly ModuleScienceExperiment module;
        private readonly MethodInfo gatherDataMethod, resetExperimentMethod, sendDataToCommsMethod, dumpDataMethod;

        public ScienceExperimentFields(ModuleScienceExperiment module, SharedObjects sharedObj) : base(module, sharedObj)
        {
            this.module = module;

            gatherDataMethod = module.GetType().GetMethod("gatherData",
                BindingFlags.NonPublic | BindingFlags.Instance);
            resetExperimentMethod = module.GetType().GetMethod("resetExperiment",
                BindingFlags.NonPublic | BindingFlags.Instance);
            sendDataToCommsMethod = module.GetType().GetMethod("sendDataToComms",
                BindingFlags.NonPublic | BindingFlags.Instance);
            dumpDataMethod = module.GetType().GetMethod("dumpData",
                BindingFlags.NonPublic | BindingFlags.Instance);

            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("DEPLOY", new NoArgsVoidSuffix(DeployExperiment, "Deploy and run this experiment"));
            AddSuffix("RESET", new NoArgsVoidSuffix(ResetExperiment, "Reset this experiment"));
            AddSuffix("TRANSMIT", new NoArgsVoidSuffix(TransmitData, "Transmit experiment data back to Kerbin"));
            AddSuffix("DUMP", new NoArgsVoidSuffix(DumpData, "Dump experiment data"));
            AddSuffix("INOPERABLE", new Suffix<BooleanValue>(() => module.Inoperable, "Is this experiment inoperable"));
            AddSuffix("DEPLOYED", new Suffix<BooleanValue>(() => module.Deployed, "Is this experiment deployed"));
            AddSuffix("RERUNNABLE", new Suffix<BooleanValue>(() => module.rerunnable, "Is this experiment rerunnable"));
            AddSuffix("HASDATA", new Suffix<BooleanValue>(() => module.GetData().Any(), "Does this experiment have any data stored"));
        }

        private void DeployExperiment()
        {
            if (module.GetData().Any())
            {
                throw new KOSException("Experiment already contains data");
            }

            if (module.Inoperable)
            {
                throw new KOSException("Experiment is inoperable");
            }

            object result = gatherDataMethod.Invoke(module, new object[] { false });

            IEnumerator e = result as IEnumerator;

            module.StartCoroutine(e);
        }

        private void ResetExperiment()
        {
            if (module.Inoperable)
            {
                throw new KOSException("Experiment is inoperable");
            }

            object result = resetExperimentMethod.Invoke(module, new object[] { });

            IEnumerator e = result as IEnumerator;

            module.StartCoroutine(e);
        }

        private void TransmitData()
        {
            if (!module.GetData().Any())
            {
                throw new KOSException("Experiment contains no data");
            }

            ScienceData[] dataArray = module.GetData();

            foreach (ScienceData data in dataArray)
            {
                sendDataToCommsMethod.Invoke(module, new object[] { data });
            }
        }

        private void DumpData()
        {
            if (!module.rerunnable)
            {
                module.SetInoperable();
            }

            dumpDataMethod.Invoke(module, new object[] { });
        }
    }
}