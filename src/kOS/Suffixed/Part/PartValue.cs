using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;
using kOS.Safe.Encapsulation.Suffixes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace kOS.Suffixed.Part
{
    public class PartValue : Structure, IKOSTargetable
    {
        private readonly SharedObjects shared;

        private global::Part Part { get; set; }

        public PartValue(global::Part part, SharedObjects sharedObj)
        {
            Part = part;
            shared = sharedObj;
            
            // This cannot be called from inside InitializeSuffixes because the base constructor calls
            // InitializeSuffixes first before this constructor has set "Part" to a real value.
            PartInitializeSuffixes();
        }

        private void PartInitializeSuffixes()
        {
            AddSuffix("CONTROLFROM", new NoArgsSuffix(ControlFrom));
            AddSuffix("NAME", new Suffix<string>(() => Part.name));
            AddSuffix("STAGE", new Suffix<int>(() => Part.inverseStage));
            AddSuffix("UID", new Suffix<uint>(() => Part.uid));
            AddSuffix("ROTATION", new Suffix<Direction>(() => new Direction(Part.orgRot)));
            AddSuffix("POSITION", new Suffix<Vector>(() => new Vector(Part.orgPos)));
            AddSuffix("FACING", new Suffix<Direction>(() => GetFacing(Part)));
            AddSuffix("RESOURCES", new Suffix<ListValue>(() => GatherResources(Part)));
            AddSuffix("TARGETABLE", new Suffix<bool>(() => Part.Modules.OfType<ITargetable>().Any()));
            AddSuffix("SHIP", new Suffix<VesselTarget>(() => new VesselTarget(Part.vessel, shared)));
            AddSuffix("GETMODULE", new OneArgsSuffix<PartModuleFields,string>(GetModule));
            AddSuffix("MODULES", new Suffix<ListValue>(GetAllModules, "A List of all the modules' names on this part"));            
            AddSuffix("PARENT", new Suffix<PartValue>(() => PartValueFactory.Construct(Part.parent,shared), "The parent part of this part"));
            AddSuffix("HASPARENT", new Suffix<bool>(() => Part.parent != null, "Tells you if this part has a parent, is used to avoid null exception from PARENT"));
            AddSuffix("CHILDREN", new Suffix<ListValue>(GetChildren, "A LIST() of the children parts of this part"));
        }
        
        private PartModuleFields GetModule(string modName)
        {
            foreach (PartModule mod in Part.Modules)
            {
                Debug.Log("Does \"" + mod.moduleName.ToUpper() + "\" == \"" + modName.ToUpper() + "\"?");
                if (mod.moduleName.ToUpper() == modName.ToUpper())
                {
                    Debug.Log("yes it does");
                    return new PartModuleFields(mod,shared);
                }
            }
            throw new KOSLookupFailException( "module", modName.ToUpper(), this );
        }
        public override string ToString()
        {
            return string.Format("PART({0},{1})", Part.name, Part.uid);
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

        private ListValue GetChildren()
        {
            return PartValueFactory.Construct(Part.children);
        }

        private ListValue GetAllModules()
        {
            var returnValue = new ListValue();
            foreach (PartModule mod in Part.Modules)
            {
                returnValue.Add(mod.moduleName);
            }
            return returnValue;
        }
        
    }
}
