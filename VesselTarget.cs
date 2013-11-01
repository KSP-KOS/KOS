using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace kOS
{
    public class VesselTarget : SpecialValue
    {
        public ExecutionContext context;
        public Vessel target;
        public static String[] ShortCuttableShipSuffixes;

        static VesselTarget()
        {
            ShortCuttableShipSuffixes = new String[] 
            {
                "HEADING", "PROGRADE", "RETROGRADE", "FACING", "MAXTHRUST", "VELOCITY", "GEOPOSITION", "LATITUDE", "LONGITUDE", 
                "UP", "NORTH", "BODY", "ANGULARMOMENTUM", "ANGULARVEL", "MASS", "VERTICALSPEED", "SURFACESPEED", "VESSELNAME", 
                "ALTITUDE", "APOAPSIS", "PERIAPSIS"
            };
        }

        public VesselTarget(Vessel target, ExecutionContext context)
        {
            this.context = context;
            this.target = target;
        }

        public bool IsInRange(double range)
        {
            if (GetDistance() <= range) return true;

            return false;
        }

        public double GetDistance()
        {
            return Vector3d.Distance(context.Vessel.GetWorldPos3D(), target.GetWorldPos3D());
        }

        public override string ToString()
        {
            return "VESSEL(\"" + target.vesselName + "\")";
        }

        public Direction GetPrograde()
        {
            var up = (target.findLocalMOI(target.findWorldCenterOfMass()) - target.mainBody.position).normalized;

            Direction d = new Direction();
            d.Rotation = Quaternion.LookRotation(target.orbit.GetVel().normalized, up);
            return d;
        }

        public Direction GetRetrograde()
        {
            var up = (target.findLocalMOI(target.findWorldCenterOfMass()) - target.mainBody.position).normalized;

            Direction d = new Direction();
            d.Rotation = Quaternion.LookRotation(target.orbit.GetVel().normalized * -1, up);
            return d;
        }

        public Direction GetFacing()
        {
            var facing = target.transform.up;
            return new Direction(new Vector3d(facing.x, facing.y, facing.z).normalized, false);
        }

        public override object GetSuffix(string suffixName)
        {
            if (suffixName == "DIRECTION")
            {
                var vector = (target.GetWorldPos3D() - context.Vessel.GetWorldPos3D());
                return new Direction(vector, false);
            }

            if (suffixName == "DISTANCE") return (float)GetDistance();
            if (suffixName == "BEARING") return VesselUtils.GetTargetBearing(context.Vessel, target);
            if (suffixName == "HEADING") return VesselUtils.GetTargetHeading(context.Vessel, target);
            if (suffixName == "PROGRADE") return GetPrograde();
            if (suffixName == "RETROGRADE") return GetRetrograde();
            if (suffixName == "MAXTHRUST") return VesselUtils.GetMaxThrust(target);
            if (suffixName == "VELOCITY") return new VesselVelocity(target);
            if (suffixName == "GEOPOSITION") return new GeoCoordinates(target);
            if (suffixName == "LATITUDE") return VesselUtils.GetVesselLattitude(target);
            if (suffixName == "LONGITUDE") return VesselUtils.GetVesselLongitude(target);
            if (suffixName == "FACING") return GetFacing();
            if (suffixName == "UP") return new Direction(target.upAxis, false);
            if (suffixName == "NORTH") return new Direction(VesselUtils.GetNorthVector(target), false);
            if (suffixName == "BODY") return target.mainBody.bodyName;
            if (suffixName == "ANGULARMOMENTUM") return  new Direction(target.angularMomentum, true);
            if (suffixName == "ANGULARVEL") return new Direction(target.angularVelocity, true);
            if (suffixName == "MASS") return  target.GetTotalMass();
            if (suffixName == "VERTICALSPEED") return  target.verticalSpeed;
            if (suffixName == "SURFACESPEED") return  target.horizontalSrfSpeed;
            if (suffixName == "VESSELNAME") return  target.vesselName;
            if (suffixName == "ALTITUDE") return target.altitude;
            if (suffixName == "APOAPSIS") return  target.orbit.ApA;
            if (suffixName == "PERIAPSIS") return  target.orbit.PeA; 

            // Is this a resource?
            double dblValue;
            if (VesselUtils.TryGetResource(target, suffixName, out dblValue))
            {
                return dblValue;
            }

            return base.GetSuffix(suffixName);
        }
    }
}
