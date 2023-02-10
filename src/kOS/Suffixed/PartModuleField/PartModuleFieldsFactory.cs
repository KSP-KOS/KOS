using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Encapsulation;
using kOS.AddOns.RemoteTech;
using kOS.Module;

namespace kOS.Suffixed.PartModuleField
{
    /// <summary>
    /// Description of PartModuleFieldFactory.
    /// </summary>
    public class PartModuleFieldsFactory 
    {
        public static ListValue Construct(IEnumerable<PartModule> modules, SharedObjects shared)
        {
            var list = modules.Select(mod => Construct(mod, shared)).ToList();
            return ListValue.CreateList(list);
        } 

        public static PartModuleFields Construct(PartModule mod, SharedObjects shared)
        {
            var moduleGimbal = mod as ModuleGimbal;
            if (moduleGimbal != null)
                return new GimbalFields(moduleGimbal, shared);

            var processor = mod as kOSProcessor;

            if (processor != null) {
                return new kOSProcessorFields(processor, shared);
            }

            if (mod.moduleName.Equals(RemoteTechAntennaModuleFields.RTAntennaModule)) {
                return new RemoteTechAntennaModuleFields(mod, shared);
            }

            if (mod.moduleName.Equals("ModuleScienceContainer")) {
                return new ScienceContainerFields(mod, shared);
            }

            var scienceExperimentFields = ScienceExperimentFieldsFactory.Construct(mod, shared);

            if (scienceExperimentFields != null)
            {
                return scienceExperimentFields;
            }

            return new PartModuleFields(mod, shared);
        }
    }
}