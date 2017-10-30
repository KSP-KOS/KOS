using kOS.Safe.Encapsulation;
using kOS.Utilities;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Suffixed
{
    /// <summary>
    /// Holds the velocity of an object in both orbital and prograde
    /// reference frames.  (Note this may become moot after vector
    /// reference frame transformation is implemented.)
    /// </summary>
    [kOS.Safe.Utilities.KOSNomenclature("OrbitableVelocity")]
    public class OrbitableVelocity : Structure
    {
        public Vector Orbital { get; private set; }

        public Vector Surface { get; private set; }

        public OrbitableVelocity(Vessel v)
        {
            Orbital = new Vector(v.obt_velocity);
            Surface = new Vector(v.srf_velocity);
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix(new[] {"OBT", "ORBIT"}, new Suffix<Vector>(() => Orbital));
            AddSuffix("SURFACE", new Suffix<Vector>(() => Surface));
        }

        public OrbitableVelocity(CelestialBody b, SharedObjects shared)
        {
            Orbital = new Vector(b.KOSExtensionGetObtVelocity(shared)); // KSP's b.GetObtVelocity() is broken - it causes stack overflow
            CelestialBody parent = b.KOSExtensionGetParentBody();
            Surface = (parent != null) ?
                new Vector(b.orbit.GetVel() - parent.getRFrmVel(b.position)) :
                new Vector(Orbital); // return same velocity as orbit when no parent body to compare against.
            InitializeSuffixes();
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
            InitializeSuffixes();
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