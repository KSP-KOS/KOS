namespace kOS.Suffixed
{
    /// <summary>
    ///   Much of what this class did has been superceeded by
    ///   Orbitable, but it's here because there's still
    ///   cases where an orbit is just a subset of a larger
    ///   orbit and therefore doesn't have as much information
    ///   (for example the orbit patch after a manuever node).
    ///   this class is used for single patches of an orbit.
    /// </summary>
    public class OrbitInfo : SpecialValue
    {
        private const int PATCHES_LIMIT = 16;
        private Orbit orbit;
        private SharedObjects shared;
        private string name;
 
        public OrbitInfo(Orbitable orb, SharedObjects sharedObj)
        {
            orbit = orb.Orbit;
            shared = sharedObj;
            name = orb.GetName();
        }
        
        public OrbitInfo( Orbit orb, SharedObjects sharedObj )
        {
            shared = sharedObj;
            orbit = orb;
            name = "<unnamed>";
        }
        
        /// <summary>
        ///   Get the position of this thing in this orbit at the given
        ///   time.  Note that it does NOT take into account any
        ///   encounters or manuever nodes - it assumes the current
        ///   orbit patch remains followed forever.
        /// </summary>
        /// <param name="timeStamp">The universal time to query for</param>
        /// <returns></returns>
        public Vector GetPositionAtUT( TimeSpan timeStamp )
        {
            return new Vector( orbit.getPositionAtUT( timeStamp.ToUnixStyleTime() ) - shared.Vessel.GetWorldPos3D() );
        }

        /// <summary>
        ///   Get the velocity pairing of this thing in this orbit at the given
        ///   time.  Note that it does NOT take into account any
        ///   encounters or manuever nodes - it assumes the current
        ///   orbit patch remains followed forever.
        /// </summary>
        /// <param name="timeStamp">The universal time to query for</param>
        /// <returns></returns>
        public OrbitableVelocity GetVelocityAtUT( TimeSpan timeStamp )
        {
            Vector orbVel = new Vector( orbit.getOrbitalVelocityAtUT( timeStamp.ToUnixStyleTime() ) );
            // For some weird reason orbit returns velocities with Y and Z swapped, so flip them back:
            orbVel = new Vector( orbVel.X, orbVel.Z, orbVel.Y );
            CelestialBody parent = orbit.referenceBody;
            Vector surfVel;
            if (parent != null)
            {
                Vector3d pos = GetPositionAtUT( timeStamp ).ToVector3D();
                surfVel = new Vector( orbit.GetVel() - parent.getRFrmVel( pos ) );
            }
            else
                surfVel = new Vector( orbVel.X, orbVel.Y, orbVel.Z );
            return new OrbitableVelocity( orbVel, surfVel );
        }
        
        public override object GetSuffix(string suffixName)
        {

            switch (suffixName)
            {
                case "NAME":
                    return name;
                case "APOAPSIS":
                    return orbit.ApA;
                case "PERIAPSIS":
                    return orbit.PeA;
                case "BODY":
                    return new BodyTarget(orbit.referenceBody, shared);
                case "PERIOD":
                    return orbit.period;
                case "INCLINATION":
                    return orbit.inclination;
                case "ECCENTRICITY":
                    return orbit.eccentricity;
                case "SEMIMAJORAXIS":
                    return orbit.semiMajorAxis;
                case "SEMIMINORAXIS":
                    return orbit.semiMinorAxis;
                case "TRANSITION":
                    return orbit.patchEndTransition;
                case "POSITION":
                    return GetPositionAtUT( new TimeSpan(Planetarium.GetUniversalTime() ) );
                case "VELOCITY":
                    return GetVelocityAtUT( new TimeSpan(Planetarium.GetUniversalTime() ) );
                case "PATCHES":
                    return BuildPatchList();
            }
            return base.GetSuffix(suffixName);
        }

        private object BuildPatchList()
        {
            var list = new ListValue();
            var orb = orbit;
            while (orb.nextPatch != null && list.Count <= PATCHES_LIMIT)
            {
                list.Add(new OrbitInfo(orb, shared ));
                orb = orb.nextPatch;
            }
            return list;
        }             

        public override string ToString()
        {
            return "ORBIT of " + name;
        }
    }
}