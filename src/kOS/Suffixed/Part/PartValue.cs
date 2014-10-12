using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace kOS.Suffixed.Part
{
    public class PartValue : Structure, IKOSTargetable
    {
        private readonly SharedObjects shared;

        protected global::Part Part { get; private set; }

        public PartValue(global::Part part, SharedObjects sharedObj)
        {
            Part = part;
            shared = sharedObj;
            PartInitializeSuffixes();
        }

        private void PartInitializeSuffixes()
        {
            AddSuffix("CONTROLFROM", new NoArgsSuffix(ControlFrom));
            AddSuffix("NAME", new Suffix<global::Part, string>(Part, model => model.name));
            AddSuffix("STAGE", new Suffix<global::Part, int>(Part, model => model.inverseStage));
            AddSuffix("UID", new Suffix<global::Part, uint>(Part, model => model.uid));
            AddSuffix("ROTATION", new Suffix<global::Part, Direction>(Part, model => new Direction(Part.orgRot)));
            AddSuffix("POSITION", new Suffix<global::Part, Vector>(Part, model => new Vector(Part.orgPos)));
            AddSuffix("FACING", new Suffix<global::Part, Direction>(Part, model => GetFacing(Part)));
            AddSuffix("RESOURCES", new Suffix<global::Part, ListValue>(Part, model => GatherResources(Part)));
            AddSuffix("MODULES", new Suffix<global::Part, ListValue>(Part, model => GatherModules(Part)));
            AddSuffix("TARGETABLE", new Suffix<global::Part, bool>(Part, model => Part.Modules.OfType<ITargetable>().Any()));
            AddSuffix("SHIP", new Suffix<global::Part, VesselTarget>(Part, model => new VesselTarget(Part.vessel, shared)));
        }

        public override string ToString()
        {
            return string.Format("PART({0},{1})", Part.name, Part.uid);
        }

        public static ListValue PartsToList(IEnumerable<global::Part> parts, SharedObjects sharedObj)
        {
            var toReturn = new ListValue();
            foreach (var part in parts)
            {
                toReturn.Add(new PartValue(part, sharedObj));
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

        private Direction GetFacing(global::Part part)
        {
            Vector3d up = part.vessel.upAxis;
            var partVec = part.partTransform.forward;

            var d = new Direction { Rotation = Quaternion.LookRotation(partVec, up) };
            return d;
        }

        private void ControlFrom()
        {
            var dockingModule = Part.Modules.OfType<ModuleDockingNode>().First();
            var commandModule = Part.Modules.OfType<ModuleCommand>().First();

            if (commandModule != null)
            {
                commandModule.MakeReference();
            }
            else if (dockingModule != null)
            {
                dockingModule.MakeReferenceTransform();
            }
            else
            {
                Part.vessel.SetReferenceTransform(Part);
            }
        }

        private ListValue GatherResources(global::Part part)
        {
            var resources = new ListValue();
            foreach (PartResource resource in part.Resources)
            {
                resources.Add(new ResourceValue(resource));
            }
            return resources;
        }

        private ListValue GatherModules(global::Part part)
        {
            var modules = new ListValue();
            foreach (var module in part.Modules)
            {
                modules.Add(module.GetType());
            }
            return modules;
        }
    }
}