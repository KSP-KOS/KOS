
using System;
using kOS.Utilities;
using UnityEngine;

namespace kOS.Suffixed
{
    /// <summary>
    /// An Orbitable is any object that is capable of being an item
    /// in orbit around something.  It could be a vessel or a planet
    /// or a moon.
    /// </summary>
    abstract public class Orbitable : SpecialValue
    {

        /// <summary>
        ///   The KSP Orbit object attached to this object.  Subclasses must
        ///   override this.
        /// </summary>
        abstract public Orbit orbit{get;set;}

        /// <summary>
        ///   The shared context for the CPU running the code.  Subclasses must
        ///   override this.
        /// </summary>
        abstract public SharedObjects shared{get;set;}
        
        /// <summary>
        ///   Subclasses must override this method to return the position of this object right now.
        /// </summary>
        /// <returns>
        ///   The coords of the object in the
        ///   <a href='http://ksp-kos.github.io/KOS_DOC/summary_topics/coordframe/raw/'>
        ///     raw (cpu-vessel-origin)
        ///   </a>
        ///   coordinate reference frame.
        /// </returns>
        abstract public Vector GetPosition();
        
        /// <summary>
        ///   Subclasses must override this method to return the velocity of this object right now.
        /// </summary>
        /// <returns>
        ///   A Velocities object containing both the oribtal and surface velocities of the object in the
        ///   <a href='http://ksp-kos.github.io/KOS_DOC/summary_topics/coordframe/raw/'>
        ///     raw (soi-body-origin)
        ///   </a>
        ///   coordinate reference frame.
        /// </returns>
        abstract public Velocities GetVelocities();

        /// <summary>
        ///   Subclasses must orverride this method to return the position of this object at some
        ///   arbitrary future time.  It must take into account any orbital transfers to other SOI's
        ///   and any planned maneuver nodes (It should return the predicted location under the
        ///   assumption that the maneuver nodes currently planned will be executed as planned.)
        ///   (Technically it can also describe positions in the past).
        /// </summary>
        /// <param name="timeStamp">The universal time of the future prediction</param>
        /// <returns>
        ///   The coords of the object in the
        ///   <a href='http://ksp-kos.github.io/KOS_DOC/summary_topics/coordframe/raw/'>
        ///     raw (cpu-vessel-origin)
        ///   </a>
        ///   coordinate reference frame.
        /// </returns>
        abstract public Vector GetPositionAtUT( TimeSpan timeStamp );

        /// <summary>
        ///   Subclasses must override this method to return the Velocities of this object at some
        ///   arbitrary future time.  It must take into account any orbital transfers to other SOI's
        ///   and any planned maneuver nodes (It should return the predicted location under the
        ///   assumption that the maneuver nodes currently planned will be executed as planned.)
        ///   (Technically it can also describe positions in the past).
        /// </summary>
        /// <param name="timeStamp">The universal time of the future prediction</param>
        /// <returns>
        ///   A Velocities object containing both the oribtal and surface velocities of the object in the
        ///   <a href='http://ksp-kos.github.io/KOS_DOC/summary_topics/coordframe/raw/'>
        ///     raw (soi-body-origin)
        ///   </a>
        ///   coordinate reference frame.
        /// </returns>
        abstract public Velocities GetVelocitiesAtUT( TimeSpan timeStamp );
        
        /// <summary>
        ///   Subclasses must override this method to return a unit vector in
        ///   the upward direction away from its SOI body.
        /// </summary>
        /// <returns>A vector pointing upward away from the SOI body.</returns>
        abstract public Vector GetUpVector();
        
        /// <summary>
        ///   Subclasses must override this method to return a unit vector in
        ///   the northward direction of its SOI body.
        /// </summary>
        /// <returns>A vector pointing northward away from the SOI body.</returns>
        abstract public Vector GetNorthVector();

        /// <summary>
        ///   Subclasses must override this method to return a string name of
        ///   this orbital thing (its vessel name or body name)
        /// </summary>
        /// <returns> string name of the thing</returns>
        abstract public string GetName();

        /// <summary>
        ///   Get the OrbitInfo object assocaited with this Orbitable.
        /// </summary>
        /// <returns>same as using the :ORB suffix term.</returns>
        public OrbitInfo GetOrbitInfo()
        {
            return new OrbitInfo(this,shared);
        }

        /// <summary>
        ///   Get the KSP Orbit object associated with this orbitable
        /// </summary>
        /// <returns>same as using the :ORB suffix term.</returns>
        public Orbit GetOrbit()
        {
            return orbit;
        }

