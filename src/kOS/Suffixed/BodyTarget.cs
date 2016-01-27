using kOS.Safe.Encapsulation.Suffixes;
using kOS.Utilities;
using UnityEngine;
using System;
using kOS.Serialization;
using System.Collections.Generic;
using kOS.Safe.Encapsulation;
using kOS.Safe.Serialization;
using kOS.Safe.Exceptions;

namespace kOS.Suffixed
{
    public class BodyTarget : Orbitable, IKOSTargetable, IDumperWithSharedObjects
    {
        private static string DumpName = "name";

        public CelestialBody Body { get; set; }

        public override Orbit Orbit { get { return Body.orbit; } }

        public override StringValue GetName()
        {
            return Body.name;
        }

        public override Vector GetPosition()
        {
            return new Vector(Body.position - Shared.Vessel.findWorldCenterOfMass());
        }

        public override OrbitableVelocity GetVelocities()
        {
            return new OrbitableVelocity(Body, Shared);
        }

        public override Vector GetPositionAtUT(TimeSpan timeStamp)
        {
            return new Vector(Body.getPositionAtUT(timeStamp.ToUnixStyleTime()) - Shared.Vessel.findWorldCenterOfMass());
        }

        public override OrbitableVelocity GetVelocitiesAtUT(TimeSpan timeStamp)
        {
            CelestialBody parent = Body.KOSExtensionGetParentBody();
            if (parent == null) // only if Body is Sun and therefore has no parent, then do more complex work instead because KSP didn't provide a way itself
            {
                Vector3d futureOrbitalVel;
                CelestialBody soiBody = Shared.Vessel.mainBody;
                if (soiBody.orbit != null)
                    futureOrbitalVel = soiBody.orbit.GetFrameVelAtUT(timeStamp.ToUnixStyleTime());
                else
                    futureOrbitalVel = -1 * new VesselTarget(Shared.Vessel, Shared).GetVelocitiesAtUT(timeStamp).Orbital.ToVector3D();
                return new OrbitableVelocity(new Vector(futureOrbitalVel), new Vector(0.0, 0.0, 0.0));
            }

            var orbVel = new Vector(Orbit.getOrbitalVelocityAtUT(timeStamp.ToUnixStyleTime()));
            orbVel = new Vector(orbVel.X, orbVel.Z, orbVel.Y); // swap Y and Z because KSP API is weird.

            var surfVel = new Vector(Body.orbit.GetVel() - parent.getRFrmVel(Body.position));

            return new OrbitableVelocity(orbVel, surfVel);
        }

        public override Orbit GetOrbitAtUT(double desiredUT)
        {
            return Orbit;  // Bodies cannot transition and are always on rails so this is constant.
        }

        public override Vector GetUpVector()
        {
            CelestialBody parent = Body.KOSExtensionGetParentBody();
            return parent == null ?
                new Vector(0.0, 0.0, 0.0) :
                new Vector((Body.position - parent.position).normalized);
        }

        public override Vector GetNorthVector()
        {
            CelestialBody parent = Body.KOSExtensionGetParentBody() ?? Body;
            return new Vector(Vector3d.Exclude(GetUpVector(), parent.transform.up));
        }

        public BodyTarget()
        {
            BodyInitializeSuffixes();
        }

        public BodyTarget(string name, SharedObjects shareObj)
            : this(VesselUtils.GetBodyByName(name), shareObj)
        {
            BodyInitializeSuffixes();
        }

        public BodyTarget(CelestialBody body, SharedObjects shareObj)
            : base(shareObj)
        {
            Body = body;
            BodyInitializeSuffixes();
        }

