using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Utilities;
using UnityEngine;
using kOS.Safe.Serialization;
using kOS.Serialization;

namespace kOS.Suffixed
{
    /// <summary>
    /// An Orbitable is any object that is capable of being an item
    /// in orbit around something.  It could be a vessel or a planet
    /// or a moon.
    /// </summary>
    [kOS.Safe.Utilities.KOSNomenclature("Orbitable")]
    public abstract class Orbitable : SerializableStructure
    {
        protected Orbitable(SharedObjects shareObj) : this()
        {
            Shared = shareObj;
        }

        protected Orbitable()
        {
            InitializeSuffixes();
        }

        /// <summary>
        ///   The KSP Orbit object attached to this object.
        /// </summary>
        public abstract Orbit Orbit{get;}

        /// <summary>
        ///   The shared context for the CPU running the code.
        /// </summary>
        public SharedObjects Shared{get; set;}
        
        /// <summary>
        ///   Subclasses must override this method to return the position of this object right now.
        /// </summary>
        /// <returns>
        ///   The coords of the object in the
        ///   <a href='https://ksp-kos.github.io/KOS_DOC/math/geocoordinates.html'>
        ///     raw (cpu-vessel-origin)
        ///   </a>
        ///   coordinate reference frame.
        /// </returns>
        public abstract Vector GetPosition();
        
        /// <summary>
        ///   Subclasses must override this method to return the velocity of this object right now.
        /// </summary>
        /// <returns>
        ///   A OrbitableVelocity object containing both the orbital and surface velocities of the object in the
        ///   <a href='https://ksp-kos.github.io/KOS_DOC/math/geocoordinates.html'>
        ///     raw (soi-body-origin)
        ///   </a>
        ///   coordinate reference frame.
        /// </returns>
        public abstract OrbitableVelocity GetVelocities();

        /// <summary>
        ///   Subclasses must override this method to return the position of this object at some
        ///   arbitrary future time.  It must take into account any orbital transfers to other SOI's
        ///   and any planned maneuver nodes (It should return the predicted location under the
        ///   assumption that the maneuver nodes currently planned will be executed as planned.)
        ///   (Technically it can also describe positions in the past).
        /// </summary>
        /// <param name="timeStamp">The universal time of the future prediction</param>
        /// <returns>
        ///   The coords of the object in the
        ///   <a href='https://ksp-kos.github.io/KOS_DOC/math/geocoordinates.html'>
        ///     raw (cpu-vessel-origin)
        ///   </a>
        ///   coordinate reference frame.
        /// </returns>
        public abstract Vector GetPositionAtUT( TimeStamp timeStamp );

        /// <summary>
        ///   Subclasses must override this method to return the OrbitableVelocity of this object at some
        ///   arbitrary future time.  It must take into account any orbital transfers to other SOI's
        ///   and any planned maneuver nodes (It should return the predicted location under the
        ///   assumption that the maneuver nodes currently planned will be executed as planned.)
        ///   (Technically it can also describe positions in the past).
        /// </summary>
        /// <param name="timeStamp">The universal time of the future prediction</param>
        /// <returns>
        ///   A OrbitableVelocity object containing both the orbital and surface velocities of the object in the
        ///   <a href='https://ksp-kos.github.io/KOS_DOC/math/geocoordinates.html'>
        ///     raw (soi-body-origin)
        ///   </a>
        ///   coordinate reference frame.
        /// </returns>
        public abstract OrbitableVelocity GetVelocitiesAtUT( TimeStamp timeStamp );

        /// <summary>
        ///   Return the Orbit that the object will be in at some point in the future.
        ///   If the object is capable of having maneuver nodes and transitions (a vessel),
        ///   then it should give a prediction under the assumption that the chain of
        ///   maneuver nodes will be executed as planned.
        /// </summary>
        /// <param name="desiredUT">the timestamp when to query for </param>
        /// <returns>An OrbitInfo constructed from the orbit patch in question</returns>
        public abstract Orbit GetOrbitAtUT(double desiredUT);
        
        /// <summary>
        ///   Subclasses must override this method to return a unit vector in
        ///   the upward direction away from its SOI body.
        /// </summary>
        /// <returns>A vector pointing upward away from the SOI body.</returns>
        public abstract Vector GetUpVector();
        
        /// <summary>
        ///   Subclasses must override this method to return a unit vector in
        ///   the northward direction of its SOI body.
        /// </summary>
        /// <returns>A vector pointing northward away from the SOI body.</returns>
        public abstract Vector GetNorthVector();

        /// <summary>
        ///   Subclasses must override this method to return a string name of
        ///   this orbital thing (its vessel name or body name)
        /// </summary>
        /// <returns> string name of the thing</returns>
        public abstract StringValue GetName();

        /// <summary>
        ///   Get the OrbitInfo object associated with this Orbitable.
        /// </summary>
        /// <returns>same as using the :ORB suffix term.</returns>
        public OrbitInfo GetOrbitInfo()
        {
            return new OrbitInfo(this,Shared);
        }

        /// <summary>
        ///   Get the KSP Orbit object associated with this orbitable
        /// </summary>
        /// <returns>same as using the :ORB suffix term.</returns>
        public Orbit GetOrbit()
        {
            return Orbit;
        }
        
        public CelestialBody GetParentBody()
        {
            return (Orbit == null) ? null : Orbit.referenceBody;
        }
        
