using kOS.Module;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Suffixed.PartModuleField;
using kOS.Utilities;
using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using kOS.Safe.Compilation.KS;
using kOS.Safe.Utilities;

using UnityEngine;

namespace kOS.Suffixed.Part
{
    [kOS.Safe.Utilities.KOSNomenclature("Part")]
    public class PartValue : Structure, IKOSTargetable
    {
        public SharedObjects Shared { get; private set; }
        public global::Part Part { get; private set; }
        public PartValue Parent { get; private set; }
        public DecouplerValue Decoupler { get; private set; }
        public ListValue<PartValue> Children { get; private set; }
        public Structure ParentValue { get { return (Structure)Parent ?? StringValue.None; } }
        public Structure DecouplerValue { get { return (Structure)Decoupler ?? StringValue.None; } }
        public int DecoupledIn { get { return (Decoupler != null) ? Decoupler.Part.inverseStage : -1; } }

        /// <summary>
        /// Do not call! VesselTarget.ConstructPart uses this, would use `friend VesselTarget` if this was C++!
        /// </summary>
        internal PartValue(SharedObjects shared, global::Part part, PartValue parent, DecouplerValue decoupler)
        {
            Shared = shared;
            Part = part;
            Parent = parent;
            Decoupler = decoupler;
            RegisterInitializer(PartInitializeSuffixes);
            Children  = new ListValue<PartValue>();
        }

        private void PartInitializeSuffixes()
        {
            AddSuffix("CONTROLFROM", new NoArgsVoidSuffix(ControlFrom));
            AddSuffix("NAME", new Suffix<StringValue>(() => Part.name));
            AddSuffix("FUELCROSSFEED", new Suffix<BooleanValue>(() => Part.fuelCrossFeed));
            AddSuffix("TITLE", new Suffix<StringValue>(() => Part.partInfo.title));
            AddSuffix("STAGE", new Suffix<ScalarValue>(() => Part.inverseStage));
            AddSuffix("CID", new Suffix<StringValue>(() => Part.craftID.ToString()));
            AddSuffix("UID", new Suffix<StringValue>(() => Part.flightID.ToString()));
            AddSuffix("ROTATION", new Suffix<Direction>(() => new Direction(Part.transform.rotation)));
            AddSuffix("POSITION", new Suffix<Vector>(() => GetPosition()));
            AddSuffix("TAG", new SetSuffix<StringValue>(GetTagName, SetTagName));
            AddSuffix("FACING", new Suffix<Direction>(() => GetFacing()));
            AddSuffix("BOUNDS", new Suffix<BoundsValue>(GetBoundsValue));
            AddSuffix("RESOURCES", new Suffix<ListValue>(() => GatherResources(Part)));
            AddSuffix("TARGETABLE", new Suffix<BooleanValue>(() => Part.Modules.OfType<ITargetable>().Any()));
            AddSuffix("SHIP", new Suffix<VesselTarget>(() => VesselTarget.CreateOrGetExisting(Part.vessel, Shared)));
            AddSuffix("HASMODULE", new OneArgsSuffix<BooleanValue, StringValue>(HasModule));
            AddSuffix("GETMODULE", new OneArgsSuffix<PartModuleFields, StringValue>(GetModule));
            AddSuffix("GETMODULEBYINDEX", new OneArgsSuffix<PartModuleFields, ScalarValue>(GetModuleIndex));
            AddSuffix(new[] { "MODULES", "ALLMODULES" }, new Suffix<ListValue>(GetAllModules, "A List of all the modules' names on this part"));
            AddSuffix("PARENT", new Suffix<Structure>(() => ParentValue, "The parent part of this part"));
            AddSuffix(new[] { "DECOUPLER", "SEPARATOR" }, new Suffix<Structure>(() => DecouplerValue, "The part that will decouple/separate this part when activated"));
            AddSuffix(new[] { "DECOUPLEDIN", "SEPARATEDIN" }, new Suffix<ScalarValue>(() => DecoupledIn));
            AddSuffix("HASPARENT", new Suffix<BooleanValue>(() => Part.parent != null, "Tells you if this part has a parent, is used to avoid null exception from PARENT"));
            AddSuffix("CHILDREN", new Suffix<ListValue<PartValue>>(() => PartValueFactory.ConstructGeneric(Part.children, Shared), "A LIST() of the children parts of this part"));
            AddSuffix("DRYMASS", new Suffix<ScalarValue>(() => Part.GetDryMass(), "The Part's mass when empty"));
            AddSuffix("MASS", new Suffix<ScalarValue>(() => Part.CalculateCurrentMass(), "The Part's current mass"));
            AddSuffix("WETMASS", new Suffix<ScalarValue>(() => Part.GetWetMass(), "The Part's mass when full"));
            AddSuffix("HASPHYSICS", new Suffix<BooleanValue>(() => Part.HasPhysics(), "Is this a strange 'massless' part"));
            AddSuffix("SYMMETRYCOUNT", new Suffix<ScalarIntValue>(() => Part.symmetryCounterparts.Count + 1));
            AddSuffix("SYMMETRYTYPE", new Suffix<ScalarIntValue>(() => (int)Part.symMethod));
            AddSuffix("REMOVESYMMETRY", new NoArgsVoidSuffix(Part.RemoveFromSymmetry));
            AddSuffix("SYMMETRYPARTNER", new OneArgsSuffix<PartValue, ScalarValue>(GetSymmetryPartner));

            AddSuffix("PARTSNAMED", new OneArgsSuffix<ListValue, StringValue>(GetPartsNamed));
            AddSuffix("PARTSNAMEDPATTERN", new OneArgsSuffix<ListValue, StringValue>(GetPartsNamedPattern));
            AddSuffix("PARTSTITLED", new OneArgsSuffix<ListValue, StringValue>(GetPartsTitled));
            AddSuffix("PARTSTITLEDPATTERN", new OneArgsSuffix<ListValue, StringValue>(GetPartsTitledPattern));
            AddSuffix("PARTSDUBBED", new OneArgsSuffix<ListValue, StringValue>(GetPartsDubbed));
            AddSuffix("PARTSDUBBEDPATTERN", new OneArgsSuffix<ListValue, StringValue>(GetPartsDubbedPattern));
            AddSuffix("MODULESNAMED", new OneArgsSuffix<ListValue, StringValue>(GetModulesNamed));
            AddSuffix("PARTSTAGGED", new OneArgsSuffix<ListValue, StringValue>(GetPartsTagged));
            AddSuffix("PARTSTAGGEDPATTERN", new OneArgsSuffix<ListValue, StringValue>(GetPartsTaggedPattern));
            AddSuffix("ALLTAGGEDPARTS", new NoArgsSuffix<ListValue>(GetAllTaggedParts));
            AddSuffix("ATTITUDECONTROLLERS", new NoArgsSuffix<ListValue>(() => new ListValue(GetAttitudeControllers())));
        }