        public CelestialBody GetParentBody()
        {
            return orbit.referenceBody;
        }
        
        public Direction GetPrograde()
        {
            Vector3d up = GetUpVector().ToVector3D();
            Velocities vels = GetVelocities();
            Vector3d normOrbVec = vels.orbital.normalized().ToVector3D();

            var d = new Direction {Rotation = Quaternion.LookRotation(normOrbVec, up)};
            return d;
        }

        public Direction GetRetrograde()
        {
            Vector3d up = GetUpVector().ToVector3D();
            Velocities vels = GetVelocities();
            Vector3d normOrbVec = vels.orbital.normalized().ToVector3D();

            var d = new Direction {Rotation = Quaternion.LookRotation(normOrbVec*(-1), up)};
            return d;
        }

        public Direction GetSurfacePrograde()
        {
            Vector3d up = GetUpVector().ToVector3D();
            Velocities vels = GetVelocities();
            Vector3d normSrfVec = vels.surface.normalized().ToVector3D();

            var d = new Direction {Rotation = Quaternion.LookRotation(normSrfVec, up)};
            return d;
        }

        public Direction GetSurfaceRetrograde()
        {
            Vector3d up = GetUpVector().ToVector3D();
            Velocities vels = GetVelocities();
            Vector3d normSrfVec = vels.surface.normalized().ToVector3D();

            var d = new Direction {Rotation = Quaternion.LookRotation(normSrfVec*(-1), up)};
            return d;
        }


        public double PositionToLatitude( Vector pos )
        {
            CelestialBody parent = orbit.referenceBody;
            if (parent == null) //happens when this Orbitable is the Sun
                return 0.0;
            Vector3d unityWorldPos = GetPosition() + shared.Vessel.GetWorldPos3D();
            return parent.GetLatitude(unityWorldPos);
        }
        public double PositionToLongitude( Vector pos )
        {
            CelestialBody parent = orbit.referenceBody;
            if (parent == null) //happens when this Orbitable is the Sun
                return 0.0;
            Vector3d unityWorldPos = GetPosition() + shared.Vessel.GetWorldPos3D();
            return Utils.DegreeFix( parent.GetLongitude(unityWorldPos), -180.0 );
        }
        public double PositionToAltitude( Vector pos )
        {
            CelestialBody parent = orbit.referenceBody;
            if (parent == null) //happens when this Orbitable is the Sun
                return 0.0;
            Vector3d unityWorldPos = GetPosition() + shared.Vessel.GetWorldPos3D();
            return parent.GetAltitude(unityWorldPos);
        }
        
        public object GetOnlyOrbitableSuffixes( string suffixName )
        {
            // This is a separate call so that it is possible to distinguish
            // those suffixes that are in the base class from the ones in
            // the subclasses.  If you want ONLY the base class terms to be
            // parsed, without parsing terms defined in subclasses, then
            // call GetOnlyOrbitalSuffixes.
            switch(suffixName)
            {
                // These first cases are an exact copy of what Orbit object did,
                // for backward compatibility:

                case "NAME":
                    return GetName();
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
                    
                // The cases after this point were added to Orbitable from either VesselTarget or BodyTarget:
                
                case "UP":
                    return new Direction(GetUpVector().ToVector3D(), false);
                case "NORTH":
                    return new Direction(GetNorthVector().ToVector3D(), false);
                case "PROGRADE":
                    return GetPrograde();
                case "RETROGRADE":
                    return GetRetrograde();
                case "SRFPROGRADE":
                    return GetSurfacePrograde();
                case "SRFRETROGRADE":
                    return GetSurfaceRetrograde();
                case "OBT":
                    return GetOrbitInfo();
                case "POSITION":
                    return GetPosition();
                case "VELOCITY":
                    return GetVelocities();
                case "DISTANCE":
                    return GetPosition().magnitude();
                case "DIRECTION":
                    return new Direction(GetPosition(), false);
                case "LATITUDE":
                    return PositionToLatitude( GetPosition() );
                case "LONGITUDE":
                    return PositionToLongitude( GetPosition() );
                case "ALTITUDE":
                    return PositionToAltitude( GetPosition() );
                case "GEOPOSITION":
                    return new GeoCoordinates(this, shared);
            }
            return null;
        }

        public override object GetSuffix( string suffixName )
        {
            object returnVal = GetOnlyOrbitableSuffixes(suffixName);

            if (returnVal == null)
                return base.GetSuffix(suffixName);
            else
                return returnVal;
        }
        
        public override string ToString()
        {
            return "Orbitable"; // Should be overridden by subclasses, I hope.
        }
    }
}
