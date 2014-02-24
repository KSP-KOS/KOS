using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using kOS.Utilities;

namespace kOS.Suffixed
{
    public class VesselTarget : SpecialValue
    {
        public Vessel currentVessel;
        public Vessel target;
        public static String[] ShortCuttableShipSuffixes;

        static VesselTarget()
        {
            ShortCuttableShipSuffixes = new String[] 
            {
                "HEADING", "PROGRADE", "RETROGRADE", "FACING", "MAXTHRUST", "VELOCITY", "GEOPOSITION", "LATITUDE", "LONGITUDE", 
                "UP", "NORTH", "BODY", "ANGULARMOMENTUM", "ANGULARVEL", "MASS", "VERTICALSPEED", "SURFACESPEED", "AIRSPEED", "VESSELNAME", 
                "ALTITUDE", "APOAPSIS", "PERIAPSIS", "SENSOR"
            };
        }

        public VesselTarget(Vessel target, Vessel currentVessel)
        {
            this.currentVessel = currentVessel;
            this.target = target;
        }

        public bool IsInRange(double range)
        {
            if (GetDistance() <= range) return true;

            return false;
        }

        public double GetDistance()
        {
            return Vector3d.Distance(currentVessel.GetWorldPos3D(), target.GetWorldPos3D());
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
            var vesselRotation = target.transform.rotation;
            Quaternion vesselFacing = Quaternion.Inverse(Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(vesselRotation) * Quaternion.identity);
            return new Direction(vesselFacing);
        }

        public override object GetSuffix(string suffixName)
        {
            if (suffixName == "DIRECTION")
            {
                var vector = (target.GetWorldPos3D() - currentVessel.GetWorldPos3D());
                return new Direction(vector, false);
            }

            if (suffixName == "DISTANCE") return (float)GetDistance();
            if (suffixName == "BEARING") return VesselUtils.GetTargetBearing(currentVessel, target);
            if (suffixName == "HEADING") return VesselUtils.GetTargetHeading(currentVessel, target);
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
            if (suffixName == "AIRSPEED") return (target.orbit.GetVel() - FlightGlobals.currentMainBody.getRFrmVel(target.GetWorldPos3D())).magnitude; //the velocity of the vessel relative to the air);
            if (suffixName == "VESSELNAME") return  target.vesselName;
            if (suffixName == "ALTITUDE") return target.altitude;
            if (suffixName == "APOAPSIS") return  target.orbit.ApA;
            if (suffixName == "PERIAPSIS") return  target.orbit.PeA; 
            if (suffixName == "SENSOR") return new VesselSensors(target);
            if (suffixName == "TERMVELOCITY") return VesselUtils.GetTerminalVelocity(target);

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
