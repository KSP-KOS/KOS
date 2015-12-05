using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using System;
using kOS.Serialization;
using kOS.Safe.Serialization;

namespace kOS.Suffixed
{
    public class OrbitInfo : Structure, IDumperWithSharedObjects
    {
        public static string DUMP_INCLINATION = "inclination";
        public static string DUMP_ECCENTRICITY = "eccentricity";
        public static string DUMP_SEMI_MAJOR_AXIS = "semi_major_axis";
        public static string DUMP_LONGITUDE_OF_ASCENDING_NODE = "longitude_of_ascending_node";
        public static string DUMP_ARGUMENT_OF_PERIAPSIS = "argument_of_periapsis";
        public static string DUMP_MEAN_ANOMALY_AT_EPOCH = "mean_anomaly_at_epoch";
        public static string DUMP_EPOCH = "epoch";
        public static string DUMP_BODY = "body";

        private Orbit orbit;
        private SharedObjects shared;
        private string name;
 
        public OrbitInfo()
        {
            InitializeSuffixes();
        }

        public OrbitInfo(Orbitable orb, SharedObjects sharedObj) : this()
        {
            orbit = orb.Orbit;
            shared = sharedObj;
            name = orb.GetName();
        }
        
        public OrbitInfo( Orbit orb, SharedObjects sharedObj) : this()
        {
            shared = sharedObj;
            orbit = orb;
            name = "<unnamed>";
        }

        private void InitializeSuffixes()
        {
            AddSuffix("NAME", new Suffix<string>(() => name));
            AddSuffix("APOAPSIS", new Suffix<double>(() => orbit.ApA));
            AddSuffix("PERIAPSIS", new Suffix<double>(() => orbit.PeA));
            AddSuffix("BODY", new Suffix<BodyTarget>(() => new BodyTarget(orbit.referenceBody, shared)));
            AddSuffix("PERIOD", new Suffix<double>(() => orbit.period));
            AddSuffix("INCLINATION", new Suffix<double>(() => orbit.inclination));
            AddSuffix("ECCENTRICITY", new Suffix<double>(() => orbit.eccentricity));
            AddSuffix("SEMIMAJORAXIS", new Suffix<double>(() => orbit.semiMajorAxis));
            AddSuffix("SEMIMINORAXIS", new Suffix<double>(() => orbit.semiMinorAxis));
            AddSuffix(new[]{"LAN", "LONGITUDEOFASCENDINGNODE"}, new Suffix<double>(() => orbit.LAN));
            AddSuffix("ARGUMENTOFPERIAPSIS", new Suffix<double>(() => orbit.argumentOfPeriapsis));
            AddSuffix("TRUEANOMALY", new Suffix<double>(() => Utilities.Utils.DegreeFix(orbit.trueAnomaly,0.0)));
            AddSuffix("MEANANOMALYATEPOCH", new Suffix<double>(() => Utilities.Utils.DegreeFix(orbit.meanAnomalyAtEpoch * 180.0 / Math.PI, 0.0)));
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

        public void SetSharedObjects(SharedObjects sharedObjects)
        {
            shared = sharedObjects;
        }

        public System.Collections.Generic.IDictionary<object, object> Dump()
        {
            DictionaryWithHeader dump = new DictionaryWithHeader();

            dump.Header = "ORBIT of " + name;
            dump.Add(DUMP_INCLINATION, orbit.inclination);
            dump.Add(DUMP_ECCENTRICITY, orbit.eccentricity);
            dump.Add(DUMP_SEMI_MAJOR_AXIS, orbit.semiMajorAxis);
            dump.Add(DUMP_LONGITUDE_OF_ASCENDING_NODE, orbit.LAN);
            dump.Add(DUMP_ARGUMENT_OF_PERIAPSIS, orbit.argumentOfPeriapsis);
            dump.Add(DUMP_MEAN_ANOMALY_AT_EPOCH, orbit.meanAnomalyAtEpoch);
            dump.Add(DUMP_EPOCH, orbit.epoch);
            dump.Add(DUMP_BODY, new BodyTarget(orbit.referenceBody, shared));

            return dump;
        }

        public void LoadDump(System.Collections.Generic.IDictionary<object, object> dump)
        {
            name = "<unnamed>";

            double inclination = Convert.ToDouble(dump[DUMP_INCLINATION]);
            double eccentricity = Convert.ToDouble(dump[DUMP_ECCENTRICITY]);
            double semi_major_axis = Convert.ToDouble(dump[DUMP_SEMI_MAJOR_AXIS]);
            double longitude_of_ascending_node = Convert.ToDouble(dump[DUMP_LONGITUDE_OF_ASCENDING_NODE]);
            double argument_of_periapsis = Convert.ToDouble(dump[DUMP_ARGUMENT_OF_PERIAPSIS]);
            double mean_anomaly_at_epoch = Convert.ToDouble(dump[DUMP_MEAN_ANOMALY_AT_EPOCH]);
            double epoch = Convert.ToDouble(dump[DUMP_EPOCH]);
            BodyTarget body = dump[DUMP_BODY] as BodyTarget;

            orbit = new Orbit(inclination, eccentricity, semi_major_axis, longitude_of_ascending_node, argument_of_periapsis,
                mean_anomaly_at_epoch, epoch, body.Body);
        }
    }
}