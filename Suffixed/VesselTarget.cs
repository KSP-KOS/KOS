using UnityEngine;
using kOS.Context;
using kOS.Utilities;

namespace kOS.Suffixed
{
    public class VesselTarget : SpecialValue
    {
        private readonly IExecutionContext context;

        static VesselTarget()
        {
            ShortCuttableShipSuffixes = new[]
                {
                    "HEADING", "PROGRADE", "RETROGRADE", "FACING", "MAXTHRUST", "VELOCITY", "GEOPOSITION", "LATITUDE",
                    "LONGITUDE",
                    "UP", "NORTH", "BODY", "ANGULARMOMENTUM", "ANGULARVEL", "MASS", "VERTICALSPEED", "SURFACESPEED",
                    "AIRSPEED", "VESSELNAME",
                    "ALTITUDE", "APOAPSIS", "PERIAPSIS", "SENSOR"
                };
        }

        public VesselTarget(Vessel target, IExecutionContext context)
        {
            this.context = context;
            Target = target;
        }

        public Vessel Target { get; private set; }
        public static string[] ShortCuttableShipSuffixes { get; private set; }

        public bool IsInRange(double range)
        {
            return GetDistance() <= range;
        }

        public double GetDistance()
        {
            return Vector3d.Distance(context.Vessel.GetWorldPos3D(), Target.GetWorldPos3D());
        }

        public override string ToString()
        {
            return "VESSEL(\"" + Target.vesselName + "\")";
        }

        public Direction GetPrograde()
        {
            var up = (Target.findLocalMOI(Target.findWorldCenterOfMass()) - Target.mainBody.position).normalized;

            var d = new Direction {Rotation = Quaternion.LookRotation(Target.orbit.GetVel().normalized, up)};
            return d;
        }

        public Direction GetRetrograde()
        {
            var up = (Target.findLocalMOI(Target.findWorldCenterOfMass()) - Target.mainBody.position).normalized;

            var d = new Direction {Rotation = Quaternion.LookRotation(Target.orbit.GetVel().normalized*-1, up)};
            return d;
        }

        public Direction GetFacing()
        {
            var facing = Target.transform.up;
            return new Direction(new Vector3d(facing.x, facing.y, facing.z).normalized, false);
        }

        public override object GetSuffix(string suffixName)
        {
            switch (suffixName)
            {
                case "DIRECTION":
                    var vector = (Target.GetWorldPos3D() - context.Vessel.GetWorldPos3D());
                    return new Direction(vector, false);
                case "DISTANCE":
                    return (float) GetDistance();
                case "BEARING":
                    return VesselUtils.GetTargetBearing(context.Vessel, Target);
                case "HEADING":
                    return VesselUtils.GetTargetHeading(context.Vessel, Target);
                case "PROGRADE":
                    return GetPrograde();
                case "RETROGRADE":
                    return GetRetrograde();
                case "MAXTHRUST":
                    return VesselUtils.GetMaxThrust(Target);
                case "VELOCITY":
                    return new VesselVelocity(Target);
                case "GEOPOSITION":
                    return new GeoCoordinates(Target);
                case "LATITUDE":
                    return VesselUtils.GetVesselLattitude(Target);
                case "LONGITUDE":
                    return VesselUtils.GetVesselLongitude(Target);
                case "FACING":
                    return GetFacing();
                case "UP":
                    return new Direction(Target.upAxis, false);
                case "NORTH":
                    return new Direction(VesselUtils.GetNorthVector(Target), false);
                case "BODY":
                    return Target.mainBody.bodyName;
                case "ANGULARMOMENTUM":
                    return new Direction(Target.angularMomentum, true);
                case "ANGULARVEL":
                    return new Direction(Target.angularVelocity, true);
                case "MASS":
                    return Target.GetTotalMass();
                case "VERTICALSPEED":
                    return Target.verticalSpeed;
                case "SURFACESPEED":
                    return Target.horizontalSrfSpeed;
                case "AIRSPEED":
                    return
                        (Target.orbit.GetVel() - FlightGlobals.currentMainBody.getRFrmVel(Target.GetWorldPos3D()))
                            .magnitude; //the velocity of the vessel relative to the air);
                case "VESSELNAME":
                    return Target.vesselName;
                case "ALTITUDE":
                    return Target.altitude;
                case "APOAPSIS":
                    return Target.orbit.ApA;
                case "PERIAPSIS":
                    return Target.orbit.PeA;
                case "SENSOR":
                    return new VesselSensors(Target);
                case "TERMVELOCITY":
                    return VesselUtils.GetTerminalVelocity(Target);
                case "OBT":
                    return new OrbitInfo(Target.orbit, Target);
            }

            // Is this a resource?
            double dblValue;
            if (VesselUtils.TryGetResource(Target, suffixName, out dblValue))
            {
                return dblValue;
            }

            return base.GetSuffix(suffixName);
        }
    }
}