        public BoundsValue GetBoundsValue()
        {
            // Our normal facings use Z for forward, but parts use Y for forward:
            Quaternion rotateYToZ = Quaternion.FromToRotation(Vector2.up, Vector3.forward);

            Bounds unionBounds = new Bounds();

            Collider[] colliders = Part.GetComponentsInChildren<Collider>();
            for (int colliderIndex = 0; colliderIndex < colliders.Length; ++colliderIndex)
            {
                Collider collider = colliders[colliderIndex];

                // Colliders that are triggers should be ignored.
                // They merely fire an event when they intersect with something, but have no physics effect.
                if (collider.isTrigger) continue;

                // Colliders report their bounds in world space, as AABB. But we need the bounds in Part local space.
                // AABB also means that there may be (significant) empty space inside the bounds.
                // To get the local bounds:
                // For a MeshCollider we get the sharedMesh.bounds which does use local space.
                // For the other colliders we need special cases.

                Vector3 center;
                Vector3 extents;

                if (collider is MeshCollider meshCollider)
                {
                    center = meshCollider.sharedMesh.bounds.center;
                    extents = meshCollider.sharedMesh.bounds.extents;
                }
                else if (collider is BoxCollider boxCollider)
                {
                    center = boxCollider.center;
                    extents = boxCollider.size / 2;
                }
                else if (collider is SphereCollider sphereCollider)
                {
                    center = sphereCollider.center;
                    extents = Vector3.one * sphereCollider.radius;
                }
                else if (collider is CapsuleCollider capsuleCollider)
                {
                    center = capsuleCollider.center;

                    extents = Vector3.one * capsuleCollider.radius;
                    // The Capsule could face in one of 3 directions, overwrite extent on that axis.
                    switch (capsuleCollider.direction)
                    {
                        case 0:
                        {
                            extents.x = capsuleCollider.height / 2;
                            break;
                        }
                        case 1:
                        {
                            extents.y = capsuleCollider.height / 2;
                            break;
                        }
                        case 2:
                        {
                            extents.z = capsuleCollider.height / 2;
                            break;
                        }
                        default:
                        {
                            SafeHouse.Logger.LogWarning($"Unknown CapsuleCollider direction: {capsuleCollider.direction} collider #{colliderIndex} of '{Part}' will be skipped.");
                            continue;
                        }
                    }
                }
                else if (collider is WheelCollider wheelCollider)
                {
                    center = wheelCollider.center;
                    extents = Vector3.one * wheelCollider.radius;

                    // Wheels may move on their suspension.
                    center.y += wheelCollider.suspensionDistance / 2;
                    extents.y += wheelCollider.suspensionDistance / 2;
                }
                else
                {
                    SafeHouse.Logger.LogWarning($"Unknown Collider type: '{collider.GetType().FullName}' collider #{colliderIndex} of Part: '{Part}' will be skipped.");
                    continue;
                }

                // Part meshes could be scaled as well as rotated (the mesh might describe a
                // part that's 1 meter wide while the real part is 2 meters wide, and has a scale of 2x
                // encoded into its transform to do this).  Because of this, the only really
                // reliable way to get the real shape is to let the transform do its work on all 6 corners
                // of the bounding box, transforming them with the mesh's transform, then back-calculating
                // from that world-space result back into the part's own reference frame to get the bounds
                // relative to the part.

                // This triple-nested loop visits all 8 corners of the box:
                for (int signX = -1; signX <= 1; signX += 2) // -1, then +1
                    for (int signY = -1; signY <= 1; signY += 2) // -1, then +1
                        for (int signZ = -1; signZ <= 1; signZ += 2) // -1, then +1
                        {
                            Vector3 corner = center + new Vector3(signX * extents.x, signY * extents.y, signZ * extents.z);
                            Vector3 worldCorner = collider.transform.TransformPoint(corner);
                            Vector3 partCorner = rotateYToZ * Part.transform.InverseTransformPoint(worldCorner);

                            // Stretches the bounds we're making (which started at size zero in all axes),
                            // just big enough to include this corner:
                            unionBounds.Encapsulate(partCorner);
                        }
            }

            Vector min = new Vector(unionBounds.min);
            Vector max = new Vector(unionBounds.max);
            return new BoundsValue(min, max, delegate { return GetPosition() + new Vector(Part.boundsCentroidOffset); }, delegate { return GetFacing(); }, Shared);
        }


