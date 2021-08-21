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
        public delegate PartModuleFields ConstructPartModuleFieldsMethod(PartModule partModule, SharedObjects shared);

        public static ListValue Construct(IEnumerable<PartModule> modules, SharedObjects shared)
        {
            var list = modules.Select(mod => Construct(mod, shared)).ToList();
            return ListValue.CreateList(list);
        } 

        public static PartModuleFields Construct(PartModule mod, SharedObjects shared)
        {
            var moduleGimbal = mod as ModuleGimbal;
            if (moduleGimbal != null)
            {
                return new GimbalFields(moduleGimbal, shared);
            }

            var processor = mod as kOSProcessor;
            if (processor != null)
            {
                return new kOSProcessorFields(processor, shared);
            }

            // see if any addons have registered a constructor for this module
            if (constructionMethods.TryGetValue(mod.moduleName, out var constructionMethod))
            {
                var moduleFields = constructionMethod(mod, shared);
                if (moduleFields != null)
                {
                    return moduleFields;
                }
            }

            // this should probably get moved to the registration system
            if (mod.moduleName.Equals(RemoteTechAntennaModuleFields.RTAntennaModule))
            {
                return new RemoteTechAntennaModuleFields(mod, shared);
            }

            var scienceExperimentFields = ScienceExperimentFieldsFactory.Construct(mod, shared);
            if (scienceExperimentFields != null)
            {
                return scienceExperimentFields;
            }

            return new PartModuleFields(mod, shared);
        }

        public static void RegisterConstructionMethod(string moduleName, ConstructPartModuleFieldsMethod method)
        {
            constructionMethods[moduleName] = method;
        }

        // maps a module name to a function
        protected static Dictionary<string, ConstructPartModuleFieldsMethod> constructionMethods = new Dictionary<string, ConstructPartModuleFieldsMethod>();
    }
}