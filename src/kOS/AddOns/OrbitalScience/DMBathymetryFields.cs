﻿using kOS.Safe.Encapsulation.Suffixes;
using System.Reflection;

namespace kOS.AddOns.OrbitalScience
{
    [kOS.Safe.Utilities.KOSNomenclature("ScienceExperimentModule", KOSToCSharp = false)]
    public class DMBathymetryFields : DMModuleScienceAnimateFields
    {
        public DMBathymetryFields(ModuleScienceExperiment mod, SharedObjects shared)
            : base(mod, shared)
        {
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("LIGHTSON", new NoArgsVoidSuffix(LightsOn, "Turn on the experiment lights"));
            AddSuffix("LIGHTSOFF", new NoArgsVoidSuffix(LightsOff, "Turn off the experiment lights"));
        }

        protected void LightsOn()
        {
            ThrowIfNotCPUVessel();

            var lightsMethod = module.GetType().GetMethod("turnLightsOn",
                BindingFlags.Public | BindingFlags.Instance);

            lightsMethod.Invoke(module, new object[] { });
        }

        protected void LightsOff()
        {
            ThrowIfNotCPUVessel();

            var lightsMethod = module.GetType().GetMethod("turnLightsOff",
                BindingFlags.Public | BindingFlags.Instance);

            lightsMethod.Invoke(module, new object[] { });
        }
    }
}