using kOS.Binding;
using kOS.Communication;
using kOS.Module;
using kOS.Safe;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Execution;
using kOS.Safe.Serialization;
using kOS.Safe.Utilities;
using kOS.Suffixed.Part;
using kOS.Suffixed.PartModuleField;
using kOS.Utilities;
using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("Vessel")]
    public class VesselTarget : Orbitable, IKOSTargetable, IKOSScopeObserver
    {
        private static string DumpGuid = "guid";

        public StageValues StageValues { get; }

        //TODO: share these between all CPUs (or maybe have one VesselTarget instance?)
        //TODO: create single list of parts and _slices_ for `children`
        //..... [root, child1, child2, ..., part1-1, part1-2, ..., part2-1, ... heap ;)
        private PartValue rootPart;
        private DecouplerValue nextDecoupler;
        private ListValue<PartValue> allParts;
        private ListValue<DockingPortValue> dockingPorts;
        private ListValue<DecouplerValue> decouplers;
        private Dictionary<global::Part, PartValue> partCache;

        private void InvalidateParts()
        {
            StageValues.stale = true;

            rootPart = null;
            nextDecoupler = null;
            allParts = null;
            dockingPorts = null;
            decouplers = null;
            partCache = null;
        }
        public PartValue Root
        {
            get
            {
                if (allParts == null)
                    ConstructParts();
                return rootPart;
            }
        }
        public DecouplerValue NextDecoupler
        {
            get
            {
                if (allParts == null)
                    ConstructParts();
                return nextDecoupler;
            }
        }
        public ListValue<PartValue> Parts
        {
            get
            {
                if (allParts == null)
                    ConstructParts();
                return allParts;
            }
        }
        public ListValue<DockingPortValue> DockingPorts
        {
            get
            {
                if (dockingPorts == null)
                    ConstructParts();
                return dockingPorts;
            }
        }
        public ListValue<DecouplerValue> Decouplers
        {
            get
            {
                if (decouplers == null)
                    ConstructParts();
                return decouplers;
            }
        }
        public PartValue this[global::Part part]
        {
            get
            {
                if (allParts == null)
                    ConstructParts();
                return partCache[part];
            }
        }
        private void ConstructParts()
        {
            rootPart = null;
            nextDecoupler = null;
            allParts = new ListValue<PartValue>();
            dockingPorts = new ListValue<DockingPortValue>();
            decouplers = new ListValue<DecouplerValue>();
            partCache = new Dictionary<global::Part, PartValue>();

            ConstructPart(Vessel.rootPart, null, null);

            allParts.IsReadOnly = true;
            dockingPorts.IsReadOnly = true;
            decouplers.IsReadOnly = true;
        }
        private void ConstructPart(global::Part part, PartValue parent, DecouplerValue decoupler)
        {
            if (part.State == PartStates.DEAD || part.transform == null)
                return;

            PartValue self = null;
            foreach (var module in part.Modules)
            {
                if (module is IEngineStatus engine)
                {
                    self = new EngineValue(Shared, part, parent, decoupler);
                    break;
                }
                if (module is IStageSeparator)
                {
                    if (module is ModuleDockingNode dock)
                    {
                        var port = new DockingPortValue(Shared, part, parent, decoupler, dock);
                        self = port;
                        dockingPorts.Add(port);
                        if (dock.stagingEnabled)
                        {
                            decoupler = port;
                            decouplers.Add(decoupler);
                        }
                    }
                    else
                    {
                        if (module is LaunchClamp)
                            self = decoupler = new LaunchClampValue(Shared, part, parent, decoupler);
                        else if (module is ModuleDecouple || module is ModuleAnchoredDecoupler)
                            self = decoupler = new DecouplerValue(Shared, part, parent, decoupler);
                        else // ModuleServiceModule ?
                            break;
                        decouplers.Add(decoupler);
                    }
                    // ignore leftover decouplers
                    if (decoupler == null || decoupler.Part.inverseStage >= StageManager.CurrentStage)
                        break;
                    // check if we just created closer decoupler (see StageValues.CreatePartSet)
                    if (nextDecoupler == null || decoupler.Part.inverseStage > nextDecoupler.Part.inverseStage)
                        nextDecoupler = decoupler;
                    break;
                }
                if (module is ModuleEnviroSensor sensor)
                {
                    self = new SensorValue(Shared, part, parent, decoupler, sensor);
                    break;
                }
            }
            if (self == null)
                self = new PartValue(Shared, part, parent, decoupler);
            if (rootPart == null)
                rootPart = self;
            partCache[part] = self;
            allParts.Add(self);
            foreach (var child in part.children)
                ConstructPart(child, self, decoupler);
            self.Children.IsReadOnly = true;
        }

        //Really need per-CPU instance??
        private static Dictionary<InstanceKey, WeakReference> instanceCache;

        public Guid Guid => Vessel.id;
        public override Orbit Orbit => Vessel.orbit;
        public override StringValue GetName() => Vessel.vesselName;
        public override Vector GetPosition() => new Vector(Vessel.CoMD - CurrentVessel.CoMD);
        public override OrbitableVelocity GetVelocities() => new OrbitableVelocity(Vessel);

        /// <summary>
        ///   Calculates the position of this vessel at some future universal timestamp,
        ///   taking into account all currently predicted SOI transition patches, and also
        ///   assuming that all the planned maneuver nodes will actually be executed precisely
        ///   as planned.  Note that this cannot "see" into the future any farther than the
        ///   KSP orbit patches setting allows for.
        /// </summary>
        /// <param name="timeStamp">The time to predict for.  Although the intention is to
        ///   predict for a future time, it could be used to predict for a past time.</param>
        /// <returns>The position as a user-readable Vector in Shared.Vessel-origin raw rotation coordinates.</returns>
        public override Vector GetPositionAtUT(TimeSpan timeStamp)
        {
            string blockingTech;
            if (!Career.CanMakeNodes(out blockingTech))
                throw new KOSLowTechException("use POSITIONAT on a vessel", blockingTech);

            double desiredUT = timeStamp.ToUnixStyleTime();

            Orbit patch = GetOrbitAtUT(desiredUT);
            Vector3d pos = patch.getPositionAtUT(desiredUT);

            // This is an ugly workaround to fix what is probably a bug in KSP's API:
            // If looking at a future orbit patch around a child body of the current body, then
            // the various get{Thingy}AtUT() methods return numbers calculated incorrectly as
            // if the child body was going to remain stationary where it is now, rather than
            // taking into account where it will be later when the intercept happens.
            // This corrects for that case:
            if (Utils.BodyOrbitsBody(patch.referenceBody, Vessel.orbit.referenceBody))
            {
                Vector3d futureSOIPosNow = patch.referenceBody.position;
                Vector3d futureSOIPosLater = patch.referenceBody.getPositionAtUT(desiredUT);
                Vector3d offset = futureSOIPosLater - futureSOIPosNow;
                pos = pos + offset;
            }

            return new Vector(pos - Shared.Vessel.CoMD); // Convert to ship-centered frame.
        }

        /// <summary>
        ///   Calculates the velocities of this vessel at some future universal timestamp,
        ///   taking into account all currently predicted SOI transition patches, and also
        ///   assuming that all the planned maneuver nodes will actually be executed precisely
        ///   as planned.  Note that this cannot "see" into the future any farther than the
        ///   KSP orbit patches setting allows for.
        /// </summary>
        /// <param name="timeStamp">The time to predict for.  Although the intention is to
        ///   predict for a future time, it could be used to predict for a past time.</param>
        /// <returns>The orbit/surface velocity pair as a user-readable Vector in raw rotation coordinates.</returns>
        public override OrbitableVelocity GetVelocitiesAtUT(TimeSpan timeStamp)
        {
            string blockingTech;
            if (!Career.CanMakeNodes(out blockingTech))
                throw new KOSLowTechException("use VELOCITYAT on a vessel", blockingTech);

            double desiredUT = timeStamp.ToUnixStyleTime();

            Orbit patch = GetOrbitAtUT(desiredUT);

            Vector3d orbVel = patch.getOrbitalVelocityAtUT(desiredUT);

            // This is an ugly workaround to fix what is probably a bug in KSP's API:
            // If looking at a future orbit patch around a child body of the current body, then
            // the various get{Thingy}AtUT() methods return numbers calculated incorrectly as
            // if the child body was going to remain stationary where it is now, rather than
            // taking into account where it will be later when the intercept happens.
            // This corrects for that case:
            if (Utils.BodyOrbitsBody(patch.referenceBody, Vessel.orbit.referenceBody))
            {
                Vector3d futureBodyVel = patch.referenceBody.orbit.getOrbitalVelocityAtUT(desiredUT);
                orbVel = orbVel + futureBodyVel;
            }

            // For some weird reason orbital velocities are returned by the KSP API
            // with Y and Z swapped, so swap them back:
            orbVel = new Vector3d(orbVel.x, orbVel.z, orbVel.y);

            CelestialBody parent = patch.referenceBody;
            Vector surfVel;
            if (parent != null)
            {
                Vector3d pos = GetPositionAtUT(timeStamp);
                surfVel = new Vector(orbVel - parent.getRFrmVel(pos + Shared.Vessel.CoMD));
            }
            else
                surfVel = new Vector(orbVel.x, orbVel.y, orbVel.z);

            return new OrbitableVelocity(new Vector(orbVel), surfVel);
        }

        public override Vector GetUpVector()
        {
            return new Vector(Vessel.upAxis);
        }

        public override Vector GetNorthVector()
        {
            return new Vector(VesselUtils.GetNorthVector(Vessel));
        }

        /// <summary>
        ///   Calculate which orbit patch contains the timestamp given.
        /// </summary>
        /// <param name="desiredUT">The timestamp to look for</param>
        /// <returns>the orbit patch the vessel is expected to be in at the given time.</returns>
        public override Orbit GetOrbitAtUT(double desiredUT)
        {
            // After much trial and error this seems to be the only way to do this:

            // Find the lastmost maneuver node that occurs prior to timestamp:
            List<ManeuverNode> nodes = Vessel.patchedConicSolver == null ? new List<ManeuverNode>() : Vessel.patchedConicSolver.maneuverNodes;
            Orbit orbitPatch = Vessel.orbit;
            for (int nodeIndex = 0; nodeIndex < nodes.Count && nodes[nodeIndex].UT <= desiredUT; ++nodeIndex)
            {
                orbitPatch = nodes[nodeIndex].nextPatch; // the orbit patch that starts with this node.
            }

            // Walk the orbit patch list from there looking for the lastmost orbit patch that
            // contains this timestamp, or if this timestamp is later than the end of the last
            // patch, then just return the last patch (this can happen because a patches' EndUT
            // is one period of time and we might be predicting for a point in time more than one
            // period into the future.)
            while (!(orbitPatch.StartUT <= desiredUT && desiredUT < orbitPatch.EndUT))
            {
                // Sadly the way to detect that you are at the end of the orbitPatch list is
                // messy and inconsistent.  Sometimes KSP's API gives you a list that ends
                // with null, and other times it gives you a list that ends with a bogus
                // dummy orbit patch that is not null but contains bogus data and will crash
                // KSP if you try calling many of its methods.  The way to detect if you have
                // such a bogus patch is to check its activePath property:
                if (orbitPatch.nextPatch != null && orbitPatch.nextPatch.activePatch)
                {
                    orbitPatch = orbitPatch.nextPatch;
                }
                else
                {
                    break;
                }
            }

            return orbitPatch;
        }

        /// <summary>
        /// All constructors for this class have been restricted because everyone should
        /// be calling the factory method CreateOrGetExisting() instead.
        /// </summary>
        protected VesselTarget(Vessel target, SharedObjects shared)
            : base(shared)
        {
            Vessel = target;
            StageValues = new StageValues(shared);
            HookEvents();
            RegisterInitializer(InitializeSuffixes);
        }

        /// <summary>
        /// Factory method you should use instead of the constructor for this class.
        /// This will construct a new instance if and only if there isn't already
        /// an instance made for this particular kOSProcessor, for the given vessel
        /// (Uniqueness determinied by the vessel's GUID).
        /// If an instance already exists it will return a reference to that instead of making
        /// a new one.
        /// The reason this enforcement is needed is because VesselTarget has callback hooks
        /// that prevent orphaning and garbage collection.  (The delegate inserted
        /// into KSP's GameEvents counts as a reference to the VesselTarget.)
        /// Using this factory method instead of a constructor prevents having thousands of stale
        /// instances of VesselTarget, which was the cause of Github issue #1980.
        /// </summary>
        public static VesselTarget CreateOrGetExisting(SharedObjects shared) =>
            CreateOrGetExisting(shared.Vessel, shared);

        /// <summary>
        /// Factory method you should use instead of the constructor for this class.
        /// This will construct a new instance if and only if there isn't already
        /// an instance made for this particular kOSProcessor, for the given vessel
        /// (Uniqueness determinied by the vessel's GUID).
        /// If an instance already exists it will return a reference to that instead of making
        /// a new one.
        /// The reason this enforcement is needed is because VesselTarget has callback hooks
        /// that prevent orphaning and garbage collection.  (The delegate inserted
        /// into KSP's GameEvents counts as a reference to the VesselTarget.)
        /// Using this factory method instead of a constructor prevents having thousands of stale
        /// instances of VesselTarget, which was the cause of Github issue #1980.
        /// </summary>
        public static VesselTarget CreateOrGetExisting(Vessel target, SharedObjects shared)
        {
            if (instanceCache == null)
                instanceCache = new Dictionary<InstanceKey, WeakReference>();

            InstanceKey key = new InstanceKey { ProcessorId = shared.Processor.KOSCoreId, VesselId = target.id };
            if (instanceCache.ContainsKey(key))
            {
                WeakReference weakRef = instanceCache[key];
                if (weakRef.IsAlive)
                    return (VesselTarget)weakRef.Target;
                else
                    instanceCache.Remove(key);
            }
            // If it either wasn't in the cache, or it was but the GC destroyed it by now, make a new one:
            VesselTarget newlyConstructed = new VesselTarget(target, shared);
            instanceCache.Add(key, new WeakReference(newlyConstructed));
            return newlyConstructed;
        }

        public static void ClearInstanceCache()
        {
            if (instanceCache == null)
                instanceCache = new Dictionary<InstanceKey, WeakReference>();
            else
                instanceCache.Clear();
        }

        // called in .ctor only
        private void HookEvents()
        {
            GameEvents.onVesselDestroy.Add(OnVesselDestroy);
            GameEvents.onVesselPartCountChanged.Add(OnVesselPartCountChanged);
            GameEvents.onStageActivate.Add(OnStageActive);
            GameEvents.onPartPriorityChanged.Add(OnPartPriorityChanged);
            GameEvents.StageManager.OnGUIStageAdded.Add(OnStageAdded);
            GameEvents.StageManager.OnGUIStageRemoved.Add(OnStageRemoved);
            GameEvents.StageManager.OnGUIStageSequenceModified.Add(OnStageModified);
        }
        private void OnStageActive(int stage) =>
            InvalidateParts();
        private void OnStageAdded(int stage) =>
            InvalidateParts();
        private void OnStageRemoved(int stage) =>
            InvalidateParts();
        private void OnStageModified() =>
            InvalidateParts();

        private void OnPartPriorityChanged(global::Part part)
        {
            if (part.vessel == Vessel)
                InvalidateParts();
        }

        private void OnVesselPartCountChanged(Vessel v)
        {
            if (Vessel.Equals(v))
                InvalidateParts();
        }
        private void OnVesselDestroy(Vessel v)
        {
            if (Vessel.Equals(v))
                UnhookEvents();
        }
        private void UnhookEvents()
        {
            if (Vessel != null)
            {
                GameEvents.onVesselDestroy.Remove(OnVesselDestroy);
                GameEvents.onVesselPartCountChanged.Remove(OnVesselPartCountChanged);
                GameEvents.onStageActivate.Remove(OnStageActive);
                GameEvents.onPartPriorityChanged.Remove(OnPartPriorityChanged);
                GameEvents.StageManager.OnGUIStageAdded.Remove(OnStageAdded);
                GameEvents.StageManager.OnGUIStageRemoved.Remove(OnStageRemoved);
                GameEvents.StageManager.OnGUIStageSequenceModified.Remove(OnStageModified);
                Vessel = null;
                InvalidateParts();
            }
        }
        private Vessel CurrentVessel => Shared.Vessel;
        public ITargetable Target => Vessel;
        public Vessel Vessel { get; private set; }

        // TODO: We will need to replace with the same thing Orbitable:DISTANCE does
        // in order to implement the orbit solver later.
        public ScalarValue GetDistance() =>
            Vector3d.Distance(CurrentVessel.CoMD, Vessel.CoMD);

        public static string[] ShortCuttableShipSuffixes { get; } = new[]
        {
            "HEADING", "PROGRADE", "RETROGRADE", "FACING", "MAXTHRUST", "AVAILABLETHRUST", "VELOCITY", "GEOPOSITION", "LATITUDE",
            "LONGITUDE",
            "UP", "NORTH", "BODY", "ANGULARMOMENTUM", "ANGULARVEL", "MASS", "VERTICALSPEED", "SURFACESPEED", "GROUNDSPEED",
            "AIRSPEED", "SHIPNAME",
            "ALTITUDE", "APOAPSIS", "PERIAPSIS", "SENSOR", "SRFPROGRADE", "SRFRETROGRADE"
        };

        private int linkCount = 0;
        public int LinkCount
        {
            get => linkCount;
            set
            {
                linkCount = value;

                // Note, the following check to fire scopelost when link count
                // hits zero is also happening in Variable.cs, so ScopeLost()
                // fires twice for some cases.  But this still needs to
                // be here to catch cases where the link count hits zero
                // for reasons other than being in a named variable:
                if (linkCount <= 0)
                    ScopeLost();
            }
        }

        public override string ToString()
        {
            return "VESSEL(\"" + Vessel.vesselName + "\")";
        }

        private ListValue GetPartsDubbed(StringValue searchTerm)
        {
            // Get the list of all the parts where the part's API name OR its GUI title or its tag name matches.
            List<global::Part> kspParts = new List<global::Part>();
            kspParts.AddRange(GetRawPartsNamed(searchTerm));
            kspParts.AddRange(GetRawPartsTitled(searchTerm));
            kspParts.AddRange(GetRawPartsTagged(searchTerm));

            // The "Distinct" operation is there because it's possible for someone to use a tag name that matches the part name.
            return PartValueFactory.Construct(kspParts.Distinct(), Shared);
        }

        private ListValue GetPartsDubbedPattern(StringValue searchPattern)
        {
            // Prepare case-insensivie regex.
            Regex r = new Regex(searchPattern, RegexOptions.IgnoreCase);
            // Get the list of all the parts where the part's API name OR its GUI title or its tag name matches the pattern.
            List<global::Part> kspParts = new List<global::Part>();
            kspParts.AddRange(GetRawPartsNamedPattern(r));
            kspParts.AddRange(GetRawPartsTitledPattern(r));
            kspParts.AddRange(GetRawPartsTaggedPattern(r));

            // The "Distinct" operation is there because it's possible for someone to use a tag name that matches the part name.
            return PartValueFactory.Construct(kspParts.Distinct(), Shared);
        }

        private ListValue GetPartsNamed(StringValue partName)
        {
            return PartValueFactory.Construct(GetRawPartsNamed(partName), Shared);
        }

        private IEnumerable<global::Part> GetRawPartsNamed(string partName)
        {
            // Get the list of all the parts where the part's KSP API title matches:
            return Vessel.parts.FindAll(
                part => String.Equals(part.name, partName, StringComparison.CurrentCultureIgnoreCase));
        }

        private ListValue GetPartsNamedPattern(StringValue partNamePattern)
        {
            // Prepare case-insensivie regex.
            Regex r = new Regex(partNamePattern, RegexOptions.IgnoreCase);
            return PartValueFactory.Construct(GetRawPartsNamedPattern(r), Shared);
        }

        private IEnumerable<global::Part> GetRawPartsNamedPattern(Regex partNamePattern)
        {
            // Get the list of all the parts where the part's KSP API title matches the pattern:
            return Vessel.parts.FindAll(
                part => partNamePattern.IsMatch(part.name));
        }

        private ListValue GetPartsTitled(StringValue partTitle)
        {
            return PartValueFactory.Construct(GetRawPartsTitled(partTitle), Shared);
        }

        private IEnumerable<global::Part> GetRawPartsTitled(string partTitle)
        {
            // Get the list of all the parts where the part's GUI title matches:
            return Vessel.parts.FindAll(
                part => String.Equals(part.partInfo.title, partTitle, StringComparison.CurrentCultureIgnoreCase));
        }

        private ListValue GetPartsTitledPattern(StringValue partTitlePattern)
        {
            // Prepare case-insensivie regex.
            Regex r = new Regex(partTitlePattern, RegexOptions.IgnoreCase);
            return PartValueFactory.Construct(GetRawPartsTitledPattern(r), Shared);
        }

        private IEnumerable<global::Part> GetRawPartsTitledPattern(Regex partTitlePattern)
        {
            // Get the list of all the parts where the part's GUI title matches the pattern:
            return Vessel.parts.FindAll(
                part => partTitlePattern.IsMatch(part.partInfo.title));
        }

        private ListValue GetPartsTagged(StringValue tagName)
        {
            return PartValueFactory.Construct(GetRawPartsTagged(tagName), Shared);
        }

        private IEnumerable<global::Part> GetRawPartsTagged(string tagName)
        {
            return Vessel.parts
                .Where(p => p.Modules.OfType<KOSNameTag>()
                .Any(tag => String.Equals(tag.nameTag, tagName, StringComparison.CurrentCultureIgnoreCase)));
        }

        private ListValue GetPartsTaggedPattern(StringValue tagPattern)
        {
            // Prepare case-insensivie regex.
            Regex r = new Regex(tagPattern, RegexOptions.IgnoreCase);
            return PartValueFactory.Construct(GetRawPartsTaggedPattern(r), Shared);
        }

        private IEnumerable<global::Part> GetRawPartsTaggedPattern(Regex tagPattern)
        {
            return Vessel.parts
                .Where(p => p.Modules.OfType<KOSNameTag>()
                .Any(tag => tagPattern.IsMatch(tag.nameTag)));
        }

        /// <summary>
        /// Get all the parts which have at least SOME non-default name:
        /// </summary>
        /// <returns></returns>
        private ListValue GetAllTaggedParts()
        {
            IEnumerable<global::Part> partsWithName = Vessel.parts
                .Where(p => p.Modules.OfType<KOSNameTag>()
                .Any(tag => !String.Equals(tag.nameTag, "", StringComparison.CurrentCultureIgnoreCase)));

            return PartValueFactory.Construct(partsWithName, Shared);
        }

        private ListValue GetModulesNamed(StringValue modName)
        {
            // This is slow - maybe there should be a faster lookup string hash, but
            // KSP's data model seems to have not implemented it:
            IEnumerable<PartModule> modules = Vessel.parts
                .SelectMany(p => p.Modules.Cast<PartModule>()
                .Where(pMod => String.Equals(pMod.moduleName, modName, StringComparison.CurrentCultureIgnoreCase)));

            return PartModuleFieldsFactory.Construct(modules, Shared);
        }

        private ListValue GetPartsInGroup(StringValue groupName)
        {
            var matchGroup = KSPActionGroup.None;
            string upperName = groupName.ToUpper();

            // TODO: later refactor:  put this in a Dictionary lookup instead, and then share it
            // by both this code and the code in ActionGroup.cs:
            if (upperName == "SAS") { matchGroup = KSPActionGroup.SAS; }
            if (upperName == "GEAR") { matchGroup = KSPActionGroup.Gear; }
            if (upperName == "LIGHTS") { matchGroup = KSPActionGroup.Light; }
            if (upperName == "BRAKES") { matchGroup = KSPActionGroup.Brakes; }
            if (upperName == "RCS") { matchGroup = KSPActionGroup.RCS; }
            if (upperName == "ABORT") { matchGroup = KSPActionGroup.Abort; }
            if (upperName == "AG1") { matchGroup = KSPActionGroup.Custom01; }
            if (upperName == "AG2") { matchGroup = KSPActionGroup.Custom02; }
            if (upperName == "AG3") { matchGroup = KSPActionGroup.Custom03; }
            if (upperName == "AG4") { matchGroup = KSPActionGroup.Custom04; }
            if (upperName == "AG5") { matchGroup = KSPActionGroup.Custom05; }
            if (upperName == "AG6") { matchGroup = KSPActionGroup.Custom06; }
            if (upperName == "AG7") { matchGroup = KSPActionGroup.Custom07; }
            if (upperName == "AG8") { matchGroup = KSPActionGroup.Custom08; }
            if (upperName == "AG9") { matchGroup = KSPActionGroup.Custom09; }
            if (upperName == "AG10") { matchGroup = KSPActionGroup.Custom10; }

            ListValue kScriptParts = new ListValue();
            if (matchGroup == KSPActionGroup.None) return kScriptParts;

            foreach (global::Part p in Vessel.parts)
            {
                // See if any of the parts' actions are this action group:
                bool hasPartAction = p.Actions.Any(a => a.actionGroup.Equals(matchGroup));
                if (hasPartAction)
                {
                    kScriptParts.Add(PartValueFactory.Construct(p, Shared));
                    continue;
                }

                var modules = p.Modules.Cast<PartModule>();
                bool hasModuleAction = modules.Any(pm => pm.Actions.Any(a => a.actionGroup.Equals(matchGroup)));
                if (hasModuleAction)
                {
                    kScriptParts.Add(PartValueFactory.Construct(p, Shared));
                }
            }
            return kScriptParts;
        }

        private ListValue GetModulesInGroup(StringValue groupName)
        {
            var matchGroup = KSPActionGroup.None;
            string upperName = groupName.ToUpper();

            // TODO: later refactor:  put this in a Dictionary lookup instead, and then share it
            // by both this code and the code in ActionGroup.cs:
            if (upperName == "SAS") { matchGroup = KSPActionGroup.SAS; }
            if (upperName == "GEAR") { matchGroup = KSPActionGroup.Gear; }
            if (upperName == "LIGHTS") { matchGroup = KSPActionGroup.Light; }
            if (upperName == "BRAKES") { matchGroup = KSPActionGroup.Brakes; }
            if (upperName == "RCS") { matchGroup = KSPActionGroup.RCS; }
            if (upperName == "ABORT") { matchGroup = KSPActionGroup.Abort; }
            if (upperName == "AG1") { matchGroup = KSPActionGroup.Custom01; }
            if (upperName == "AG2") { matchGroup = KSPActionGroup.Custom02; }
            if (upperName == "AG3") { matchGroup = KSPActionGroup.Custom03; }
            if (upperName == "AG4") { matchGroup = KSPActionGroup.Custom04; }
            if (upperName == "AG5") { matchGroup = KSPActionGroup.Custom05; }
            if (upperName == "AG6") { matchGroup = KSPActionGroup.Custom06; }
            if (upperName == "AG7") { matchGroup = KSPActionGroup.Custom07; }
            if (upperName == "AG8") { matchGroup = KSPActionGroup.Custom08; }
            if (upperName == "AG9") { matchGroup = KSPActionGroup.Custom09; }
            if (upperName == "AG10") { matchGroup = KSPActionGroup.Custom10; }

            ListValue kScriptParts = new ListValue();

            // This is almost identical to the logic in GetPartsInGroup and it might be a nice idea
            // later to merge them somehow:
            //
            if (matchGroup == KSPActionGroup.None) return kScriptParts;

            foreach (global::Part p in Vessel.parts)
                foreach (PartModule pm in p.Modules)
                {
                    if (pm.Actions.Any(a => a.actionGroup.Equals(matchGroup)))
                    {
                        kScriptParts.Add(PartModuleFieldsFactory.Construct(pm, Shared));
                    }
                }

            return kScriptParts;
        }

        private void InitializeSuffixes()
        {
            AddSuffix("PARTSNAMED", new OneArgsSuffix<ListValue, StringValue>(GetPartsNamed));
            AddSuffix("PARTSNAMEDPATTERN", new OneArgsSuffix<ListValue, StringValue>(GetPartsNamedPattern));
            AddSuffix("PARTSTITLED", new OneArgsSuffix<ListValue, StringValue>(GetPartsTitled));
            AddSuffix("PARTSTITLEDPATTERN", new OneArgsSuffix<ListValue, StringValue>(GetPartsTitledPattern));
            AddSuffix("PARTSDUBBED", new OneArgsSuffix<ListValue, StringValue>(GetPartsDubbed));
            AddSuffix("PARTSDUBBEDPATTERN", new OneArgsSuffix<ListValue, StringValue>(GetPartsDubbedPattern));
            AddSuffix("MODULESNAMED", new OneArgsSuffix<ListValue, StringValue>(GetModulesNamed));
            AddSuffix("PARTSINGROUP", new OneArgsSuffix<ListValue, StringValue>(GetPartsInGroup));
            AddSuffix("MODULESINGROUP", new OneArgsSuffix<ListValue, StringValue>(GetModulesInGroup));
            AddSuffix("PARTSTAGGED", new OneArgsSuffix<ListValue, StringValue>(GetPartsTagged));
            AddSuffix("PARTSTAGGEDPATTERN", new OneArgsSuffix<ListValue, StringValue>(GetPartsTaggedPattern));
            AddSuffix("ALLTAGGEDPARTS", new NoArgsSuffix<ListValue>(GetAllTaggedParts));
            AddSuffix("PARTS", new NoArgsSuffix<ListValue<PartValue>>(() => Parts));
            AddSuffix("DOCKINGPORTS", new NoArgsSuffix<ListValue<DockingPortValue>>(() => DockingPorts));
            AddSuffix(new string[] { "DECOUPLERS", "SEPARATORS" }, new NoArgsSuffix<ListValue<DecouplerValue>>(() => Decouplers));
            AddSuffix("ELEMENTS", new NoArgsSuffix<ListValue>(() => Vessel.PartList("elements", Shared)));

            AddSuffix("CONTROL", new Suffix<FlightControl>(GetFlightControl));
            AddSuffix("BEARING", new Suffix<ScalarValue>(() => VesselUtils.GetTargetBearing(CurrentVessel, Vessel)));
            AddSuffix("HEADING", new Suffix<ScalarValue>(() => VesselUtils.GetTargetHeading(CurrentVessel, Vessel)));
            AddSuffix("AVAILABLETHRUST", new Suffix<ScalarValue>(() => VesselUtils.GetAvailableThrust(Vessel)));
            AddSuffix("AVAILABLETHRUSTAT", new OneArgsSuffix<ScalarValue, ScalarValue>(GetAvailableThrustAt));
            AddSuffix("MAXTHRUST", new Suffix<ScalarValue>(() => VesselUtils.GetMaxThrust(Vessel)));
            AddSuffix("MAXTHRUSTAT", new OneArgsSuffix<ScalarValue, ScalarValue>(GetMaxThrustAt));
            AddSuffix("FACING", new Suffix<Direction>(() => VesselUtils.GetFacing(Vessel)));
            AddSuffix("ANGULARMOMENTUM", new Suffix<Vector>(() => new Vector(Vessel.angularMomentum)));
            AddSuffix("ANGULARVEL", new Suffix<Vector>(() => RawAngularVelFromRelative(Vessel.angularVelocity)));
            AddSuffix("MASS", new Suffix<ScalarValue>(() => Vessel.GetTotalMass()));
            AddSuffix("VERTICALSPEED", new Suffix<ScalarValue>(() => Vessel.verticalSpeed));
            AddSuffix("GROUNDSPEED", new Suffix<ScalarValue>(GetHorizontalSrfSpeed));
            AddSuffix("SURFACESPEED", new Suffix<ScalarValue>(() => { throw new KOSObsoletionException("0.18.0", "SURFACESPEED", "GROUNDSPEED", ""); }));
            AddSuffix("AIRSPEED", new Suffix<ScalarValue>(() => (Vessel.orbit.GetVel() - FlightGlobals.currentMainBody.getRFrmVel(Vessel.CoMD)).magnitude, "the velocity of the vessel relative to the air"));
            AddSuffix(new[] { "SHIPNAME", "NAME" }, new SetSuffix<StringValue>(() => Vessel.vesselName, RenameVessel, "The KSP name for a craft, cannot be empty"));
            AddSuffix("TYPE", new SetSuffix<StringValue>(() => Vessel.vesselType.ToString(), RetypeVessel, "The Ship's KSP type (e.g. rover, base, probe)"));
            AddSuffix("SENSORS", new Suffix<VesselSensors>(() => new VesselSensors(Vessel)));
            AddSuffix("TERMVELOCITY", new Suffix<ScalarValue>(() => { throw new KOSAtmosphereObsoletionException("17.2", "TERMVELOCITY", "<None>", string.Empty); }));
            AddSuffix(new[] { "DYNAMICPRESSURE", "Q" }, new Suffix<ScalarValue>(() => Vessel.dynamicPressurekPa * ConstantValue.KpaToAtm, "Dynamic Pressure in Atmospheres"));
            AddSuffix("LOADED", new Suffix<BooleanValue>(() => Vessel.loaded));
            AddSuffix("UNPACKED", new Suffix<BooleanValue>(() => !Vessel.packed));
            AddSuffix("ROOTPART", new Suffix<PartValue>(() => Root));
            AddSuffix("CONTROLPART", new Suffix<PartValue>(() => PartValueFactory.Construct(GetControlPart(), Shared)));
            AddSuffix("DRYMASS", new Suffix<ScalarValue>(() => Vessel.GetDryMass(), "The Ship's mass when empty"));
            AddSuffix("WETMASS", new Suffix<ScalarValue>(() => Vessel.GetWetMass(), "The Ship's mass when full"));
            AddSuffix("RESOURCES", new Suffix<ListValue<AggregateResourceValue>>(() => AggregateResourceValue.FromVessel(Vessel, Shared), "The Aggregate resources from every part on the craft"));
            AddSuffix("LOADDISTANCE", new Suffix<LoadDistanceValue>(() => new LoadDistanceValue(Vessel)));
            AddSuffix("ISDEAD", new NoArgsSuffix<BooleanValue>(() => (Vessel == null || Vessel.state == Vessel.State.DEAD)));
            AddSuffix("STATUS", new Suffix<StringValue>(() => Vessel.situation.ToString()));

            //// Although there is an implementation of lat/long/alt in Orbitible,
            //// it's better to use the methods for vessels that are faster if they're
            //// available:
            AddSuffix("LATITUDE", new Suffix<ScalarValue>(() => VesselUtils.GetVesselLatitude(Vessel)));
            AddSuffix("LONGITUDE", new Suffix<ScalarValue>(() => VesselUtils.GetVesselLongitude(Vessel)));
            AddSuffix("ALTITUDE", new Suffix<ScalarValue>(() => Vessel.altitude));
            AddSuffix("CREW", new NoArgsSuffix<ListValue>(GetCrew));
            AddSuffix("CREWCAPACITY", new NoArgsSuffix<ScalarValue>(GetCrewCapacity));
            AddSuffix("CONNECTION", new NoArgsSuffix<VesselConnection>(() => new VesselConnection(Vessel, Shared)));
            AddSuffix("MESSAGES", new NoArgsSuffix<MessageQueueStructure>(() => GetMessages()));

            AddSuffix("STARTTRACKING", new NoArgsVoidSuffix(StartTracking));

            AddSuffix("SOICHANGEWATCHERS", new NoArgsSuffix<UniqueSetValue<UserDelegate>>(() => Shared.DispatchManager.CurrentDispatcher.GetSOIChangeNotifyees(Vessel)));
        }

        public global::Part GetControlPart()
        {
            global::Part res = Vessel.GetReferenceTransformPart(); //this can actually be null
            if (res != null) { return res; }
            else { return Vessel.rootPart; } //the root part is used as reference in that case
        }

        public ScalarValue GetCrewCapacity()
        {
            return Vessel.GetCrewCapacity();
        }

        public ListValue GetCrew()
        {
            var crew = new ListValue();

            foreach (var crewMember in Vessel.GetVesselCrew())
            {
                crew.Add(new CrewMember(crewMember, Shared));
            }

            return crew;
        }

        public MessageQueueStructure GetMessages()
        {
            if (Shared.Vessel.id != Vessel.id)
            {
                throw new KOSWrongCPUVesselException("MESSAGES");
            }

            return InterVesselManager.Instance.GetQueue(Shared.Vessel, Shared);
        }

        public void ThrowIfNotCPUVessel()
        {
            if (this.Vessel.id != Shared.Vessel.id)
                throw new KOSWrongCPUVesselException();
        }

        public FlightControl GetFlightControl()
        {
            ThrowIfNotCPUVessel();
            var flightControl = kOSVesselModule.GetInstance(Shared.Vessel).GetFlightControlParameter("flightcontrol") as FlightControl;
            return flightControl;
        }

        public ScalarValue GetAvailableThrustAt(ScalarValue atmPressure)
        {
            return VesselUtils.GetAvailableThrust(Vessel, atmPressure);
        }

        public ScalarValue GetMaxThrustAt(ScalarValue atmPressure)
        {
            return VesselUtils.GetMaxThrust(Vessel, atmPressure);
        }

        private void RetypeVessel(StringValue value)
        {
            Vessel.vesselType = value.ToString().ToEnum<VesselType>();
        }

        private void RenameVessel(StringValue value)
        {
            if (Vessel.IsValidVesselName(value))
            {
                Vessel.vesselName = value;
            }
        }

        private ScalarValue GetHorizontalSrfSpeed()
        {
            // NOTE: THIS Function replaces the functionality of the
            // single KSP API CALL:
            //       Vessel.horizontalSrfSpeed;
            // Which broke in KSP 1.0.3, badly, so we're just going to
            // calculate it manually instead.

            // The logic, shamefully copied from the Kerbal Engineer mod,
            // which had the same problem, is this:
            //    Run the Pythagorean Theorem slightly backward.
            //    If we know that:
            //        srfspd == sqrt( a^2 + b^2 + c^2).
            //    And we want to get what the speed would be if dimension C was excluded so it was projected
            //    into the plane of just the a and b components, we can do this:
            //        srfspd^2 == a^2 + b^2 + c^2.
            //    solve for (a^2+b^2):
            //        srfspd^2 - c^2 == a^2 + b^2.
            //    We know that, in just the two dimensions:
            //        speed_2D = sqrt(a^2+b^2).
            //    Therefore:
            //        speed_2D = sqrt(srfspd^2 - c^2)
            //    Since C in our case is the vertical speed we want to remove, we get the following formula:

            double squared2DSpeed = Vessel.srfSpeed * Vessel.srfSpeed - Vessel.verticalSpeed * Vessel.verticalSpeed;

            // Due to floating point roundoff errrors in the KSP API, the above expression can sometimes come
            // out slightly negative when it should be nearly zero.  (i.e. -0.0000012).  The Sqrt() would
            // return NaN for such a case, so it needs to be protected from ever going negative like so:
            if (squared2DSpeed < 0)
                squared2DSpeed = 0;

            return System.Math.Sqrt(squared2DSpeed);
        }

        /// <summary>
        /// Annoyingly, KSP returns vessel.angularVelociy in a frame of reference
        /// relative to the ship facing instead of the universe facing.  This would be
        /// wonderful if that was their philosophy everywhere, but it's not - its just a
        /// weird exception for this one case.  This transforms it back into raw universe
        /// axes again:
        /// </summary>
        /// <param name="angularVelFromKSP">the value KSP is returning for angular velocity</param>
        /// <returns>altered velocity in the new reference frame</returns>
        private Vector RawAngularVelFromRelative(Vector3 angularVelFromKSP)
        {
            return new Vector(VesselUtils.GetFacing(Vessel).Rotation *
                              new Vector3d(angularVelFromKSP.x, -angularVelFromKSP.z, angularVelFromKSP.y));
        }

        private void StartTracking()
        {
            if (Vessel != null)
            {
                if (!Vessel.DiscoveryInfo.HaveKnowledgeAbout(DiscoveryLevels.Appearance))
                {
                    KSP.UI.Screens.SpaceTracking.StartTrackingObject(Vessel);
                }
            }
        }

        public override ISuffixResult GetSuffix(string suffixName)
        {
            // Most suffixes are handled by the newer AddSuffix system, except for the
            // resource levels, which have to use this older technique as a fallback because
            // the AddSuffix system doesn't support this type of late-binding string matching:

            // Is this a resource?
            double dblValue;
            if (VesselUtils.TryGetResource(Vessel, suffixName, out dblValue))
            {
                return new SuffixResult(ScalarValue.Create(dblValue));
            }

            return base.GetSuffix(suffixName);
        }

        protected bool Equals(VesselTarget other)
        {
            return Vessel.Equals(other.Vessel);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((VesselTarget)obj);
        }

        public override int GetHashCode()
        {
            return Vessel.rootPart.flightID.GetHashCode();
        }

        public static bool operator ==(VesselTarget left, VesselTarget right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(VesselTarget left, VesselTarget right)
        {
            return !Equals(left, right);
        }

        public override Dump Dump()
        {
            DumpWithHeader dump = new DumpWithHeader();

            dump.Header = "VESSEL '" + Vessel.vesselName + "'";

            dump.Add(DumpGuid, Vessel.id.ToString());

            return dump;
        }

        public override void LoadDump(Dump dump)
        {
            string guid = dump[DumpGuid] as string;

            if (guid == null)
            {
                throw new KOSSerializationException("Vessel's guid is null or invalid");
            }

            Vessel vessel = FlightGlobals.Vessels.Find((v) => v.id.ToString().Equals(guid));

            if (vessel == null)
            {
                throw new KOSSerializationException("Vessel with the given id does not exist");
            }

            Vessel = vessel;
        }

        public void ScopeLost()
        {
            UnhookEvents();
        }

        // The data that identifies a unique instance of this class, for use
        // with the factory method that avoids duplicate instances:
        private struct InstanceKey
        {
            /// <summary>The kOSProcessor Module that built me.</summary>
            public int ProcessorId { get; set; }

            /// <summary>The KSP vessel object that I'm wrapping.</summary>
            public Guid VesselId { get; set; }
        }
    }
}
