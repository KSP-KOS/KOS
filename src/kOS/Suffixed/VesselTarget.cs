using kOS.Binding;
using kOS.Module;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;
using kOS.Suffixed.Part;
using kOS.Suffixed.PartModuleField;
using kOS.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace kOS.Suffixed
{
    public class VesselTarget : Orbitable, IKOSTargetable
    {
        override public Orbit Orbit { get { return Vessel.orbit; } }

        override public string GetName()
        {
            return Vessel.vesselName;
        }

        override public Vector GetPosition()
        {
            return new Vector(Vessel.findWorldCenterOfMass() - CurrentVessel.findWorldCenterOfMass());
        }

        override public OrbitableVelocity GetVelocities()
        {
            return new OrbitableVelocity(Vessel);
        }

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
        override public Vector GetPositionAtUT(TimeSpan timeStamp)
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

            return new Vector(pos - Shared.Vessel.findWorldCenterOfMass()); // Convert to ship-centered frame.
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
        override public OrbitableVelocity GetVelocitiesAtUT(TimeSpan timeStamp)
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
                surfVel = new Vector(orbVel - parent.getRFrmVel(pos + Shared.Vessel.findWorldCenterOfMass()));
            }
            else
                surfVel = new Vector(orbVel.x, orbVel.y, orbVel.z);

            return new OrbitableVelocity(new Vector(orbVel), surfVel);
        }

        override public Vector GetUpVector()
        {
            return new Vector(Vessel.upAxis);
        }

        override public Vector GetNorthVector()
        {
            return new Vector(VesselUtils.GetNorthVector(Vessel));
        }

        /// <summary>
        ///   Calculate which orbit patch contains the timestamp given.
        /// </summary>
        /// <param name="desiredUT">The timestamp to look for</param>
        /// <returns>the orbit patch the vessel is expected to be in at the given time.</returns>
        override public Orbit GetOrbitAtUT(double desiredUT)
        {
            // After much trial and error this seems to be the only way to do this:

            // Find the lastmost maneuver node that occurs prior to timestamp:
            List<ManeuverNode> nodes = Vessel.patchedConicSolver.maneuverNodes;
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
            while (!(orbitPatch.StartUT < desiredUT && desiredUT < orbitPatch.EndUT))
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

        static VesselTarget()
        {
            // TODO: These need to be refactored into the new suffix system at some point:

            ShortCuttableShipSuffixes = new[]
                {
                    "HEADING", "PROGRADE", "RETROGRADE", "FACING", "MAXTHRUST", "AVAILABLETHRUST", "VELOCITY", "GEOPOSITION", "LATITUDE",
                    "LONGITUDE",
                    "UP", "NORTH", "BODY", "ANGULARMOMENTUM", "ANGULARVEL", "MASS", "VERTICALSPEED", "SURFACESPEED",
                    "AIRSPEED", "VESSELNAME", "SHIPNAME",
                    "ALTITUDE", "APOAPSIS", "PERIAPSIS", "SENSOR", "SRFPROGRADE", "SRFRETROGRADE"
                };
        }

        public VesselTarget(Vessel target, SharedObjects shared)
            : base(shared)
        {
            Vessel = target;
            InitializeSuffixes();
        }

        public VesselTarget(SharedObjects shared)
            : this(shared.Vessel, shared)
        {
        }

        private Vessel CurrentVessel { get { return Shared.Vessel; } }

        public ITargetable Target
        {
            get { return Vessel; }
        }

        // TODO: We will need to replace with the same thing Orbitable:DISTANCE does
        // in order to implement the orbit solver later.
        public double GetDistance()
        {
            return Vector3d.Distance(CurrentVessel.findWorldCenterOfMass(), Vessel.findWorldCenterOfMass());
        }

        public Vessel Vessel { get; private set; }

        public static string[] ShortCuttableShipSuffixes { get; private set; }

        public override string ToString()
        {
            return "SHIP(\"" + Vessel.vesselName + "\")";
        }

        public ListValue GetAllParts()
        {
            return PartValueFactory.Construct(Vessel.Parts, Shared);
        }

        private ListValue GetPartsDubbed(string searchTerm)
        {
            // Get the list of all the parts where the part's API name OR its GUI title or its tag name matches.
            List<global::Part> kspParts = new List<global::Part>();
            kspParts.AddRange(GetRawPartsNamed(searchTerm));
            kspParts.AddRange(GetRawPartsTitled(searchTerm));
            kspParts.AddRange(GetRawPartsTagged(searchTerm));

            // The "Distinct" operation is there because it's possible for someone to use a tag name that matches the part name.
            return PartValueFactory.Construct(kspParts.Distinct(), Shared);
        }

        private ListValue GetPartsNamed(string partName)
        {
            return PartValueFactory.Construct(GetRawPartsNamed(partName), Shared);
        }

        private IEnumerable<global::Part> GetRawPartsNamed(string partName)
        {
            // Get the list of all the parts where the part's KSP API title matches:
            return Vessel.parts.FindAll(
                part => String.Equals(part.name, partName, StringComparison.CurrentCultureIgnoreCase));
        }

        private ListValue GetPartsTitled(string partTitle)
        {
            return PartValueFactory.Construct(GetRawPartsTitled(partTitle), Shared);
        }

        private IEnumerable<global::Part> GetRawPartsTitled(string partTitle)
        {
            // Get the list of all the parts where the part's GUI title matches:
            return Vessel.parts.FindAll(
                part => String.Equals(part.partInfo.title, partTitle, StringComparison.CurrentCultureIgnoreCase));
        }

        private ListValue GetPartsTagged(string tagName)
        {
            return PartValueFactory.Construct(GetRawPartsTagged(tagName), Shared);
        }

        private IEnumerable<global::Part> GetRawPartsTagged(string tagName)
        {
            return Vessel.parts
                .Where(p => p.Modules.OfType<KOSNameTag>()
                .Any(tag => String.Equals(tag.nameTag, tagName, StringComparison.CurrentCultureIgnoreCase)));
        }

        /// <summary>
        /// Get all the parts which have at least SOME non-default name:
        /// </summary>
        /// <returns></returns>
        private ListValue GetAllTaggedParts()
        {
            IEnumerable<global::Part> partsWithName = Vessel.parts
                .Where(p => p.Modules.OfType<KOSNameTag>()
                .Any(tag => !(String.Equals(tag.nameTag, "", StringComparison.CurrentCultureIgnoreCase))));

            return PartValueFactory.Construct(partsWithName, Shared);
        }

        private ListValue GetModulesNamed(string modName)
        {
            // This is slow - maybe there should be a faster lookup string hash, but
            // KSP's data model seems to have not implemented it:
            IEnumerable<PartModule> modules = Vessel.parts
                .SelectMany(p => p.Modules.Cast<PartModule>()
                .Where(pMod => String.Equals(pMod.moduleName, modName, StringComparison.CurrentCultureIgnoreCase)));

            return PartModuleFieldsFactory.Construct(modules, Shared);
        }

        private ListValue GetPartsInGroup(string groupName)
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

        private ListValue GetModulesInGroup(string groupName)
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
            AddSuffix("PARTSNAMED", new OneArgsSuffix<ListValue, string>(GetPartsNamed));
            AddSuffix("PARTSTITLED", new OneArgsSuffix<ListValue, string>(GetPartsTitled));
            AddSuffix("PARTSDUBBED", new OneArgsSuffix<ListValue, string>(GetPartsDubbed));
            AddSuffix("MODULESNAMED", new OneArgsSuffix<ListValue, string>(GetModulesNamed));
            AddSuffix("PARTSINGROUP", new OneArgsSuffix<ListValue, string>(GetPartsInGroup));
            AddSuffix("MODULESINGROUP", new OneArgsSuffix<ListValue, string>(GetModulesInGroup));
            AddSuffix("PARTSTAGGED", new OneArgsSuffix<ListValue, string>(GetPartsTagged));
            AddSuffix("ALLTAGGEDPARTS", new NoArgsSuffix<ListValue>(GetAllTaggedParts));
            AddSuffix("PARTS", new NoArgsSuffix<ListValue>(GetAllParts));
            AddSuffix("DOCKINGPORTS", new NoArgsSuffix<ListValue>(() => Vessel.PartList("dockingports", Shared)));
            AddSuffix("ELEMENTS", new NoArgsSuffix<ListValue>(() => Vessel.PartList("elements", Shared)));

            AddSuffix("CONTROL", new Suffix<FlightControl>(() => FlightControlManager.GetControllerByVessel(Vessel)));
            AddSuffix("BEARING", new Suffix<float>(() => VesselUtils.GetTargetBearing(CurrentVessel, Vessel)));
            AddSuffix("HEADING", new Suffix<float>(() => VesselUtils.GetTargetHeading(CurrentVessel, Vessel)));
            AddSuffix("AVAILABLETHRUST", new Suffix<double>(() => VesselUtils.GetAvailableThrust(Vessel)));
            AddSuffix("AVAILABLETHRUSTAT", new OneArgsSuffix<double, double>(GetAvailableThrustAt));
            AddSuffix("MAXTHRUST", new Suffix<double>(() => VesselUtils.GetMaxThrust(Vessel)));
            AddSuffix("MAXTHRUSTAT", new OneArgsSuffix<double, double>(GetMaxThrustAt));
            AddSuffix("FACING", new Suffix<Direction>(() => VesselUtils.GetFacing(Vessel)));
            AddSuffix("ANGULARMOMENTUM", new Suffix<Vector>(() => new Vector(Vessel.angularMomentum)));
            AddSuffix("ANGULARVEL", new Suffix<Vector>(() => RawAngularVelFromRelative(Vessel.angularVelocity)));
            AddSuffix("MASS", new Suffix<float>(() => Vessel.GetTotalMass()));
            AddSuffix("VERTICALSPEED", new Suffix<double>(() => Vessel.verticalSpeed));
            AddSuffix("SURFACESPEED", new Suffix<double>(() => Vessel.horizontalSrfSpeed));
            AddSuffix("AIRSPEED", new Suffix<double>(() => (Vessel.orbit.GetVel() - FlightGlobals.currentMainBody.getRFrmVel(Vessel.findWorldCenterOfMass())).magnitude, "the velocity of the vessel relative to the air"));
            AddSuffix(new[] { "SHIPNAME", "NAME" }, new SetSuffix<string>(() => Vessel.vesselName, RenameVessel, "The KSP name for a craft, cannot be empty"));
            AddSuffix("TYPE", new SetSuffix<string>(() => Vessel.vesselType.ToString(), RetypeVessel, "The Ship's KSP type (e.g. rover, base, probe)"));
            AddSuffix("SENSORS", new Suffix<VesselSensors>(() => new VesselSensors(Vessel)));
            AddSuffix("TERMVELOCITY", new Suffix<double>(() => { throw new KOSAtmosphereDeprecationException("17.2", "TERMVELOCITY", "<None>", string.Empty); }));
            AddSuffix("LOADED", new Suffix<bool>(() => Vessel.loaded));
            AddSuffix("ROOTPART", new Suffix<PartValue>(() => PartValueFactory.Construct(Vessel.rootPart, Shared)));
            AddSuffix("DRYMASS", new Suffix<float>(() => Vessel.GetDryMass(), "The Ship's mass when empty"));
            AddSuffix("WETMASS", new Suffix<float>(Vessel.GetWetMass, "The Ship's mass when full"));
            AddSuffix("RESOURCES", new Suffix<ListValue<AggregateResourceValue>>(() => AggregateResourceValue.FromVessel(Vessel, Shared), "The Aggregate resources from every part on the craft"));
            AddSuffix("PACKDISTANCE", new SetSuffix<float>(
                () =>
                {
                    return System.Math.Min(Vessel.vesselRanges.landed.pack, Vessel.vesselRanges.prelaunch.pack);
                },
                value =>
                {
                    Vessel.vesselRanges.landed.pack = value;
                    Vessel.vesselRanges.splashed.pack = value;
                    Vessel.vesselRanges.prelaunch.pack = value;
                }));
            AddSuffix("ISDEAD", new NoArgsSuffix<bool>(() => (Vessel.state == Vessel.State.DEAD)));
            AddSuffix("STATUS", new Suffix<String>(() => Vessel.situation.ToString()));

            //// Although there is an implementation of lat/long/alt in Orbitible,
            //// it's better to use the methods for vessels that are faster if they're
            //// available:
            AddSuffix("LATITUDE", new Suffix<float>(() => VesselUtils.GetVesselLatitude(Vessel)));
            AddSuffix("LONGITUDE", new Suffix<double>(() => VesselUtils.GetVesselLongitude(Vessel)));
            AddSuffix("ALTITUDE", new Suffix<double>(() => Vessel.altitude));
        }

        public double GetAvailableThrustAt(double atmPressure)
        {
            return VesselUtils.GetAvailableThrust(Vessel, atmPressure);
        }

        public double GetMaxThrustAt(double atmPressure)
        {
            return VesselUtils.GetMaxThrust(Vessel, atmPressure);
        }

        private void RetypeVessel(string value)
        {
            Vessel.vesselType = value.ToEnum<VesselType>();
        }

        private void RenameVessel(string value)
        {
            if (Vessel.IsValidVesselName(value))
            {
                Vessel.vesselName = value;
            }
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

        public override object GetSuffix(string suffixName)
        {
            // Most suffixes are handled by the newer AddSuffix system, except for the
            // resource levels, which have to use this older technique as a fallback because
            // the AddSuffix system doesn't support this type of late-binding string matching:

            // Is this a resource?
            double dblValue;
            if (VesselUtils.TryGetResource(Vessel, suffixName, out dblValue))
            {
                return dblValue;
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
    }
}