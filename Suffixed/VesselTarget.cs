using UnityEngine;
using kOS.Binding;
using kOS.Utilities;

namespace kOS.Suffixed
{
    public class VesselTarget : SpecialValue
    {
        static VesselTarget()
        {
            ShortCuttableShipSuffixes = new[]
                {
                    "HEADING", "PROGRADE", "RETROGRADE", "FACING", "MAXTHRUST", "VELOCITY", "GEOPOSITION", "LATITUDE",
                    "LONGITUDE",
                    "UP", "NORTH", "BODY", "ANGULARMOMENTUM", "ANGULARVEL", "MASS", "VERTICALSPEED", "SURFACESPEED",
                    "AIRSPEED", "VESSELNAME",
                    "ALTITUDE", "APOAPSIS", "PERIAPSIS", "SENSOR", "SRFPROGRADE"
                };
        }

        public VesselTarget(Vessel target, Vessel currentVessel)
        {
            CurrentVessel = currentVessel;
            Vessel = target;
        }

        public Vessel CurrentVessel { get; private set; }

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

        public double GetDistance()
        {
            return Vector3d.Distance(CurrentVessel.GetWorldPos3D(), Vessel.GetWorldPos3D());
        }

        public override string ToString()
        {
            return "VESSEL(\"" + Vessel.vesselName + "\")";
        }

        public Direction GetPrograde()
        {
            var up = (Vessel.findLocalMOI(Vessel.findWorldCenterOfMass()) - Vessel.mainBody.position).normalized;

            var d = new Direction {Rotation = Quaternion.LookRotation(Vessel.orbit.GetVel().normalized, up)};
            return d;
        }

        public Direction GetRetrograde()
        {
            var up = (Vessel.findLocalMOI(Vessel.findWorldCenterOfMass()) - Vessel.mainBody.position).normalized;

            var d = new Direction {Rotation = Quaternion.LookRotation(Vessel.orbit.GetVel().normalized*-1, up)};
            return d;
        }

        public Direction GetFacing()
        {
            var vesselRotation = Vessel.transform.rotation;
            Quaternion vesselFacing = Quaternion.Inverse(Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(vesselRotation) * Quaternion.identity);
            return new Direction(vesselFacing);
        }

        public Direction GetSurfacePrograde()
        {
            var up = (Vessel.findLocalMOI(Vessel.findWorldCenterOfMass()) - Vessel.mainBody.position).normalized;

            var d = new Direction { Rotation = Quaternion.LookRotation(Vessel.srf_velocity.normalized, up) };
            return d;
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
                case "DIRECTION":
                    var vector = (Vessel.GetWorldPos3D() - CurrentVessel.GetWorldPos3D());
                    return new Direction(vector, false);
                case "DISTANCE":
                    return (float) GetDistance();
                case "BEARING":
                    return VesselUtils.GetTargetBearing(CurrentVessel, Vessel);
                case "HEADING":
                    return VesselUtils.GetTargetHeading(CurrentVessel, Vessel);
                case "PROGRADE":
                    return GetPrograde();
                case "RETROGRADE":
                    return GetRetrograde();
                case "MAXTHRUST":
                    return VesselUtils.GetMaxThrust(Vessel);
                case "VELOCITY":
                    return new VesselVelocity(Vessel);
                case "GEOPOSITION":
                    return new GeoCoordinates(Vessel);
                case "LATITUDE":
                    return VesselUtils.GetVesselLattitude(Vessel);
                case "LONGITUDE":
                    return VesselUtils.GetVesselLongitude(Vessel);
                case "FACING":
                    return GetFacing();
                case "UP":
                    return new Direction(Vessel.upAxis, false);
                case "NORTH":
                    return new Direction(VesselUtils.GetNorthVector(Vessel), false);
                case "BODY":
                    return new BodyTarget(Vessel.mainBody, Vessel);
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
                case "ALTITUDE":
                    return Vessel.altitude;
                case "APOAPSIS":
                    return Vessel.orbit.ApA;
                case "PERIAPSIS":
                    return Vessel.orbit.PeA;
                case "SENSORS":
                    return new VesselSensors(Vessel);
                case "TERMVELOCITY":
                    return VesselUtils.GetTerminalVelocity(Vessel);
                case "LOADED":
                    return Vessel.loaded;
                case "OBT":
                    return new OrbitInfo(Vessel);
                case "SRFPROGRADE":
                    return GetSurfacePrograde();
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
