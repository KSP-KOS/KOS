using System;
using kOS.Safe.Encapsulation.Suffixes;
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
            return new Vector( Body.position - Shared.Vessel.findWorldCenterOfMass() );
        }
        
        override public OrbitableVelocity GetVelocities()
        {
            return new OrbitableVelocity(Body);
        }
        
        override public Vector GetPositionAtUT( TimeSpan timeStamp )
        {
            return new Vector( Body.getPositionAtUT( timeStamp.ToUnixStyleTime() ) - Shared.Vessel.findWorldCenterOfMass() );
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
            BodyInitializeSuffixes();

        }

        public BodyTarget(CelestialBody body, SharedObjects shareObj) :base(shareObj)
        {
            Body = body;
            BodyInitializeSuffixes();
        }

        private void BodyInitializeSuffixes()
        {
            AddSuffix("NAME", new Suffix<CelestialBody,string>(Body, model => model.name));
            AddSuffix("DESCRIPTION", new Suffix<CelestialBody,string>(Body, model => model.bodyDescription));
            AddSuffix("MASS", new Suffix<CelestialBody,double>(Body, model => model.Mass));
            AddSuffix("ALTITUDE", new Suffix<CelestialBody,double>(Body, model => model.orbit.altitude));
            AddSuffix("RADIUS", new Suffix<CelestialBody,double>(Body, model => model.Radius));
            AddSuffix("MU", new Suffix<CelestialBody,double>(Body, model => model.gravParameter));
            AddSuffix("ROTATIONPERIOD", new Suffix<CelestialBody,double>(Body, model => model.rotationPeriod));
            AddSuffix("ATM", new Suffix<CelestialBody,BodyAtmosphere>(Body, model => new BodyAtmosphere(Body)));
            AddSuffix("ANGULARVEL", new Suffix<CelestialBody,Direction>(Body, model => new Direction(Body.angularVelocity, true)));
        }
        
        public double GetDistance()
        {
            return Vector3d.Distance(Shared.Vessel.findWorldCenterOfMass(), Body.position) - Body.Radius;
        }

        public override object GetSuffix(string suffixName)
        {
            if (Target == null) throw new Exception("BODY structure appears to be empty!");
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
