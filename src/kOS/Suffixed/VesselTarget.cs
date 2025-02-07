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
using kOS.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace kOS.Suffixed
{
    // See VesselTarget.Hooks.cs for Vessel (property), event hooks,
    // instance cache, creation and destruction

    // See VesselTarget.Parts.cs for parts, part cache, part lists and StageValues.

    [kOS.Safe.Utilities.KOSNomenclature("Vessel")]
    public partial class VesselTarget : Orbitable, IKOSTargetable, IDisposable
    {
        private static string DumpGuid = "guid";
        public Guid Guid { get { return Vessel.id; } }

        public override Orbit Orbit { get { return Vessel.orbit; } }
        public override StringValue GetName()
        {
            return Vessel.vesselName;
        }
        public override Vector GetPosition()
        {
            return new Vector(GetPositionInternal());
        }

        private Vector3d GetPositionInternal()
        {
            return Vessel.CoMD - CurrentVessel.CoMD + GetPositionError();
        }

        public Vector3d GetPositionError()
        {
            Vector3d positionError = Vector3d.zero;
            // Workaround to fix a KSP bug:
            // When the target vessel is packed KSP returns the position where it's going to be
            // in the next simulation frame instead of the position where it is now.
            // To work around this issue the velocity of the target vessel is integrated over one frame
            // to calculate the corrent position in the current simulation frame.

            // normal time or physics warp
            bool usingPhysics = TimeWarp.CurrentRate == 1f || TimeWarp.WarpMode == TimeWarp.Modes.LOW;

            if (Vessel.loaded && Vessel.packed && usingPhysics && CurrentVessel.isActiveVessel)
            {
                // If the body is in inverse rotation mode (i.e. the world axis are fixed to the body surface) the surface velocity is used
                // because the position reported by KSP is accounting for the frame of reference rotation
                Vector3d velocity = CurrentVessel.mainBody.inverseRotation ? Vessel.srf_velocity : Vessel.obt_velocity;
                // Calculate the current position by integrating the velocity vector over one frame and subtracting that from the reported position
                positionError = -velocity * TimeWarp.fixedDeltaTime;
            }

            return positionError;
        }

        public override OrbitableVelocity GetVelocities()
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
        public override Vector GetPositionAtUT(TimeStamp timeStamp)
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
        public override OrbitableVelocity GetVelocitiesAtUT(TimeStamp timeStamp)
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

        private Vessel CurrentVessel { get { return Shared.Vessel; } }
        public ITargetable Target { get { return Vessel; } }

        // TODO: We will need to replace with the same thing Orbitable:DISTANCE does
        // in order to implement the orbit solver later.
        public ScalarValue GetDistance()
        {
            return GetPositionInternal().magnitude;
        }

        public static string[] ShortCuttableShipSuffixes { get; private set; }

        static VesselTarget()
        {
            ShortCuttableShipSuffixes = new[]
            {
                "HEADING", "PROGRADE", "RETROGRADE", "FACING", "MAXTHRUST", "AVAILABLETHRUST", "VELOCITY", "GEOPOSITION", "LATITUDE",
                "LONGITUDE",
                "UP", "NORTH", "BODY", "ANGULARMOMENTUM", "ANGULARVEL", "MASS", "VERTICALSPEED", "SURFACESPEED", "GROUNDSPEED",
                "AIRSPEED", "SHIPNAME",
                "ALTITUDE", "APOAPSIS", "PERIAPSIS", "SENSOR", "SRFPROGRADE", "SRFRETROGRADE"
            };
        }

        public override string ToString()
        {
            return "VESSEL(\"" + Vessel.vesselName + "\")";
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
            AddSuffix("PARTS", new NoArgsSuffix<ListValue>(() => Parts));
            AddSuffix("DOCKINGPORTS", new NoArgsSuffix<ListValue>(() => DockingPorts));
            AddSuffix(new string[] { "DECOUPLERS", "SEPARATORS" }, new NoArgsSuffix<ListValue>(() => Decouplers));
            AddSuffix("ELEMENTS", new NoArgsSuffix<ListValue>(() => Vessel.PartList("elements", Shared)));
            
            AddSuffix("ENGINES", new NoArgsSuffix<ListValue>(() => Vessel.PartList("engines", Shared)));
            AddSuffix("RCS", new NoArgsSuffix<ListValue>(() => Vessel.PartList("rcs", Shared)));

            AddSuffix("CONTROL", new Suffix<FlightControl>(GetFlightControl));
            AddSuffix("BEARING", new Suffix<ScalarValue>(() => VesselUtils.GetTargetBearing(CurrentVessel, Vessel)));
            AddSuffix("HEADING", new Suffix<ScalarValue>(() => VesselUtils.GetTargetHeading(CurrentVessel, Vessel)));
            AddSuffix("THRUST", new Suffix<ScalarValue>(() => VesselUtils.GetCurrentThrust(Vessel)));
            AddSuffix("AVAILABLETHRUST", new Suffix<ScalarValue>(() => VesselUtils.GetAvailableThrust(Vessel)));
            AddSuffix("AVAILABLETHRUSTAT", new OneArgsSuffix<ScalarValue, ScalarValue>(GetAvailableThrustAt));
            AddSuffix("MAXTHRUST", new Suffix<ScalarValue>(() => VesselUtils.GetMaxThrust(Vessel)));
            AddSuffix("MAXTHRUSTAT", new OneArgsSuffix<ScalarValue, ScalarValue>(GetMaxThrustAt));
            AddSuffix("FACING", new Suffix<Direction>(() => VesselUtils.GetFacing(Vessel)));
            AddSuffix("BOUNDS", new Suffix<BoundsValue>(() => GetBoundsValue()));
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
            AddSuffix("RESOURCES", new Suffix<ListValue>(() => AggregateResourceValue.FromVessel(Vessel, Shared), "The Aggregate resources from every part on the craft"));
            AddSuffix("LOADDISTANCE", new Suffix<LoadDistanceValue>(() => new LoadDistanceValue(Vessel)));
            AddSuffix("ISDEAD", new NoArgsSuffix<BooleanValue>(() => (Vessel == null || Vessel.state == Vessel.State.DEAD)));
            AddSuffix("STATUS", new Suffix<StringValue>(() => Vessel.situation.ToString()));

            AddSuffix("DELTAV", new Suffix<DeltaVCalc>(() => new DeltaVCalc(Shared, Vessel.VesselDeltaV)));
            AddSuffix("STAGEDELTAV", new OneArgsSuffix<DeltaVCalc, ScalarValue>(GetStageDV));
            AddSuffix("STAGENUM", new Suffix<ScalarValue>(() => Vessel.currentStage));

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
            AddSuffix("STOPTRACKING", new NoArgsVoidSuffix(StopTracking));
            AddSuffix("SIZECLASS", new Suffix<StringValue>(GetSizeClass));

            AddSuffix("SOICHANGEWATCHERS", new NoArgsSuffix<UniqueSetValue<UserDelegate>>(() => Shared.DispatchManager.CurrentDispatcher.GetSOIChangeNotifyees(Vessel)));

#if DEBUG
            AddSuffix("POSITIONERROR", new Suffix<Vector>(() => new Vector(GetPositionError())));
#endif
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

        public BoundsValue GetBoundsValue()
        {
            Direction myFacing = VesselUtils.GetFacing(Vessel);
            Quaternion inverseMyFacing = Quaternion.Inverse(myFacing.Rotation);
            Vector rootOrigin = ((PartValue)Parts[0]).GetPosition();
            Bounds unionBounds = new Bounds();
            for (int pNum = 0; pNum < Parts.Count; ++pNum)
            {
                PartValue p = (PartValue)Parts[pNum];
                Vector partOriginOffsetInVesselBounds = p.GetPosition() - rootOrigin;
                Bounds b = p.GetBoundsValue().GetUnityBounds();
                Vector partCenter = new Vector(b.center);

                // Just like the logic for the part needing all 8 corners of the mesh's bounds,
                // this needs all 8 corners of the part bounds:
                for (int signX = -1; signX <= 1; signX += 2)
                    for (int signY = -1; signY <= 1; signY += 2)
                        for (int signZ = -1; signZ <= 1; signZ += 2)
                        {
                            Vector corner = partCenter + new Vector(signX * b.extents.x, signY * b.extents.y, signZ * b.extents.z);
                            Vector worldCorner = partOriginOffsetInVesselBounds + p.GetFacing() * corner;
                            Vector3 vesselCorner = inverseMyFacing * worldCorner.ToVector3();

                            unionBounds.Encapsulate(vesselCorner);
                        }
            }

            Vector min = new Vector(unionBounds.min);
            Vector max = new Vector(unionBounds.max);

            // The above operation is expensive and should force the CPU to do a WAIT 0:
            Shared.Cpu.YieldProgram(new YieldFinishedNextTick());

            return new BoundsValue(min, max, delegate { return ((PartValue)Parts[0]).GetPosition(); }, delegate { return VesselUtils.GetFacing(Vessel); }, Shared);
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

        public DeltaVCalc GetStageDV(ScalarValue stageNum)
        {
            int clampedStageNum = Math.Min(Vessel.currentStage, Math.Max(0, (int)stageNum.ToPrimitive()));

            return new DeltaVCalc(Shared, Vessel.VesselDeltaV.GetStage(clampedStageNum));
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

        private void StopTracking()
        {
            if (Vessel != null)
            {
                if (Vessel.DiscoveryInfo.HaveKnowledgeAbout(DiscoveryLevels.Appearance))
                {
                    KSP.UI.Screens.SpaceTracking.StopTrackingObject(Vessel);
                }
            }
        }

        private StringValue GetSizeClass()
        {
            if (Vessel.vesselType == VesselType.SpaceObject)
            {
                if (Vessel.DiscoveryInfo.HaveKnowledgeAbout(DiscoveryLevels.Presence))
                {
                    return Vessel.DiscoveryInfo.objectSize.ToString();
                }
                else
                {
                    return "UNKNOWN";
                }
            }
            else
            {
                return Vessel.vesselType.ToString();
            }
        }

        public override ISuffixResult GetSuffix(string suffixName, bool failOkay = false)
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

            return base.GetSuffix(suffixName, failOkay);
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
            return Vessel.id.GetHashCode();
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
            Vessel = VesselFromDump(dump);
        }

        private static Vessel VesselFromDump(Dump dump)
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

            return vessel;
        }
    }
}