        public Direction GetPrograde()
        {
            Vector3d up = GetUpVector();
            OrbitableVelocity vels = GetVelocities();
            Vector3d normOrbVec = vels.Orbital.Normalized();

            var d = new Direction {Rotation = Quaternion.LookRotation(normOrbVec, up)};
            return d;
        }

        public Direction GetRetrograde()
        {
            Vector3d up = GetUpVector();
            OrbitableVelocity vels = GetVelocities();
            Vector3d normOrbVec = vels.Orbital.Normalized();

            var d = new Direction {Rotation = Quaternion.LookRotation(normOrbVec*-1, up)};
            return d;
        }

        public Direction GetSurfacePrograde()
        {
            Vector3d up = GetUpVector();
            OrbitableVelocity vels = GetVelocities();
            Vector3d normSrfVec = vels.Surface.Normalized();

            var d = new Direction {Rotation = Quaternion.LookRotation(normSrfVec, up)};
            return d;
        }

        public Direction GetSurfaceRetrograde()
        {
            Vector3d up = GetUpVector();
            OrbitableVelocity vels = GetVelocities();
            Vector3d normSrfVec = vels.Surface.Normalized();

            var d = new Direction {Rotation = Quaternion.LookRotation(normSrfVec*-1, up)};
            return d;
        }

        public double PositionToLatitude( Vector pos )
        {
            CelestialBody parent = GetParentBody();
            if (parent == null) //happens when this Orbitable is the Sun
                return 0.0;
            Vector3d unityWorldPos = GetPosition() + (Vector3d)Shared.Vessel.CoMD;
            return Utils.DegreeFix(parent.GetLatitude(unityWorldPos),-180);
        }
        public double PositionToLongitude( Vector pos )
        {
            CelestialBody parent = GetParentBody();
            if (parent == null) //happens when this Orbitable is the Sun
                return 0.0;
            Vector3d unityWorldPos = GetPosition() + (Vector3d)Shared.Vessel.CoMD;
            return Utils.DegreeFix( parent.GetLongitude(unityWorldPos), -180.0 );
        }
        public double PositionToAltitude( Vector pos )
        {
            CelestialBody parent = GetParentBody();
            if (parent == null) //happens when this Orbitable is the Sun
                return 0.0;
            Vector3d unityWorldPos = GetPosition() + (Vector3d)Shared.Vessel.CoMD;
            return parent.GetAltitude(unityWorldPos);
        }

        private void InitializeSuffixes()
        {
            AddSuffix("NAME", new Suffix<StringValue>(GetName));
            AddSuffix("APOAPSIS", new Suffix<ScalarValue>(() => Orbit.ApA));
            AddSuffix("PERIAPSIS", new Suffix<ScalarValue>(() => Orbit.PeA));
            AddSuffix("BODY", new Suffix<BodyTarget>(() => BodyTarget.CreateOrGetExisting(Orbit.referenceBody, Shared)));
            AddSuffix(new [] {"HASBODY", "HASOBT", "HASORBIT"}, new NoArgsSuffix<BooleanValue>(HasBody));
            AddSuffix("UP", new Suffix<Direction>(() => new Direction(GetUpVector(), false)));
            AddSuffix("NORTH", new Suffix<Direction>(() => new Direction(GetNorthVector(), false)));
            AddSuffix("PROGRADE", new Suffix<Direction>(GetPrograde));
            AddSuffix("RETROGRADE", new Suffix<Direction>(GetRetrograde));
            AddSuffix("SRFPROGRADE", new Suffix<Direction>(GetSurfacePrograde));
            AddSuffix("SRFRETROGRADE", new Suffix<Direction>(GetSurfaceRetrograde));
            AddSuffix(new[] {"OBT","ORBIT"}, new Suffix<OrbitInfo>(GetOrbitInfo));
            AddSuffix("POSITION", new Suffix<Vector>(GetPosition));
            AddSuffix("VELOCITY", new Suffix<OrbitableVelocity>(GetVelocities));
            AddSuffix("DISTANCE", new Suffix<ScalarValue>(GetDistance));
            AddSuffix("DIRECTION", new Suffix<Direction>(() => new Direction(GetPosition(), false)));
            AddSuffix("LATITUDE", new Suffix<ScalarValue>(()=> PositionToLatitude(GetPosition())));
            AddSuffix("LONGITUDE", new Suffix<ScalarValue>(() => PositionToLongitude(GetPosition())));
            AddSuffix("ALTITUDE", new Suffix<ScalarValue>(() => PositionToAltitude(GetPosition())));
            AddSuffix("GEOPOSITION", new Suffix<GeoCoordinates>(() => new GeoCoordinates(this, Shared)));
            AddSuffix("PATCHES", new Suffix<ListValue>(BuildPatchList));
        }

        private ScalarValue GetDistance()
        {
            return GetPosition().Magnitude();
        }
        
        private BooleanValue HasBody()
        {
            return (Orbit != null);
        }

        private ListValue BuildPatchList()
        {
            var list = new ListValue();
            var orb = Orbit;
            int index = 0;
            int highestAllowedIndex = Career.PatchLimit();
            while (index <= highestAllowedIndex)
            {
                if (orb == null || !orb.activePatch)
                {
                    break;
                }

                list.Add(new OrbitInfo(orb, Shared));
                orb = orb.nextPatch;
                ++index;
            }
            return list;
        }
    }
}
