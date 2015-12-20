using System;
using kOS.Suffixed.PartModuleField;
using System.Reflection;
using System.Collections;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.AddOns.OrbitalScience
{
    public class DMModuleScienceAnimateFields : StockScienceExperimentFields
    {
        public DMModuleScienceAnimateFields(ModuleScienceExperiment module, SharedObjects sharedObj) : base(module, sharedObj)
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
            var toggleMethod = module.GetType().GetMethod("toggleEvent",
                BindingFlags.Public | BindingFlags.Instance);

            toggleMethod.Invoke(module, new object[] { });
        }

        protected override void Deploy()
        {
            var gatherDataMethod = module.GetType().GetMethod("gatherScienceData",
                BindingFlags.Public | BindingFlags.Instance);

            gatherDataMethod.Invoke(module, new object[] { true });
        }

    }
}

