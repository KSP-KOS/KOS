using System.Collections.Generic;
using System.Linq;

namespace kOS.Suffixed.Part
{
    public class PartValue : SpecialValue, IKOSTargetable
    {
        public PartValue(global::Part part)
        {
            Part = part;
        }

        protected global::Part Part { get; private set; }
        public override bool SetSuffix(string suffixName, object value)
        {
            switch (suffixName)
            {
                case "CONTROLFROM":
                    {
                        var control = (bool) value;
                        if (control)
                        {
                            var dockingModule = Part.Modules.First<ModuleDockingNode>();
                            var commandModule = Part.Modules.First<ModuleCommand>();

                            if (commandModule != null)
                            {
                                commandModule.MakeReference();
                                return true;
                            }
                            if (dockingModule != null)
                            {
                                dockingModule.MakeReferenceTransform();
                                return true;
                            }
                            Part.vessel.SetReferenceTransform(Part);
                        }
                        else
                        {
                            Part.vessel.SetReferenceTransform(Part.vessel.rootPart);
                        }
                        break;
                    }
                    
            }
            return base.SetSuffix(suffixName, value);
        }
        public override object GetSuffix(string suffixName)
        {
            switch (suffixName)
            {
                case "NAME":
                    return Part.name;
                case "STAGE":
                    return Part.inverseStage;
                case "UID":
                    return Part.uid;
                case "RESOURCES":
                    var resources = new ListValue();
                    foreach (PartResource resource in Part.Resources)
                    {
                        resources.Add(new ResourceValue(resource));
                    }
                    return resources;
                case "MODULES":
                    var modules = new ListValue();
                    foreach (var module in Part.Modules)
                    {
                        modules.Add(module.GetType());
                    }
                    return modules;
                case "TARGETABLE":
                    return Part.Modules.OfType<ITargetable>().Any();
            }
            return base.GetSuffix(suffixName);
        }

        public override string ToString()
        {
            return string.Format("PART({0},{1})", Part.name, Part.uid);
        }

        public static ListValue PartsToList(IEnumerable<global::Part> parts)
        {
            var toReturn = new ListValue();
            foreach (var part in parts)
            {
                toReturn.Add(new PartValue(part));
            }
            return toReturn;
        }

        public virtual ITargetable Target
        {
            get
            {
                return Part.Modules.OfType<ITargetable>().FirstOrDefault();
            }
        }
    }
}