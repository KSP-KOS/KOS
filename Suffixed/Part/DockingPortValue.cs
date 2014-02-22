using System.Collections.Generic;

namespace kOS.Suffixed.Part
{
    public class DockingPortValue: PartValue, IKOSTargetable
    {
        private readonly ModuleDockingNode module;

        public DockingPortValue(global::Part part, ModuleDockingNode module) : base(part)
        {
            this.module = module;
        }

        public override object GetSuffix(string suffixName)
        {
            switch (suffixName)
            {
                case "DockingState":
                    return module.state;
            }
            return base.GetSuffix(suffixName);
        }

        public ITargetable Target
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
                    var dockingNode = module as ModuleDockingNode;
                    if (dockingNode != null)
                    {
                        toReturn.Add(new DockingPortValue(part, dockingNode));
                    }
                }
            }
            return toReturn;
        }
    }
}
