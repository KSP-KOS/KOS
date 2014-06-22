namespace kOS.Suffixed
{
    public class OrbitInfo : SpecialValue
    {
        private const int PATCHES_LIMIT = 16;
        private readonly Orbit orbit;
        private readonly SharedObjects shared;
        private readonly string name;
 
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
            var orbVel = new Vector( orbit.getOrbitalVelocityAtUT( timeStamp.ToUnixStyleTime() ) );
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
                case "LAN":
                case "LONGITUDEOFASCENDINGNODE":
                    return orbit.LAN;
                case "ARGUMENTOFPERIAPSIS":
                    return orbit.argumentOfPeriapsis;
                case "TRUEANOMALY":
                    return orbit.trueAnomaly;
                case "MEANANOMALYATEPOCH":
                    return orbit.meanAnomalyAtEpoch;
                case "TRANSITION":
                    return orbit.patchEndTransition;
                case "POSITION":
                    return GetPositionAtUT( new TimeSpan(Planetarium.GetUniversalTime() ) );
                case "VELOCITY":
                    return GetVelocityAtUT( new TimeSpan(Planetarium.GetUniversalTime() ) );
                case "PATCHES":
                    return BuildPatchList();
                    
                //TODO: Determine if these vectors are different than POSITION and VELOCITY
                case "VSTATEVECTOR":
                    return orbit.vel;
                case "RSTATEVECTOR":
                    return orbit.pos;
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