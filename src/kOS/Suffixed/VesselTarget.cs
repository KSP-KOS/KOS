using UnityEngine;
using kOS.Binding;
using kOS.Safe.Exceptions;
using kOS.Utilities;
using System.Collections.Generic;

namespace kOS.Suffixed
{
    public class VesselTarget : Orbitable
    {
        override public Orbit Orbit { get{return Vessel.orbit;} }

        override public string GetName()
        {
            return Vessel.vesselName;
        }

        override public Vector GetPosition()
        {
            return new Vector( Vessel.findWorldCenterOfMass() - CurrentVessel.findWorldCenterOfMass() );
        }

        override public OrbitableVelocity GetVelocities()
        {
            return new OrbitableVelocity(Vessel);
        }
        
        /// <summary>
        ///   Calculates the position of this vessel at some future universal timestamp,
        ///   taking into account all currently predicted SOI transition patches, and also
        ///   assuming that all the planned manuever nodes will actually be executed precisely
        ///   as planned.  Note that this cannot "see" into the future any farther than the
        ///   KSP orbit patches setting allows for.
        /// </summary>
        /// <param name="timeStamp">The time to predict for.  Although the intention is to
        ///   predict for a future time, it could be used to predict for a past time.</param>
        /// <returns>The position as a user-readable Vector in Shared.Vessel-origin raw rotation coordinates.</returns>
        override public Vector GetPositionAtUT(TimeSpan timeStamp)
        {
            double desiredUT = timeStamp.ToUnixStyleTime();

            Orbit patch = GetOrbitAtUT( desiredUT );
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

            return new Vector( pos - Shared.Vessel.findWorldCenterOfMass() ); // Convert to ship-centered frame.
        }

        /// <summary>
        ///   Calculates the velocities of this vessel at some future universal timestamp,
        ///   taking into account all currently predicted SOI transition patches, and also
        ///   assuming that all the planned manuever nodes will actually be executed precisely
        ///   as planned.  Note that this cannot "see" into the future any farther than the
        ///   KSP orbit patches setting allows for.
        /// </summary>
        /// <param name="timeStamp">The time to predict for.  Although the intention is to
        ///   predict for a future time, it could be used to predict for a past time.</param>
        /// <returns>The orbit/surface velocity pair as a user-readable Vector in raw rotation coordinates.</returns>
        override public OrbitableVelocity GetVelocitiesAtUT(TimeSpan timeStamp)
        {
            double desiredUT = timeStamp.ToUnixStyleTime();

            Orbit patch = GetOrbitAtUT( desiredUT );
                        
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
            orbVel = new Vector3d( orbVel.x, orbVel.z, orbVel.y );
            

            CelestialBody parent = patch.referenceBody;
            Vector surfVel;
            if (parent != null)
            {
                Vector3d pos = GetPositionAtUT( timeStamp ).ToVector3D();
                surfVel = new Vector( orbVel - parent.getRFrmVel( pos + Shared.Vessel.findWorldCenterOfMass()) );
            }
            else
                surfVel = new Vector( orbVel.x, orbVel.y, orbVel.z );

            return new OrbitableVelocity( new Vector(orbVel), surfVel );
        }
        
        override public Vector GetUpVector()
        {
            return new Vector( Vessel.upAxis );
        }

        override public Vector GetNorthVector()
        {
            return new Vector( VesselUtils.GetNorthVector(Vessel) );
        }
        
        /// <summary>
        ///   Calcualte which orbit patch contains the timestamp given.
        /// </summary>
        /// <param name="desiredUT">The timestamp to look for</param>
        /// <returns>the orbit patch the vessel is expected to be in at the given time.</returns>
        override public Orbit GetOrbitAtUT(double desiredUT)
        {            
            // After much trial and error this seems to be the only way to do this:

            // Find the lastmost manuever node that occurs prior to timestamp:
            List<ManeuverNode> nodes = Vessel.patchedConicSolver.maneuverNodes;
            Orbit orbitPatch = Vessel.orbit;
            for (int nodeIndex = 0 ; nodeIndex < nodes.Count && nodes[nodeIndex].UT <= desiredUT ; ++nodeIndex)
            {
                orbitPatch = nodes[nodeIndex].nextPatch; // the orbit patch that starts with this node.
            }
            
            // Walk the orbit patch list from there looking for the lastmost orbit patch that
            // contains this timestamp, or if this timestamp is later than the end of the last
            // patch, then just return the last patch (this can happen because a patches' EndUT
            // is one period of time and we might be predicting for a point in time more than one
            // period into the future.)
            while ( !( orbitPatch.StartUT < desiredUT && desiredUT < orbitPatch.EndUT ) )
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
            ShortCuttableShipSuffixes = new[]
                {
                    "HEADING", "PROGRADE", "RETROGRADE", "FACING", "MAXTHRUST", "VELOCITY", "GEOPOSITION", "LATITUDE",
                    "LONGITUDE",
                    "UP", "NORTH", "BODY", "ANGULARMOMENTUM", "ANGULARVEL", "MASS", "VERTICALSPEED", "SURFACESPEED",
                    "AIRSPEED", "VESSELNAME", "SHIPNAME",
                    "ALTITUDE", "APOAPSIS", "PERIAPSIS", "SENSOR", "SRFPROGRADE", "SRFRETROGRADE"
                };
        }

