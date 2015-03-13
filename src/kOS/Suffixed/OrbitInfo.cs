using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Suffixed
{
    public class OrbitInfo : Structure
    {
        private readonly Orbit orbit;
        private readonly SharedObjects shared;
        private readonly string name;
 
        public OrbitInfo(Orbitable orb, SharedObjects sharedObj)
        {
            orbit = orb.Orbit;
            shared = sharedObj;
            name = orb.GetName();
            InitializeSuffixes();
        }
        
        public OrbitInfo( Orbit orb, SharedObjects sharedObj )
        {
            shared = sharedObj;
            orbit = orb;
            name = "<unnamed>";
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("NAME", new Suffix<string>(() => name));
            AddSuffix("APOAPSIS", new Suffix<double>(() => orbit.ApA));
            AddSuffix("PERIAPSIS", new Suffix<double>(() => orbit.PeA));
            AddSuffix("ETA_APOAPSIS", new Suffix<double>(() => orbit.timeToAp));
            AddSuffix("ETA_PERIAPSIS", new Suffix<double>(() => orbit.timeToPe));
            AddSuffix("BODY", new Suffix<BodyTarget>(() => new BodyTarget(orbit.referenceBody, shared)));
            AddSuffix("PERIOD", new Suffix<double>(() => orbit.period));
            AddSuffix("INCLINATION", new Suffix<double>(() => orbit.inclination));
            AddSuffix("ECCENTRICITY", new Suffix<double>(() => orbit.eccentricity));
            AddSuffix("SEMIMAJORAXIS", new Suffix<double>(() => orbit.semiMajorAxis));
            AddSuffix("SEMIMINORAXIS", new Suffix<double>(() => orbit.semiMinorAxis));
            AddSuffix(new[]{"LAN", "LONGITUDEOFASCENDINGNODE"}, new Suffix<double>(() => orbit.LAN));
            AddSuffix("ARGUMENTOFPERIAPSIS", new Suffix<double>(() => orbit.argumentOfPeriapsis));
            AddSuffix("TRUEANOMALY", new Suffix<double>(() => orbit.trueAnomaly));
            AddSuffix("MEANANOMALYATEPOCH", new Suffix<double>(() => orbit.meanAnomalyAtEpoch));
            AddSuffix("TRANSITION", new Suffix<string>(() => orbit.patchEndTransition.ToString()));
            AddSuffix("POSITION", new Suffix<Vector>(() => GetPositionAtUT( new TimeSpan(Planetarium.GetUniversalTime() ) )));
            AddSuffix("VELOCITY", new Suffix<OrbitableVelocity>(() => GetVelocityAtUT( new TimeSpan(Planetarium.GetUniversalTime() ) )));
            AddSuffix("NEXTPATCH", new Suffix<OrbitInfo>(GetNextPatch));
            AddSuffix("HASNEXTPATCH", new Suffix<bool>(GetHasNextPatch));

            //TODO: Determine if these vectors are different than POSITION and VELOCITY
            AddSuffix("VSTATEVECTOR", new Suffix<Vector>(() => new Vector(orbit.vel)));
            AddSuffix("RSTATEVECTOR", new Suffix<Vector>(() => new Vector(orbit.pos)));

        }

        /// <summary>
        ///   Get the position of this thing in this orbit at the given
        ///   time.  Note that it does NOT take into account any
        ///   encounters or maneuver nodes - it assumes the current
        ///   orbit patch remains followed forever.
        /// </summary>
        /// <param name="timeStamp">The universal time to query for</param>
        /// <returns></returns>
        public Vector GetPositionAtUT( TimeSpan timeStamp )
        {
            return new Vector( orbit.getPositionAtUT( timeStamp.ToUnixStyleTime() ) - shared.Vessel.findWorldCenterOfMass() );
        }

        /// <summary>
        ///   Get the velocity pairing of this thing in this orbit at the given
        ///   time.  Note that it does NOT take into account any
        ///   encounters or maneuver nodes - it assumes the current
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
                Vector3d pos = GetPositionAtUT( timeStamp );
                surfVel = new Vector( orbVel - parent.getRFrmVel( pos + shared.Vessel.findWorldCenterOfMass()) );
            }
            else
                surfVel = new Vector( orbVel.X, orbVel.Y, orbVel.Z );
            return new OrbitableVelocity( orbVel, surfVel );
        }
        
        /// <summary>
        /// Return the next OrbitInfo after this one (i.e. transitional encounter)
        /// </summary>
        /// <returns>an OrbitInfo, or a null if there isn't any.</returns>
        private OrbitInfo GetNextPatch()
        {
            return ! GetHasNextPatch() ? null : new OrbitInfo(orbit.nextPatch,shared);
        }

        /// <summary>
        /// Find out whether or not the orbit has a next patch.
        /// </summary>
        /// <returns>true if the :NEXTPATCH suffix will return a real suffix.</returns>
        private bool GetHasNextPatch()
        {
            return orbit.nextPatch != null && (orbit.nextPatch.activePatch);
        }

        public override string ToString()
        {
            return "ORBIT of " + name;
        }
    }
}