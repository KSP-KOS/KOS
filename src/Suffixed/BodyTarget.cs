﻿using System;
using kOS.Utilities;

namespace kOS.Suffixed
{
    public class BodyTarget : SpecialValue
    {
        private readonly Vessel vessel;

        public BodyTarget(string name, Vessel vessel) : this(VesselUtils.GetBodyByName(name), vessel)
        {
        }

        public BodyTarget(CelestialBody target, Vessel vessel)
        {
            this.vessel = vessel;
            CelestialBody = target;
        }

        public CelestialBody CelestialBody { get; set; }

        public double GetDistance()
        {
            return Vector3d.Distance(vessel.GetWorldPos3D(), CelestialBody.position) - CelestialBody.Radius;
        }

        public override object GetSuffix(string suffixName)
        {
            if (Target == null) throw new Exception("BODY structure appears to be empty!");

            switch (suffixName)
            {
                case "NAME":
                    return CelestialBody.name;
                case "DESCRIPTION":
                    return CelestialBody.bodyDescription;
                case "MASS":
                    return CelestialBody.Mass;
                case "POSITION":
                    return new Vector(CelestialBody.position - vessel.GetWorldPos3D() );
                case "ALTITUDE":
                    return CelestialBody.orbit.altitude;
                case "APOAPSIS":
                    return CelestialBody.orbit.ApA;
                case "PERIAPSIS":
                    return CelestialBody.orbit.PeA;
                case "RADIUS":
                    return CelestialBody.Radius;
                case "MU":
                    return CelestialBody.gravParameter;
                case "ATM":
                    return new BodyAtmosphere(CelestialBody);
                case "VELOCITY":
                    return new Vector(CelestialBody.orbit.GetVel());
                case "DISTANCE":
                    return (float) GetDistance();
                case "OBT":
                    return new OrbitInfo(CelestialBody.orbit, vessel);
                case "BODY":
                    return new BodyTarget(CelestialBody.orbit.referenceBody, vessel);
            }

            return base.GetSuffix(suffixName);
        }

        public override string ToString()
        {
            if (CelestialBody != null)
            {
                return "BODY(\"" + CelestialBody.name + "\")";
            }

            return base.ToString();
        }

        public ITargetable Target
        {
            get { return CelestialBody; }
        }
    }
}