        public VesselTarget(Vessel target, SharedObjects shared) :base(shared)
        {
            Vessel = target;
        }

        public VesselTarget(SharedObjects shared) : this(shared.Vessel, shared) { }

        public Vessel CurrentVessel { get { return Shared.Vessel; } }

        public ITargetable Target
        {
            get { return Vessel; }
        }

        public Vessel Vessel { get; private set; }

        public static string[] ShortCuttableShipSuffixes { get; private set; }


        public bool IsInRange(double range)
        {
            return GetDistance() <= range;
        }

        // TODO: We will need to replace with the same thing Orbitable:DISTANCE does
        // in order to implement the orbit solver later.
        public double GetDistance()
        {
            return Vector3d.Distance(CurrentVessel.findWorldCenterOfMass(), Vessel.findWorldCenterOfMass());
        }

        public override string ToString()
        {
            return "SHIP(\"" + Vessel.vesselName + "\")";
        }

        public Direction GetFacing()
        {
            var vesselRotation = Vessel.ReferenceTransform.rotation;
            Quaternion vesselFacing = Quaternion.Inverse(Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(vesselRotation) * Quaternion.identity);
            return new Direction(vesselFacing);
        }

        public override bool SetSuffix(string suffixName, object value)
        {
            switch (suffixName)
            {
                case "PACKDISTANCE":
                    var distance = (float) value;
                    Vessel.distanceLandedPackThreshold = distance;
                    Vessel.distancePackThreshold = distance;
                    return true;
            }

            return base.SetSuffix(suffixName, value);
        }

        public override object GetSuffix(string suffixName)
        {
            switch (suffixName)
            {
                case "CONTROL":
                    return FlightControlManager.GetControllerByVessel(Vessel);
                case "BEARING":
                    return VesselUtils.GetTargetBearing(CurrentVessel, Vessel);
                case "HEADING":
                    return VesselUtils.GetTargetHeading(CurrentVessel, Vessel);
                case "MAXTHRUST":
                    return VesselUtils.GetMaxThrust(Vessel);
                case "FACING":
                    return GetFacing();
                case "ANGULARMOMENTUM":
                    return new Direction(Vessel.angularMomentum, true);
                case "ANGULARVEL":
                    return new Direction(Vessel.angularVelocity, true);
                case "MASS":
                    return Vessel.GetTotalMass();
                case "VERTICALSPEED":
                    return Vessel.verticalSpeed;
                case "SURFACESPEED":
                    return Vessel.horizontalSrfSpeed;
                case "AIRSPEED":
                    return
                        (Vessel.orbit.GetVel() - FlightGlobals.currentMainBody.getRFrmVel(Vessel.findWorldCenterOfMass()))
                            .magnitude; //the velocity of the vessel relative to the air);
                //DEPRICATED VESSELNAME
                case "VESSELNAME":
                    throw new KOSException("VESSELNAME is DEPRICATED, use SHIPNAME.");
                case "SHIPNAME":
                    return Vessel.vesselName;

                // Although there is an implementation of lat/long/alt in Orbitible,
                // it's better to use the methods for vessels that are faster if they're
                // available:
                case "LATITUDE":
                    return VesselUtils.GetVesselLattitude(Vessel);
                case "LONGITUDE":
                    return VesselUtils.GetVesselLongitude(Vessel);
                case "ALTITUDE":
                    return Vessel.altitude;

                case "SENSORS":
                    return new VesselSensors(Vessel);
                case "TERMVELOCITY":
                    return VesselUtils.GetTerminalVelocity(Vessel);
                case "LOADED":
                    return Vessel.loaded;
                case "ROOTPART":
                    return new Part.PartValue(Vessel.rootPart,Shared);
            }

            // Is this a resource?
            double dblValue;
            if (VesselUtils.TryGetResource(Vessel, suffixName, out dblValue))
            {
                return dblValue;
            }

            return base.GetSuffix(suffixName);
        }
    }
}
