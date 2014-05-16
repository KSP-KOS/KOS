using System.Collections.Generic;

namespace kOS.Suffixed.Part
{
    public class DockingPortValue: PartValue
    {
        private readonly ModuleDockingNode module;

        public DockingPortValue(ModuleDockingNode module) : base(module.part)
        {
            this.module = module;
        }

        public override object GetSuffix(string suffixName)
        {
            switch (suffixName)
            {
                case "STATE":
                    return module.state;
                case "FACING":
                    return new Vector( module.GetFwdVector() );
                case "DOCKEDVESSELNAME":
                    return module.vesselInfo != null ? module.vesselInfo.name : string.Empty;
                case "TARGETABLE":
                    return true;
            }
            return base.GetSuffix(suffixName);
        }

        public override ITargetable Target
        {
            get { return module; }
        }

        public new static ListValue PartsToList(IEnumerable<global::Part> parts)
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
                        toReturn.Add(new DockingPortValue(dockingNode));
                    }
                }
            }
            return toReturn;
        }
    }
}
