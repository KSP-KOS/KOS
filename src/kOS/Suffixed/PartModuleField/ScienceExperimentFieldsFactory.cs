using System;
using kOS.Safe.Utilities;
using kOS.AddOns.OrbitalScience;

namespace kOS.Suffixed.PartModuleField
{
    public class ScienceExperimentFieldsFactory
    {
        public static ScienceExperimentFields Construct(PartModule mod, SharedObjects shared)
        {
            var fields = DMOrbitalScienceFieldsFactory.Construct(mod, shared);
            if (fields != null)
            {
                return fields;
            } else if (mod is ModuleScienceExperiment)
            {
                return new StockScienceExperimentFields(mod as ModuleScienceExperiment, shared);
            }

            return null;
        }
    }
}

