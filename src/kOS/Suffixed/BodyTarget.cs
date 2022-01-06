using kOS.Safe.Encapsulation.Suffixes;
using kOS.Utilities;
using UnityEngine;
using System;
using kOS.Serialization;
using System.Collections.Generic;
using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;
using kOS.Safe;
using kOS.Safe.Serialization;

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("Body")]
    public class BodyTarget : Orbitable, IKOSTargetable
    {
        private const string DumpName = "name";

        public CelestialBody Body { get; set; }

        public override Orbit Orbit { get { return Body.orbit; } }

        private static Dictionary<InstanceKey, WeakReference> instanceCache;

        private BodyTarget(CelestialBody body, SharedObjects shareObj)
            : base(shareObj)
        {
            Body = body;
            BodyInitializeSuffixes();
        }

        /// <summary>
        /// Factory method you should use instead of the constructor for this class.
        /// This will construct a new instance if and only if there isn't already
        /// an instance made for this particular kOSProcessor, for the given body
        /// (Uniqueness determinied by the Body's text name.  If someone makes
        /// a modded solar system with two bodies having the same name, that
        /// will be a problem (but who would do that??) ).
        /// If an instance already exists it will return a reference to that instead of making
        /// a new one.
        /// The reason this enforcement is needed is because BodyTarget has callback hooks
        /// that prevent orphaning and garbage collection.  (The delegate inserted
        /// into KSP's GameEvents counts as a reference to the BodyTarget.)
        /// Using this factory method instead of a constructor prevents having multiple
        /// instances of BodyTarget.
        /// </summary>
        /// <returns>The or get.</returns>
        /// <param name="body">celestial body to make the wrapper for</param>
        /// <param name="Shared">kOS shared objects reference</param>
        public static BodyTarget CreateOrGetExisting(CelestialBody body, SharedObjects shared)
        {
            if (instanceCache == null)
                instanceCache = new Dictionary<InstanceKey, WeakReference>();

            InstanceKey key = new InstanceKey { ProcessorId = shared.Processor.KOSCoreId, BodyName = body.name };
            if (instanceCache.ContainsKey(key))
            {
                WeakReference weakRef = instanceCache[key];
                if (weakRef.IsAlive)
                    return (BodyTarget)weakRef.Target;
                else
                    instanceCache.Remove(key);
            }
            // If it either wasn't in the cache, or it was but the GC destroyed it by now, make a new one:
            BodyTarget newlyConstructed = new BodyTarget(body, shared);
            instanceCache.Add(key, new WeakReference(newlyConstructed));
            return newlyConstructed;
        }

        public static void ClearInstanceCache()
        {
            if (instanceCache == null)
                instanceCache = new Dictionary<InstanceKey, WeakReference>();
            else
                instanceCache.Clear();
        }

        public static BodyTarget CreateOrGetExisting(string bodyName, SharedObjects shared)
        {
            var bod = VesselUtils.GetBodyByName(bodyName);
            if (bod == null)
                throw new KOSInvalidArgumentException("BODY() constructor", bodyName, "Body not found in this solar system");

            return CreateOrGetExisting(bod, shared);
        }

        private void BodyInitializeSuffixes()
        {
            AddSuffix("NAME", new Suffix<StringValue>(() => Body.name));
            AddSuffix("DESCRIPTION", new Suffix<StringValue>(() => Body.bodyDescription));
            AddSuffix("MASS", new Suffix<ScalarValue>(() => Body.Mass));
            AddSuffix("HASOCEAN", new Suffix<BooleanValue>(() => Body.ocean));
            AddSuffix("HASSOLIDSURFACE", new Suffix<BooleanValue>(() => Body.hasSolidSurface));
            AddSuffix("ORBITINGCHILDREN", new Suffix<ListValue>(GetOrbitingChildren));
            AddSuffix("ALTITUDE", new Suffix<ScalarValue>(() => Body.orbit.altitude));
            AddSuffix("RADIUS", new Suffix<ScalarValue>(() => Body.Radius));
            AddSuffix("MU", new Suffix<ScalarValue>(() => Body.gravParameter));
            AddSuffix("ROTATIONPERIOD", new Suffix<ScalarValue>(() => Body.rotationPeriod));
            AddSuffix("ATM", new Suffix<BodyAtmosphere>(() => new BodyAtmosphere(Body, Shared)));
            AddSuffix("ANGULARVEL", new Suffix<Vector>(() => RawAngularVelFromRelative(Body.angularVelocity)));
            AddSuffix("SOIRADIUS", new Suffix<ScalarValue>(() => Body.sphereOfInfluence));
            AddSuffix("ROTATIONANGLE", new Suffix<ScalarValue>(() => Body.rotationAngle));
            AddSuffix("GEOPOSITIONOF",
                      new OneArgsSuffix<GeoCoordinates, Vector>(
                              GeoCoordinatesFromPosition,
                              "Interpret the vector given as a 3D position, and return the geocoordinates directly underneath it on this body."));
            AddSuffix("ALTITUDEOF",
                      new OneArgsSuffix<ScalarValue, Vector>(
                              AltitudeFromPosition,
                              "Interpret the vector given as a 3D position, and return its altitude above 'sea level' of this body."));
            AddSuffix("GEOPOSITIONLATLNG",
                      new TwoArgsSuffix<GeoCoordinates, ScalarValue, ScalarValue>(
                              GeoCoordinatesFromLatLng,
                              "Given latitude and longitude, return the geoposition on this body corresponding to it."));
        }

        public ListValue GetOrbitingChildren()
        {
            var toReturn = new ListValue();
            foreach (CelestialBody body in Body.orbitingBodies) {
                toReturn.Add(CreateOrGetExisting(body,Shared));
            }
            return toReturn;
        }

        public override StringValue GetName()
        {
            return Body.name;
        }

        public override Vector GetPosition()
        {
            return new Vector(Body.position - Shared.Vessel.CoMD);
        }

        public override OrbitableVelocity GetVelocities()
        {
            return new OrbitableVelocity(Body, Shared);
        }

        public override Vector GetPositionAtUT(TimeStamp timeStamp)
        {
            return new Vector(Body.getPositionAtUT(timeStamp.ToUnixStyleTime()) - Shared.Vessel.CoMD);
        }

        public override OrbitableVelocity GetVelocitiesAtUT(TimeStamp timeStamp)
        {
            CelestialBody parent = Body.KOSExtensionGetParentBody();
            if (parent == null) // only if Body is Sun and therefore has no parent, then do more complex work instead because KSP didn't provide a way itself
            {
                Vector3d futureOrbitalVel;
                CelestialBody soiBody = Shared.Vessel.mainBody;
                if (soiBody.orbit != null)
                    futureOrbitalVel = soiBody.orbit.GetFrameVelAtUT(timeStamp.ToUnixStyleTime());
                else
                    futureOrbitalVel = -1 * VesselTarget.CreateOrGetExisting(Shared.Vessel, Shared).GetVelocitiesAtUT(timeStamp).Orbital.ToVector3D();
                Vector swappedVel = new Vector(futureOrbitalVel.x, futureOrbitalVel.z, futureOrbitalVel.y); // swap Y and Z because KSP API is weird.
                 // Also invert directions because the above gives vel of my body rel to sun, and I want vel of sun rel to my body:
                return new OrbitableVelocity( -swappedVel, -swappedVel);
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

        /// <summary>
        /// Interpret the vector given as a 3D position, and return the geocoordinates directly underneath it on this body.
        /// </summary>
        /// <param name="position">Vector to use as the 3D position in ship-raw coords</param>
        /// <returns>The GeoCoordinates under the position.</returns>
        public GeoCoordinates GeoCoordinatesFromPosition(Vector position)
        {
            Vector3d unityWorldPosition = Shared.Vessel.CoMD + position.ToVector3D();
            double lat = Body.GetLatitude(unityWorldPosition);
            double lng = Body.GetLongitude(unityWorldPosition);
            return new GeoCoordinates(Body, Shared, lat, lng);
        }

        /// <summary>
        /// Return a Geocoordinates on this body, given the latitude and longitude
        /// </summary>
        /// <returns>The LATLNG (GeoCoordinates) structure.</returns>
        public GeoCoordinates GeoCoordinatesFromLatLng(ScalarValue latitude, ScalarValue longitude)
        {
            return new GeoCoordinates(Body, Shared, latitude, longitude);
        }

        /// <summary>
        /// Interpret the vector given as a 3D position, and return the altitude above sea level of this body.
        /// </summary>
        /// <param name="position">Vector to use as the 3D position in ship-raw coords</param>
        /// <returns>The altitude above 'sea level'.</returns>
        public ScalarValue AltitudeFromPosition(Vector position)
        {
            Vector3d unityWorldPosition = Shared.Vessel.CoMD + position.ToVector3D();
            return Body.GetAltitude(unityWorldPosition);
        }

        /// <summary>
        /// Interpret the vector given as a 3D position, and return the altitude above terrain unless
        /// that terrain is below sea level on a world that has a sea, in which case return the sea
        /// level atitude instead, similar to how radar altitude is displayed to the player.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public ScalarValue RadarAltitudeFromPosition(Vector position)
        {
            GeoCoordinates geo = GeoCoordinatesFromPosition(position);
            ScalarValue terrainHeight = geo.GetTerrainAltitude();
            ScalarValue seaAlt = AltitudeFromPosition(position);
            if (Body.ocean && terrainHeight < 0)
                return seaAlt;
            else
                return seaAlt - terrainHeight;
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
            return Vector3d.Distance(Shared.Vessel.CoMD, Body.position) - Body.Radius;
        }

        public override ISuffixResult GetSuffix(string suffixName, bool failOkay)
        {
            if (Target == null) throw new Exception("BODY structure appears to be empty!");
            return base.GetSuffix(suffixName, failOkay);
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

        public override Dump Dump(DumperState s)
        {
            DumpDictionary dump = new DumpDictionary(this.GetType());

            dump.Add(DumpName, Body.bodyName);

            return dump;
        }

        [DumpDeserializer]
        public static BodyTarget CreateFromDump(DumpDictionary d, SafeSharedObjects shared)
        {
            string name = d.GetString(DumpName);
            CelestialBody body = VesselUtils.GetBodyByName(name);

            if (body == null)
            {
                throw new KOSSerializationException("Body with the given name does not exist");
            }

            return CreateOrGetExisting(body, shared as SharedObjects);
        }

        [DumpPrinter]
        public static void Print(DumpDictionary dump, IndentedStringBuilder sb)
        {
            sb.Append("BODY(\"");

            string name = dump.GetString(DumpName);
            sb.Append(name.Replace("\"", "\"\""));

            sb.Append("\")");
        }

        // The data that identifies a unique instance of this class, for use
        // with the factory method that avoids duplicate instances:
        private struct InstanceKey
        {
            /// <summary>The kOSProcessor Module that built me.</summary>
            public int ProcessorId { get; set; }

            /// <summary>The KSP Body that I'm wrapping.</summary>
            public string BodyName { get; set; }
        }
    }
}
