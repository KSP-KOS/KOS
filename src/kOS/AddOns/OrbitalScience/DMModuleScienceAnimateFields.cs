using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Suffixed.PartModuleField;
using System.Reflection;

namespace kOS.AddOns.OrbitalScience
{
    [kOS.Safe.Utilities.KOSNomenclature("ScienceExperimentModule", KOSToCSharp = false)]
    public class DMModuleScienceAnimateFields : StockScienceExperimentFields
    {
        public DMModuleScienceAnimateFields(ModuleScienceExperiment module, SharedObjects sharedObj)
            : base(module, sharedObj)
        {
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("TOGGLE", new NoArgsVoidSuffix(ToggleExperiment, "Activate/deactivate the experiment"));
        }

        public override bool Deployed()
        {
            var deployedField = module.GetType().GetField("IsDeployed");

            return (bool)deployedField.GetValue(module);
        }

        public void ToggleExperiment()
        {
            ThrowIfNotCPUVessel();

            var toggleMethod = module.GetType().GetMethod("toggleEvent",
                BindingFlags.Public | BindingFlags.Instance);

            toggleMethod.Invoke(module, new object[] { });
        }

        protected override void Deploy()
        {
            ThrowIfNotCPUVessel();

            var gatherDataMethod = module.GetType().GetMethod("gatherScienceData",
                BindingFlags.Public | BindingFlags.Instance);

            gatherDataMethod.Invoke(module, new object[] { true });
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
    }
}