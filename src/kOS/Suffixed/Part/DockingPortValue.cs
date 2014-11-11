using System.Collections.Generic;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Suffixed.Part
{
    public class DockingPortValue: PartValue
    {
        private readonly ModuleDockingNode module;

        public DockingPortValue(ModuleDockingNode module, SharedObjects sharedObj) : base(module.part, sharedObj)
        {
            this.module = module;
            DockingInitializeSuffixes();
        }

        private void DockingInitializeSuffixes()
        {
            AddSuffix("AQUIRERANGE", new Suffix<float>(() => module.acquireRange));
            AddSuffix("AQUIREFORCE", new Suffix<float>(() => module.acquireForce));
            AddSuffix("AQUIRETORQUE", new Suffix<float>(() => module.acquireTorque));
            AddSuffix("REENGAGEDISTANCE", new Suffix<float>(() => module.minDistanceToReEngage));
            AddSuffix("DOCKEDSHIPNAME", new Suffix<string>(() => module.vesselInfo != null ? (string) module.vesselInfo.name : string.Empty));
            AddSuffix("STATE", new Suffix<string>(() => module.state));
            AddSuffix("TARGETABLE", new Suffix<bool>(() => true));
            AddSuffix("UNDOCK", new NoArgsSuffix(() => module.Undock()));
            AddSuffix("TARGET", new NoArgsSuffix(() => module.SetAsTarget()));
        }

        public override ITargetable Target
        {
            get { return module; }
        }

        public static ListValue PartsToList(IEnumerable<global::Part> parts, SharedObjects sharedObj)
        {
            var toReturn = new ListValue();
            foreach (var part in parts)
            {
                foreach (PartModule module in part.Modules)
                {
                    UnityEngine.Debug.Log("Module Found: "+ module);
                    var dockingNode = module as ModuleDockingNode;
                    if (dockingNode != null)
                    {
                        toReturn.Add(new DockingPortValue(dockingNode, sharedObj));
                    }
                }
            }
            return toReturn;
        }
    }
}