        public void ThrowIfNotCPUVessel()
        {
            if (Part.vessel.id != Shared.Vessel.id)
                throw new KOSWrongCPUVesselException();
        }

        private PartModuleFields GetModule(StringValue modName)
        {
            foreach (PartModule mod in Part.Modules)
            {
                if (string.Equals(mod.moduleName, modName, StringComparison.OrdinalIgnoreCase))
                {
                    return PartModuleFieldsFactory.Construct(mod, Shared);
                }
            }
            throw new KOSLookupFailException("module", modName.ToUpper(), this);
        }

        private BooleanValue HasModule(StringValue modName)
        {
            foreach (PartModule mod in Part.Modules)
            {
                if (string.Equals(mod.moduleName, modName, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private PartModuleFields GetModuleIndex(ScalarValue moduleIndex)
        {
            if (moduleIndex < Part.Modules.Count)
            {
                return PartModuleFieldsFactory.Construct(Part.Modules.GetModule(moduleIndex), Shared);
            }
            throw new KOSLookupFailException("module", string.Format("MODULEINDEX[{0}]", moduleIndex), this);
        }

        public StringValue GetTagName() // public because I picture this being a useful API method later
        {
            KOSNameTag tagModule = Part.Modules.OfType<KOSNameTag>().FirstOrDefault();
            return tagModule == null ? string.Empty : tagModule.nameTag;
        }

        private void SetTagName(StringValue value)
        {
            ThrowIfNotCPUVessel();
            KOSNameTag tagModule = Part.Modules.OfType<KOSNameTag>().FirstOrDefault();
            if (tagModule != null) tagModule.nameTag = value;
        }

        public override string ToString()
        {
            string tagName = GetTagName();
            if (string.IsNullOrEmpty(tagName))
                return string.Format("PART({0},uid={1})", Part.name, Part.uid());
            return string.Format("PART({0},tag={1})", Part.name, tagName);
        }

        public virtual ITargetable Target
        {
            get
            {
                return Part.Modules.OfType<ITargetable>().FirstOrDefault();
            }
        }

        public Direction GetFacing()
        {
            // Our normal facings use Z for forward, but parts use Y for forward:
            Quaternion rotateZToY = Quaternion.FromToRotation(Vector3.forward, Vector3.up);
            Quaternion newRotation = Part.transform.rotation * rotateZToY;
            return new Direction(newRotation);
        }

        public Vector GetPosition()
        {
            Vector3d positionError = VesselTarget.CreateOrGetExisting(Part.vessel, Shared).GetPositionError();
            return new Vector(Part.transform.position - Shared.Vessel.CoMD + positionError);
        }

        private void ControlFrom()
        {
            ThrowIfNotCPUVessel();
            var dockingModule = Part.Modules.OfType<ModuleDockingNode>().FirstOrDefault();
            var commandModule = Part.Modules.OfType<ModuleCommand>().FirstOrDefault();

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
                throw new KOSCommandInvalidHereException(LineCol.Unknown(), "CONTROLFROM", "a generic part value", "a docking port or command part");
            }
        }

        private ListValue GatherResources(global::Part part)
        {
            var resources = new ListValue();
            // eliminate enumerators, use index based access
            for (int i = 0; i < part.Resources.Count; i++)
            {
                resources.Add(new SingleResourceValue(part.Resources[i]));
            }
            return resources;
        }

        private PartValue GetSymmetryPartner(ScalarValue index)
        {
            global::Part p = Part.getSymmetryCounterPart(index);
            if (p == null)
                return this; // all parts are "self" partnered in the way we're using this suffix
            return PartValueFactory.Construct(p, Shared);
        }

        private ListValue GetAllModules()
        {
            var returnValue = new ListValue();
            foreach (PartModule mod in Part.Modules)
            {
                returnValue.Add(new StringValue(mod.moduleName));
            }
            return returnValue;
        }

        /// <summary>
        /// Return all the parts matching the condition given, in the parts
        /// tree starting from this part downward (this branch of the vessel parts tree)
        /// Note that this uses recursion to scan the children of the part and so on.
        /// </summary>
        /// <param name="p">The part to search from (root of the branch being searched)</param>
        /// <param name="condition">The predicate comparison to check for</param>
        /// <param name="listToFill">An empty list that this will fill with the result</param>
        private static void StaticFindPartsInBranch(global::Part p, Predicate<global::Part> condition, List<global::Part> listToFill)
        {
            List<global::Part> childs = p.children;
            int len = childs.Count();
            for (int i = 0; i < len; ++i)
            {
                StaticFindPartsInBranch(childs[i], condition, listToFill);
            }
            if (condition(p))
            {
                listToFill.Add(p);
            }
        }
        public List<global::Part> DynamicFindPartsInBranch(Predicate<global::Part> condition)
        {
            List<global::Part> hits = new List<global::Part>();
            StaticFindPartsInBranch(this.Part, condition, hits);
            return hits;
        }
        public ListValue GetPartsNamed(StringValue partName)
        {
            return PartValueFactory.Construct(
            DynamicFindPartsInBranch(p => String.Equals(p.name, partName, StringComparison.CurrentCultureIgnoreCase)), Shared);
        }
        public ListValue GetPartsNamedPattern(StringValue partNamePattern)
        {
            // Prepare case-insensivie regex.
            Regex r = new Regex(partNamePattern, RegexOptions.IgnoreCase);
            return PartValueFactory.Construct(DynamicFindPartsInBranch(p => r.IsMatch(p.name)), Shared);
        }
        public ListValue GetPartsTitled(StringValue partTitle)
        {
            // Get the list of all the parts where the part's GUI title matches:
            return PartValueFactory.Construct(
                DynamicFindPartsInBranch(p => String.Equals(p.partInfo.title, partTitle, StringComparison.CurrentCultureIgnoreCase)),
                Shared);
        }
        public ListValue GetPartsTitledPattern(StringValue partTitlePattern)
        {
            // Prepare case-insensivie regex.
            Regex r = new Regex(partTitlePattern, RegexOptions.IgnoreCase);
            return PartValueFactory.Construct(DynamicFindPartsInBranch(p => r.IsMatch(p.partInfo.title)), Shared);
        }
        public ListValue GetPartsTagged(StringValue tagName)
        {
            return PartValueFactory.Construct(
                DynamicFindPartsInBranch(
                    p => p.Modules.OfType<KOSNameTag>().Any(
                        tag => String.Equals(tag.nameTag, tagName, StringComparison.CurrentCultureIgnoreCase))),
                    Shared);
        }
        public ListValue GetPartsTaggedPattern(StringValue tagPattern)
        {
            Regex r = new Regex(tagPattern, RegexOptions.IgnoreCase);
            return PartValueFactory.Construct(
                DynamicFindPartsInBranch(
                    p => p.Modules.OfType<KOSNameTag>().Any(
                        tag => r.IsMatch(tag.nameTag))),
                    Shared);
        }
        public ListValue GetPartsDubbed(StringValue searchTerm)
        {
            // Get the list of all the parts where the part's API name OR its GUI title or its tag name matches.
            List<global::Part> kspParts = new List<global::Part>();
            StaticFindPartsInBranch(this.Part,
                p => String.Equals(p.name, searchTerm, StringComparison.CurrentCultureIgnoreCase), kspParts);
            StaticFindPartsInBranch(this.Part,
                p => String.Equals(p.partInfo.title, searchTerm, StringComparison.CurrentCultureIgnoreCase), kspParts);
            StaticFindPartsInBranch(this.Part,
                p => p.Modules.OfType<KOSNameTag>().Any(tag => String.Equals(tag.nameTag, searchTerm, StringComparison.CurrentCultureIgnoreCase)),
                kspParts);

            // The "Distinct" operation is there because it's possible for someone to use a tag name that matches the part name.
            return PartValueFactory.Construct(kspParts.Distinct(), Shared);
        }
        public ListValue GetPartsDubbedPattern(StringValue searchPattern)
        {
            // Prepare case-insensivie regex.
            Regex r = new Regex(searchPattern, RegexOptions.IgnoreCase);
            // Get the list of all the parts where the part's API name OR its GUI title or its tag name matches the pattern.
            List<global::Part> kspParts = new List<global::Part>();
            StaticFindPartsInBranch(this.Part, p => r.IsMatch(p.name), kspParts);
            StaticFindPartsInBranch(this.Part, p => r.IsMatch(p.partInfo.title), kspParts);
            StaticFindPartsInBranch(this.Part, p => p.Modules.OfType<KOSNameTag>().Any(tag => r.IsMatch(tag.nameTag)), kspParts);

            // The "Distinct" operation is there because it's possible for someone to use a tag name that matches the part name.
            return PartValueFactory.Construct(kspParts.Distinct(), Shared);
        }
        /// <summary>
        /// Get all the parts which have at least SOME non-default name:
        /// </summary>
        /// <returns></returns>
        public ListValue GetAllTaggedParts()
        {
            return PartValueFactory.Construct(DynamicFindPartsInBranch(p => p.Modules.OfType<KOSNameTag>()
                .Any(tag => !String.Equals(tag.nameTag, "", StringComparison.CurrentCultureIgnoreCase))), Shared);
        }

        public IEnumerable<AttitudeController> GetAttitudeControllers()
        {
            var result = new List<AttitudeController>();
            bool foundEngine = false;
            foreach (PartModule module in Part.Modules)
            {
                if (module is ModuleEngines)
                {
                    if (foundEngine)
                        continue;
                    foundEngine = true;
                }
                PartModuleFields moduleStructure = PartModuleFieldsFactory.Construct(module, Shared);
                var controller = AttitudeController.FromModule(this, moduleStructure, module);
                if (controller != null)
                    result.Add(controller);
            }
            return result;
        }

        /// <summary>
        /// Return all the PartModules matching the condition given, in the parts
        /// tree starting from this part downward (this branch of the vessel parts tree)
        /// Note that this uses recursion to scan the children of the part and so on.
        /// </summary>
        /// <param name="p">The part to search from (root of the branch being searched)</param>
        /// <param name="condition">The predicate comparison to check a PartModulefor</param>
        /// <param name="listToFill">An empty list that this will fill with the result</param>
        private static void StaticFindModulesInBranch(global::Part p, Predicate<global::PartModule> condition, List<global::PartModule> listToFill)
        {
            List<global::Part> childs = p.children;
            int len = childs.Count();
            for (int i = 0; i < len; ++i)
            {
                StaticFindModulesInBranch(childs[i], condition, listToFill);
            }
            // Can't use Linq query terms because PartModuleList doesn't seem to implement it as far
            // as I can tell, so have to walk it manually:
            int modCount = p.Modules.Count;
            for (int modIndex = 0; modIndex < modCount; ++modIndex)
            {
                if (condition(p.Modules[modIndex]))
                {
                    listToFill.Add(p.Modules[modIndex]);
                }
            }
        }
        public ListValue GetModulesNamed(StringValue modName)
        {
            List<PartModule> result = new List<PartModule>();
            StaticFindModulesInBranch(this.Part, m => String.Equals(m.moduleName, modName, StringComparison.CurrentCultureIgnoreCase), result);
            return PartModuleFieldsFactory.Construct(result, Shared);
        }

        protected bool Equals(PartValue other)
        {
            return Equals(Part, other.Part);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((PartValue)obj);
        }

        public override int GetHashCode()
        {
            return (Part != null ? Part.GetHashCode() : 0);
        }

        public static bool operator ==(PartValue left, PartValue right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(PartValue left, PartValue right)
        {
            return !Equals(left, right);
        }

    }
}
