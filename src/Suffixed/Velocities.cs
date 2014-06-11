using kOS.Utilities;

namespace kOS.Suffixed
{
    /// <summary>
    /// Holds the velocity of an object in both orbital and prograde
    /// reference frames.  (Note this may become moot after vector
    /// reference frame transformation is implemented.)
    /// </summary>
    public class Velocities : SpecialValue
    {
        private readonly Vector orbitVelocity;
        public Vector orbital{ get{return orbitVelocity;} set{} }
        private readonly Vector surfaceVelocity;
        public Vector surface{ get{return surfaceVelocity;} set{} }

        public Velocities(Vessel v)
        {
            orbitVelocity = new Vector(v.obt_velocity);
            surfaceVelocity = new Vector(v.srf_velocity);
        }

        public Velocities(CelestialBody b)
        {
            orbitVelocity = new Vector(b.orbit.GetVel()); // KSP's b.GetObtVelocity() is broken - it causes stack overflow
            CelestialBody parent = b.referenceBody;
            if (parent != null)
                surfaceVelocity = new Vector( b.orbit.GetVel() - parent.getRFrmVel( b.position ) );
            else
                surfaceVelocity = new Vector( 0.0, 0.0, 0.0 );
        }
        
        /// <summary>
        ///   Create a Velocities object out of a raw pair of hardcoded orbital and surface velocity:
        ///   This leaves it up to the caller to be responsible for making sure the orbital and
        ///   surface velocities match each other.
        /// </summary>
        /// <param name="orbVel">orbital velocity in raw reference frame</param>
        /// <param name="surfVel">surface velocity in raw reference frame</param>
        public Velocities( Vector orbVel, Vector surfVel )
        {
            orbitVelocity = orbVel;
            surfaceVelocity = surfVel;
        }
        

        public override object GetSuffix(string suffixName)
        {
            switch (suffixName)
            {
                case "ORBIT":
                    return orbitVelocity;
                case "SURFACE":
                    return surfaceVelocity;
            }

            return base.GetSuffix(suffixName);
        }

        public override string ToString()
        {
            return "Velocities(\n" +
                    "  :orbit=" + orbitVelocity.ToString() + ",\n" +
                    "  :surface="+surfaceVelocity.ToString() + "\n" +
                    ")";
        }
    }
}