        private void BodyInitializeSuffixes()
        {
            AddSuffix("NAME", new Suffix<StringValue>(() => Body.name));
            AddSuffix("DESCRIPTION", new Suffix<StringValue>(() => Body.bodyDescription));
            AddSuffix("MASS", new Suffix<ScalarDoubleValue>(() => Body.Mass));
            AddSuffix("ALTITUDE", new Suffix<ScalarDoubleValue>(() => Body.orbit.altitude));
            AddSuffix("RADIUS", new Suffix<ScalarDoubleValue>(() => Body.Radius));
            AddSuffix("MU", new Suffix<ScalarDoubleValue>(() => Body.gravParameter));
            AddSuffix("ROTATIONPERIOD", new Suffix<ScalarDoubleValue>(() => Body.rotationPeriod));
            AddSuffix("ATM", new Suffix<BodyAtmosphere>(() => new BodyAtmosphere(Body)));
            AddSuffix("ANGULARVEL", new Suffix<Vector>(() => RawAngularVelFromRelative(Body.angularVelocity)));
            AddSuffix("SOIRADIUS", new Suffix<ScalarDoubleValue>(() => Body.sphereOfInfluence));
            AddSuffix("ROTATIONANGLE", new Suffix<ScalarDoubleValue>(() => Body.rotationAngle));
            AddSuffix("GEOPOSITIONOF",
                      new OneArgsSuffix<GeoCoordinates, Vector>(
                              GeoCoordinatesFromPosition,
                              "Interpret the vector given as a 3D position, and return the geocoordinates directly underneath it on this body."));
            AddSuffix("ALTITUDEOF",
                      new OneArgsSuffix<ScalarDoubleValue, Vector>(
                              AltitudeFromPosition,
                              "Interpret the vector given as a 3D position, and return its altitude above 'sea level' of this body."));
        }

        /// <summary>
        /// Interpret the vector given as a 3D position, and return the geocoordinates directly underneath it on this body.
        /// </summary>
        /// <param name="position">Vector to use as the 3D position in ship-raw coords</param>
        /// <returns>The GeoCoordinates under the position.</returns>
        public GeoCoordinates GeoCoordinatesFromPosition(Vector position)
        {
            Vector3d unityWorldPosition = Shared.Vessel.findWorldCenterOfMass() + position.ToVector3D();
            double lat = Body.GetLatitude(unityWorldPosition);
            double lng = Body.GetLongitude(unityWorldPosition);
            return new GeoCoordinates(Body, Shared, lat, lng);
        }

        /// <summary>
        /// Interpret the vector given as a 3D position, and return the altitude above sea level of this body.
        /// </summary>
        /// <param name="position">Vector to use as the 3D position in ship-raw coords</param>
        /// <returns>The altitude above 'sea level'.</returns>
        public ScalarDoubleValue AltitudeFromPosition(Vector position)
        {
            Vector3d unityWorldPosition = Shared.Vessel.findWorldCenterOfMass() + position.ToVector3D();
            return Body.GetAltitude(unityWorldPosition);
        }

        /// <summary>
        /// Annoyingly, KSP returns CelestialBody.angularVelociy in a frame of reference 
        /// relative to the ship facing instead of the universe facing.  This would be
        /// wonderful if that was their philosophy everywhere, but it's not - its just a
        /// weird exception for this one case.  This transforms it back into raw universe
        /// axes again:
        /// </summary>
        /// <param name="angularVelFromKSP">the value KSP is returning for angular velocity</param>
        /// <returns>altered velocity in the new reference frame</returns>
        private Vector RawAngularVelFromRelative(Vector3 angularVelFromKSP)
        {
            return new Vector(VesselUtils.GetFacing(Body).Rotation *
                              new Vector3d(angularVelFromKSP.x, -angularVelFromKSP.z, angularVelFromKSP.y));
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

        protected bool Equals(BodyTarget other)
        {
            return Body.Equals(other.Body);
        }

        public static bool operator ==(BodyTarget left, BodyTarget right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(BodyTarget left, BodyTarget right)
        {
            return !Equals(left, right);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((BodyTarget)obj);
        }

        public override int GetHashCode()
        {
            return Body.name.GetHashCode();
        }

        public void SetSharedObjects(SharedObjects sharedObjects)
        {
            Shared = sharedObjects;
        }

        public IDictionary<object, object> Dump()
        {
            var dump = new DictionaryWithHeader
            {
                Header = string.Format("BODY '{0}'", Body.bodyName)
            };

            dump.Add(DumpName, Body.bodyName);

            return dump;
        }

        public void LoadDump(IDictionary<object, object> dump)
        {
            string name = dump[DumpName] as string;

            if (name == null)
            {
                throw new KOSSerializationException("Body's name is null or invalid");
            }

            CelestialBody body = VesselUtils.GetBodyByName(name);

            if (body == null)
            {
                throw new KOSSerializationException("Body with the given name does not exist");
            }

            Body = body;

        }
    }
}