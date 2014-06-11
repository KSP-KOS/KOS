using System;
using kOS.Utilities;

namespace kOS.Suffixed
{
    public class BodyTarget : Orbitable, IKOSTargetable
    {

        public CelestialBody Body { get; set; }
        
        override public Orbit orbit{ get{return Body.orbit;} set{} }
        private SharedObjects _shared;
        override public SharedObjects shared{ get{return _shared;} set{_shared = value;} }
        
        override public string GetName()
        {
            return Body.name;
        }

        override public Vector GetPosition()
        {
            return new Vector( Body.position - shared.Vessel.GetWorldPos3D() );
        }
        
        override public Velocities GetVelocities()
        {
            return new Velocities(Body);
        }
        
        override public Vector GetPositionAtUT( TimeSpan timeStamp )
        {
            return new Vector( Body.getPositionAtUT( timeStamp.ToUnixStyleTime() ) - shared.Vessel.GetWorldPos3D() );
        }

        override public Velocities GetVelocitiesAtUT( TimeSpan timeStamp )
        {
            Vector orbVel = new Vector( orbit.getOrbitalVelocityAtUT( timeStamp.ToUnixStyleTime() ) );
            CelestialBody parent = Body.referenceBody;
            if (parent==null) // only if Body is Sun and therefore has no parent.
                return new Velocities( new Vector(0.0,0.0,0.0), new Vector(0.0,0.0,0.0) );
            Vector surfVel = new Vector( Body.orbit.GetVel() - parent.getRFrmVel( Body.position ) );

            return new Velocities( orbVel, surfVel );
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
            CelestialBody parent = Body.referenceBody;
            if (parent==null) // only if Body is Sun and therefore has no parent.
                parent = Body;
            return new Vector( Vector3d.Exclude(GetUpVector().ToVector3D(), parent.transform.up) );
        }

        public BodyTarget(string name, SharedObjects shareObj) : this(VesselUtils.GetBodyByName(name),shareObj)
        {
        }

        public BodyTarget(CelestialBody body, SharedObjects shareObj)
        {
            Body = body;
            shared = shareObj;
        }
        
        public double GetDistance()
        {
            return Vector3d.Distance(shared.Vessel.GetWorldPos3D(), Body.position) - Body.Radius;
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
