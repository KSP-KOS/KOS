using System;
using kOS.Safe.Utilities;
using kOS.AddOns.OrbitalScience;
using kOS.Suffixed.PartModuleField;

namespace kOS.AddOns.OrbitalScience
{
    public class DMOrbitalScienceFieldsFactory
    {
        private const string DMSCIENCEANIMATE = "DMModuleScienceAnimate";
        private const string DMASTEROIDSCANNER = "DMAsteroidScanner";
        private const string DMBATHYMETRY = "DMBathymetry";
        private const string DMBASICSCIENCEMODULE = "DMBasicScienceModule";

        public static ScienceExperimentFields Construct(PartModule mod, SharedObjects shared)
        {
            var typeName = mod.GetType().Name;
            var baseTypeName = mod.GetType().BaseType.Name;

            if (baseTypeName.Contains(DMBASICSCIENCEMODULE) || typeName.Contains(DMASTEROIDSCANNER)) {
                return new DMScienceExperimentFields(mod, shared);
           } else if (typeName.Contains(DMBATHYMETRY)) {
                return new DMBathymetryFields(mod as ModuleScienceExperiment, shared);
            } else if (typeName.Contains(DMSCIENCEANIMATE) || baseTypeName.Contains(DMSCIENCEANIMATE)) {
                return new DMModuleScienceAnimateFields(mod as ModuleScienceExperiment, shared);
            }

            return null;
        }
    }
}

