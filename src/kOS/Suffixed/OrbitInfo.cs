using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Serialization;
using System;

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("Orbit")]
    public class OrbitInfo : Structure
    {
        private Orbit orbit;
        public SharedObjects Shared { get; set; }
        private string name;
        private OrbitEta eta;
 
        public OrbitInfo()
        {
            InitializeSuffixes();
        }

        public OrbitInfo(Orbitable orb, SharedObjects sharedObj) : this()
        {
            orbit = orb.Orbit;
            Shared = sharedObj;
            name = orb.GetName();
        }
        
        public OrbitInfo( Orbit orb, SharedObjects sharedObj) : this()
        {
            Shared = sharedObj;
            orbit = orb;
            if (orb.referenceBody == null)
                name = "<unnamed>"; // I have no clue when or how this could ever happen.  What is an orbit around nothing?
            else
                name = orb.referenceBody.name;
        }

        private void InitializeSuffixes()
        {
            AddSuffix("NAME", new Suffix<StringValue>(() => name));
            AddSuffix("APOAPSIS", new Suffix<ScalarValue>(() => orbit.ApA));
            AddSuffix("PERIAPSIS", new Suffix<ScalarValue>(() => orbit.PeA));
            AddSuffix("BODY", new Suffix<BodyTarget>(() => BodyTarget.CreateOrGetExisting(orbit.referenceBody, Shared)));
            AddSuffix("PERIOD", new Suffix<ScalarValue>(() => orbit.period));
            AddSuffix("INCLINATION", new Suffix<ScalarValue>(() => orbit.inclination));
            AddSuffix("ECCENTRICITY", new Suffix<ScalarValue>(() => orbit.eccentricity));
            AddSuffix("SEMIMAJORAXIS", new Suffix<ScalarValue>(() => orbit.semiMajorAxis));
            AddSuffix("SEMIMINORAXIS", new Suffix<ScalarValue>(() => orbit.semiMinorAxis));
            AddSuffix(new[]{"LAN", "LONGITUDEOFASCENDINGNODE"}, new Suffix<ScalarValue>(() => orbit.LAN));
            AddSuffix("ARGUMENTOFPERIAPSIS", new Suffix<ScalarValue>(() => orbit.argumentOfPeriapsis));
            AddSuffix("TRUEANOMALY", new Suffix<ScalarValue>(() => orbit.eccentricity < 1.0 ? 
                                                             Utilities.Utils.DegreeFix(Utilities.Utils.RadiansToDegrees(orbit.trueAnomaly),0.0) :
                                                             Utilities.Utils.DegreeFix (Utilities.Utils.RadiansToDegrees (orbit.trueAnomaly), -180.0)
                                                            ));
            AddSuffix("MEANANOMALYATEPOCH", new Suffix<ScalarValue>(() => orbit.eccentricity < 1.0 ? 
                                                                    Utilities.Utils.DegreeFix(Utilities.Utils.RadiansToDegrees(orbit.meanAnomalyAtEpoch), 0.0) :
                                                                    Utilities.Utils.RadiansToDegrees(orbit.meanAnomalyAtEpoch)
                                                                   ));
            AddSuffix("EPOCH", new Suffix<ScalarValue>(() => orbit.epoch));
            AddSuffix("TRANSITION", new Suffix<StringValue>(() => orbit.patchEndTransition.ToString()));
            AddSuffix("POSITION", new Suffix<Vector>(() => GetPositionAtUT( new TimeStamp(Planetarium.GetUniversalTime() ) )));
            AddSuffix("VELOCITY", new Suffix<OrbitableVelocity>(() => GetVelocityAtUT( new TimeStamp(Planetarium.GetUniversalTime() ) )));
            AddSuffix("NEXTPATCH", new Suffix<OrbitInfo>(GetNextPatch));
            AddSuffix("HASNEXTPATCH", new Suffix<BooleanValue>(GetHasNextPatch));
            AddSuffix("NEXTPATCHETA", new Suffix<ScalarValue>(() => GetETA().GetEndTransition()));  // deprecated alias for :ETA:TRANSITION
            AddSuffix("ETA", new Suffix<OrbitEta>(GetETA));

            //TODO: Determine if these vectors are different than POSITION and VELOCITY
            AddSuffix("VSTATEVECTOR", new Suffix<Vector>(() => new Vector(orbit.vel)));
            AddSuffix("RSTATEVECTOR", new Suffix<Vector>(() => new Vector(orbit.pos)));

        }

        /// <summary>
        /// Returns the OrbitEta structure associated with this orbit, creating it if needed.
        /// </summary>
        /// <returns>an OrbitEta structure.</returns>
        private OrbitEta GetETA()
        {
            // Cache the OrbitEta structure to hopefully avoid a little bit of unnecessary GC
            // if multiple ETAs are queried for the same orbit.
            if (this.eta == null) {
                this.eta = new OrbitEta(orbit, Shared);
            }
            return this.eta;
        }

        /// <summary>
        ///   Get the position of this thing in this orbit at the given
        ///   time.  Note that it does NOT take into account any
        ///   encounters or maneuver nodes - it assumes the current
        ///   orbit patch remains followed forever.
        /// </summary>
        /// <param name="timeStamp">The universal time to query for</param>
        /// <returns></returns>
        public Vector GetPositionAtUT( TimeStamp timeStamp )
        {
            return new Vector( orbit.getPositionAtUT( timeStamp.ToUnixStyleTime() ) - Shared.Vessel.CoMD );
        }

        /// <summary>
        ///   Get the velocity pairing of this thing in this orbit at the given
        ///   time.  Note that it does NOT take into account any
        ///   encounters or maneuver nodes - it assumes the current
        ///   orbit patch remains followed forever.
        /// </summary>
        /// <param name="timeStamp">The universal time to query for</param>
        /// <returns></returns>
        public OrbitableVelocity GetVelocityAtUT( TimeStamp timeStamp )
        {
            var orbVel = new Vector( orbit.getOrbitalVelocityAtUT( timeStamp.ToUnixStyleTime() ) );
            // For some weird reason orbit returns velocities with Y and Z swapped, so flip them back:
            orbVel = new Vector( orbVel.X, orbVel.Z, orbVel.Y );
            CelestialBody parent = orbit.referenceBody;
            Vector surfVel;
            if (parent != null)
            {
                Vector3d pos = GetPositionAtUT( timeStamp );
                surfVel = new Vector( orbVel - parent.getRFrmVel( pos + Shared.Vessel.CoMD) );
            }
            else
                surfVel = new Vector( orbVel.X, orbVel.Y, orbVel.Z );
            return new OrbitableVelocity( orbVel, surfVel );
        }

        /// <summary>
        /// Return the next OrbitInfo after this one (i.e. transitional encounter)
        /// </summary>
        /// <returns>an OrbitInfo representing the next orbit patch.</returns>
        private OrbitInfo GetNextPatch()
        {
            if (GetHasNextPatch())
            {
                return new OrbitInfo(orbit.nextPatch, Shared);
            }
            throw new KOSSituationallyInvalidException("Cannot get next patch when no additional patches exist.  Try checking the HASNEXTPATCH suffix.");
        }

        /// <summary>
        /// Find out whether or not the orbit has a next patch.
        /// </summary>
        /// <returns>true if the :NEXTPATCH suffix will return a real suffix.</returns>
        private BooleanValue GetHasNextPatch()
        {
            return orbit.nextPatch != null && (orbit.nextPatch.activePatch);
        }

        public override string ToString()
        {
            return "ORBIT of " + name;
        }
    }
}
