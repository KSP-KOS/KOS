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
        }

        protected override void InitializeSuffixes()
        {
            base.InitializeSuffixes();
            AddSuffix("AQUIRERANGE", new Suffix<ModuleDockingNode,float>(module, model => model.acquireRange));
            AddSuffix("AQUIREFORCE", new Suffix<ModuleDockingNode,float>(module, model => model.acquireForce));
            AddSuffix("AQUIRETORQUE", new Suffix<ModuleDockingNode,float>(module, model => model.acquireTorque));
            AddSuffix("REENGAGEDISTANCE", new Suffix<ModuleDockingNode,float>(module, model => model.minDistanceToReEngage));
            AddSuffix("DOCKEDSHIPNAME", new Suffix<ModuleDockingNode,string>(module, model => module.vesselInfo != null ? module.vesselInfo.name : string.Empty));
            AddSuffix("STATE", new Suffix<ModuleDockingNode,string>(module, model => model.state));
            AddSuffix("TARGETABLE", new Suffix<ModuleDockingNode,bool>(module, model => true));
            AddSuffix("UNDOCK", new NoArgsSuffix(() => module.Undock()));
            AddSuffix("TARGET", new NoArgsSuffix(() => module.SetAsTarget()));
        }

        public override ITargetable Target
        {
            get { return module; }
        }

        public new static ListValue PartsToList(IEnumerable<global::Part> parts, SharedObjects sharedObj)
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
