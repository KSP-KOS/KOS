using System;
using kOS.Utilities;

namespace kOS.Suffixed
{
    public class BodyTarget : Orbitable, IKOSTargetable
    {

        public CelestialBody Body { get; set; }
        
        override public Orbit Orbit{ get{return Body.orbit;} }

        override public string GetName()
        {
            return Body.name;
        }

        override public Vector GetPosition()
        {
            return new Vector( Body.position - Shared.Vessel.GetWorldPos3D() );
        }
        
        override public OrbitableVelocity GetVelocities()
        {
            return new OrbitableVelocity(Body);
        }
        
        override public Vector GetPositionAtUT( TimeSpan timeStamp )
        {
            return new Vector( Body.getPositionAtUT( timeStamp.ToUnixStyleTime() ) - Shared.Vessel.GetWorldPos3D() );
        }

        override public OrbitableVelocity GetVelocitiesAtUT( TimeSpan timeStamp )
        {
            var orbVel = new Vector( Orbit.getOrbitalVelocityAtUT( timeStamp.ToUnixStyleTime() ) );
            orbVel = new Vector(orbVel.X,orbVel.Z,orbVel.Y); // swap Y and Z because KSP API is weird.
            
            CelestialBody parent = Body.referenceBody;
            if (parent==null) // only if Body is Sun and therefore has no parent.
                return new OrbitableVelocity( new Vector(0.0,0.0,0.0), new Vector(0.0,0.0,0.0) );
            var surfVel = new Vector( Body.orbit.GetVel() - parent.getRFrmVel( Body.position ) );

            return new OrbitableVelocity( orbVel, surfVel );
        }

        override public Orbit GetOrbitAtUT(double desiredUT)
        {
            return Orbit;  // Bodies cannot transition and are always on rails so this is constant.
        }

        override public Vector GetUpVector()
        {
            CelestialBody parent = Body.referenceBody;
            if (parent==null) // only if Body is Sun and therefore has no parent.
                return new Vector(0.0,0.0,0.0);
            return new Vector( (Body.position - parent.position).normalized );
        }

        override public Vector GetNorthVector()
        {
            CelestialBody parent = Body.referenceBody ?? Body;
            return new Vector( Vector3d.Exclude(GetUpVector().ToVector3D(), parent.transform.up) );
        }

        public BodyTarget(string name, SharedObjects shareObj) : this(VesselUtils.GetBodyByName(name),shareObj)
        {
        }

        public BodyTarget(CelestialBody body, SharedObjects shareObj) :base(shareObj)
        {
            Body = body;
        }
        
        public double GetDistance()
        {
            return Vector3d.Distance(Shared.Vessel.GetWorldPos3D(), Body.position) - Body.Radius;
        }

        public override object GetSuffix(string suffixName)
        {
            if (Target == null) throw new Exception("BODY structure appears to be empty!");

            switch (suffixName)
            {
                case "NAME":
                    return Body.name;
                case "DESCRIPTION":
                    return Body.bodyDescription;
                case "MASS":
                    return Body.Mass;
                case "ALTITUDE":
                    return Body.orbit.altitude;
                case "RADIUS":
                    return Body.Radius;
                case "MU":
                    return Body.gravParameter;
                case "ROTATIONPERIOD":
                    return Body.rotationPeriod;
                case "ATM":
                    return new BodyAtmosphere(Body);
                case "ANGULARVEL":
                    return new Direction(Body.angularVelocity, true);
            }

            return base.GetSuffix(suffixName);
        }

        public override string ToString()
        {
            if (Body != null)
            {
                return "BODY(\"" + Body.name + "\")";
            }

            return base.ToString();
        }

        public ITargetable Target
        {
            get { return Body; }
        }
    }
}
