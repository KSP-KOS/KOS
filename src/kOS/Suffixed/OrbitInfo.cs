using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using System;
using kOS.Serialization;
using kOS.Safe.Serialization;

namespace kOS.Suffixed
{
    public class OrbitInfo : Structure, IDumperWithSharedObjects
    {
        public static string DumpInclination = "inclination";
        public static string DumpEccentricity = "eccentricity";
        public static string DumpSemiMajorAxis = "semiMajorAxis";
        public static string DumpLongitudeOfAscendingNode = "longitudeOfAscendingNode";
        public static string DumpArgumentOfPeriapsis = "argumentOfPeriapsis";
        public static string DumpMeanAnomalyAtEpoch = "meanAnomalyAtEpoch";
        public static string DumpEpoch = "epoch";
        public static string DumpBody = "body";

        private Orbit orbit;
        public SharedObjects Shared { get; set; }
        private string name;
 
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
            name = "<unnamed>";
        }

        private void InitializeSuffixes()
        {
            AddSuffix("NAME", new Suffix<StringValue>(() => name));
            AddSuffix("APOAPSIS", new Suffix<ScalarDoubleValue>(() => orbit.ApA));
            AddSuffix("PERIAPSIS", new Suffix<ScalarDoubleValue>(() => orbit.PeA));
            AddSuffix("BODY", new Suffix<BodyTarget>(() => new BodyTarget(orbit.referenceBody, Shared)));
            AddSuffix("PERIOD", new Suffix<ScalarDoubleValue>(() => orbit.period));
            AddSuffix("INCLINATION", new Suffix<ScalarDoubleValue>(() => orbit.inclination));
            AddSuffix("ECCENTRICITY", new Suffix<ScalarDoubleValue>(() => orbit.eccentricity));
            AddSuffix("SEMIMAJORAXIS", new Suffix<ScalarDoubleValue>(() => orbit.semiMajorAxis));
            AddSuffix("SEMIMINORAXIS", new Suffix<ScalarDoubleValue>(() => orbit.semiMinorAxis));
            AddSuffix(new[]{"LAN", "LONGITUDEOFASCENDINGNODE"}, new Suffix<ScalarDoubleValue>(() => orbit.LAN));
            AddSuffix("ARGUMENTOFPERIAPSIS", new Suffix<ScalarDoubleValue>(() => orbit.argumentOfPeriapsis));
            AddSuffix("TRUEANOMALY", new Suffix<ScalarDoubleValue>(() => Utilities.Utils.DegreeFix(orbit.trueAnomaly,0.0)));
            AddSuffix("MEANANOMALYATEPOCH", new Suffix<ScalarDoubleValue>(() => Utilities.Utils.DegreeFix(orbit.meanAnomalyAtEpoch * 180.0 / Math.PI, 0.0)));
            AddSuffix("TRANSITION", new Suffix<StringValue>(() => orbit.patchEndTransition.ToString()));
            AddSuffix("POSITION", new Suffix<Vector>(() => GetPositionAtUT( new TimeSpan(Planetarium.GetUniversalTime() ) )));
            AddSuffix("VELOCITY", new Suffix<OrbitableVelocity>(() => GetVelocityAtUT( new TimeSpan(Planetarium.GetUniversalTime() ) )));
            AddSuffix("NEXTPATCH", new Suffix<OrbitInfo>(GetNextPatch));
            AddSuffix("HASNEXTPATCH", new Suffix<BooleanValue>(GetHasNextPatch));

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
            return new Vector( orbit.getPositionAtUT( timeStamp.ToUnixStyleTime() ) - Shared.Vessel.findWorldCenterOfMass() );
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
                surfVel = new Vector( orbVel - parent.getRFrmVel( pos + Shared.Vessel.findWorldCenterOfMass()) );
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
            return ! GetHasNextPatch() ? null : new OrbitInfo(orbit.nextPatch,Shared);
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

        public System.Collections.Generic.IDictionary<object, object> Dump()
        {
            DictionaryWithHeader dump = new DictionaryWithHeader
            {
                Header = "ORBIT of " + name
            };

            dump.Add(DumpInclination, orbit.inclination);
            dump.Add(DumpEccentricity, orbit.eccentricity);
            dump.Add(DumpSemiMajorAxis, orbit.semiMajorAxis);
            dump.Add(DumpLongitudeOfAscendingNode, orbit.LAN);
            dump.Add(DumpArgumentOfPeriapsis, orbit.argumentOfPeriapsis);
            dump.Add(DumpMeanAnomalyAtEpoch, orbit.meanAnomalyAtEpoch);
            dump.Add(DumpEpoch, orbit.epoch);
            dump.Add(DumpBody, new BodyTarget(orbit.referenceBody, Shared));

            return dump;
        }

        public void LoadDump(System.Collections.Generic.IDictionary<object, object> dump)
        {
            name = "<unnamed>";

            double inclination = Convert.ToDouble(dump[DumpInclination]);
            double eccentricity = Convert.ToDouble(dump[DumpEccentricity]);
            double semiMajorAxis = Convert.ToDouble(dump[DumpSemiMajorAxis]);
            double longitudeOfAscendingNode = Convert.ToDouble(dump[DumpLongitudeOfAscendingNode]);
            double argumentOfPeriapsis = Convert.ToDouble(dump[DumpArgumentOfPeriapsis]);
            double meanAnomalyAtEpoch = Convert.ToDouble(dump[DumpMeanAnomalyAtEpoch]);
            double epoch = Convert.ToDouble(dump[DumpEpoch]);
            BodyTarget body = dump[DumpBody] as BodyTarget;

            orbit = new Orbit(inclination, eccentricity, semiMajorAxis, longitudeOfAscendingNode, argumentOfPeriapsis,
                meanAnomalyAtEpoch, epoch, body.Body);
        }
    }
}