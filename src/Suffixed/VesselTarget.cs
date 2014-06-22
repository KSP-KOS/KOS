using UnityEngine;
using kOS.Binding;
using kOS.Utilities;

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
            return new Vector( Vessel.GetWorldPos3D() - CurrentVessel.GetWorldPos3D() );
        }

        override public OrbitableVelocity GetVelocities()
        {
            return new OrbitableVelocity(Vessel);
        }
        
        override public Vector GetPositionAtUT( TimeSpan timeStamp )
        {
            // TODO: This will take work - getting the manuever nodes and SOI transitions to
            // find the position at the given timestamp.  This is a stub to get it to compile
            // for now:
            return new Vector(0.0, 0.0, 0.0);
        }

        override public OrbitableVelocity GetVelocitiesAtUT( TimeSpan timeStamp )
        {
            // TODO: This will take work - getting the manuever nodes and SOI transitions to
            // find the position at the given timestamp.  This is a stub to get it to compile
            // for now:
            return new OrbitableVelocity( new Vector(0.0, 0.0, 0.0), new Vector( 0.0, 0.0, 0.0) );
        }
        
        override public Vector GetUpVector()
        {
            return new Vector( Vessel.upAxis );
        }

        override public Vector GetNorthVector()
        {
            return new Vector( VesselUtils.GetNorthVector(Vessel) );
        }

        static VesselTarget()
        {
            ShortCuttableShipSuffixes = new[]
                {
                    "HEADING", "PROGRADE", "RETROGRADE", "FACING", "MAXTHRUST", "VELOCITY", "GEOPOSITION", "LATITUDE",
                    "LONGITUDE",
                    "UP", "NORTH", "BODY", "ANGULARMOMENTUM", "ANGULARVEL", "MASS", "VERTICALSPEED", "SURFACESPEED",
                    "AIRSPEED", "VESSELNAME",
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
            return Vector3d.Distance(CurrentVessel.GetWorldPos3D(), Vessel.GetWorldPos3D());
        }

        public override string ToString()
        {
            return "VESSEL(\"" + Vessel.vesselName + "\")";
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
                        (Vessel.orbit.GetVel() - FlightGlobals.currentMainBody.getRFrmVel(Vessel.GetWorldPos3D()))
                            .magnitude; //the velocity of the vessel relative to the air);
                case "VESSELNAME":
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
