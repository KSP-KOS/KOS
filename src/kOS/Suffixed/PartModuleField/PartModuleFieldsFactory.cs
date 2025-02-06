using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Encapsulation;
using kOS.AddOns.RemoteTech;
using kOS.Module;
using System;

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

            // see if any addons have registered a constructor for this module; starting from most derived
            for (Type moduleType = mod.GetType(); moduleType != typeof(PartModule); moduleType = moduleType.BaseType)
            {
                if (constructionMethods.TryGetValue(moduleType, out var constructionMethod))
                {
                    var moduleFields = constructionMethod(mod, shared);
                    if (moduleFields != null)
                    {
                        return moduleFields;
                    }
                }
            }

            if (mod.moduleName.Equals(RemoteTechAntennaModuleFields.RTAntennaModule))
            {
                return new RemoteTechAntennaModuleFields(mod, shared);
            }

            var scienceExperimentFields = ScienceExperimentFieldsFactory.Construct(mod, shared);

            if (scienceExperimentFields != null)
            {
                return scienceExperimentFields;
            }

            if (mod.moduleName.Equals("ModuleScienceContainer"))
            {
                return new ScienceContainerFields(mod, shared);
            }


            return new PartModuleFields(mod, shared);
        }

        public static void RegisterConstructionMethod(Type moduleType, ConstructPartModuleFieldsMethod method)
        {
            constructionMethods[moduleType] = method;
        }

        // maps a module name to a function
        protected static Dictionary<Type, ConstructPartModuleFieldsMethod> constructionMethods = new Dictionary<Type, ConstructPartModuleFieldsMethod>();
    }
}