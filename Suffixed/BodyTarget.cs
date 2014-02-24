using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Utilities;

namespace kOS.Suffixed
{
    public class BodyTarget : SpecialValue
    {
        public Vessel currentVessel;
        public CelestialBody target;

        public BodyTarget(String name, Vessel currentVessel) : this(VesselUtils.GetBodyByName(name), currentVessel) { }

        public BodyTarget(CelestialBody target, Vessel currentVessel)
        {
            this.currentVessel = currentVessel;
            this.target = target;
        }

        public double GetDistance()
        {
            return Vector3d.Distance(currentVessel.GetWorldPos3D(), target.position) - target.Radius;
        }

        public override object GetSuffix(string suffixName)
        {
            if (target == null) throw new Exception("BODY structure appears to be empty!");

            if (suffixName == "NAME") return target.name;
            if (suffixName == "DESCRIPTION") return target.bodyDescription;
            if (suffixName == "MASS") return target.Mass;
            if (suffixName == "POSITION") return new Vector(target.position);
            if (suffixName == "ALTITUDE") return target.orbit.altitude;
            if (suffixName == "APOAPSIS") return target.orbit.ApA;
            if (suffixName == "PERIAPSIS") return target.orbit.PeA;
            if (suffixName == "VELOCITY") return new Vector(target.orbit.GetVel());
            if (suffixName == "DISTANCE") return (float)GetDistance();
            if (suffixName == "BODY") return new BodyTarget(target.orbit.referenceBody, currentVessel);

            return base.GetSuffix(suffixName);
        }

        public override string ToString()
        {
 	        if (target != null)
            {
                return "BODY(\"" + target.name + "\")";
            }

            return base.ToString();
        }
    }


}
