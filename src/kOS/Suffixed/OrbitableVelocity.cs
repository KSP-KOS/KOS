namespace kOS.Suffixed
{
    /// <summary>
    /// Holds the velocity of an object in both orbital and prograde
    /// reference frames.  (Note this may become moot after vector
    /// reference frame transformation is implemented.)
    /// </summary>
    public class OrbitableVelocity : SpecialValue
    {
        public Vector Orbital { get; private set; }

        public Vector Surface { get; private set; }

        public OrbitableVelocity(Vessel v)
        {
            Orbital = new Vector(v.obt_velocity);
            Surface = new Vector(v.srf_velocity);
        }

        public OrbitableVelocity(CelestialBody b)
        {
            Orbital = new Vector(b.orbit.GetVel()); // KSP's b.GetObtVelocity() is broken - it causes stack overflow
            CelestialBody parent = b.referenceBody;
            Surface = parent != null ?
                new Vector(b.orbit.GetVel() - parent.getRFrmVel(b.position)) :
                new Vector(default(float), default(float), default(float));
        }

        /// <summary>
        ///   Create a OrbitableVelocity object out of a raw pair of hardcoded orbital and surface velocity:
        ///   This leaves it up to the caller to be responsible for making sure the orbital and
        ///   surface velocities match each other.
        /// </summary>
        /// <param name="orbVel">orbital velocity in raw reference frame</param>
        /// <param name="surfVel">surface velocity in raw reference frame</param>
        public OrbitableVelocity(Vector orbVel, Vector surfVel)
        {
            Orbital = orbVel;
            Surface = surfVel;
        }

        public override object GetSuffix(string suffixName)
        {
            switch (suffixName)
            {
                case "ORBIT":
                    return Orbital;

                case "SURFACE":
                    return Surface;
            }

            return base.GetSuffix(suffixName);
        }

        public override string ToString()
        {
            return "OrbitableVelocity(\n" +
                    "  :orbit=" + Orbital + ",\n" +
                    "  :surface=" + Surface + "\n" +
                    ")";
        }
    }
}