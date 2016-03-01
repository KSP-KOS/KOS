﻿using UnityEngine;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Utilities;
using kOS.Serialization;
using kOS.Safe.Serialization;
using System;
using kOS.Safe;

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("GeoCoordinates")]
    [kOS.Safe.Utilities.KOSNomenclature("LatLng", CSharpToKOS = false)]
    public class GeoCoordinates : SerializableStructure
    {
        private static string DumpLat = "lat";
        private static string DumpLng = "lng";
        private static string DumpBody = "body";

        private double lat;
        private double lng;
        public double Latitude
        {
            get
            {
                return Utils.DegreeFix(lat,-180);
            }
            private set
            {
                lat = value;
            }
        }
        public double Longitude
        {
            get
            {
                return Utils.DegreeFix(lng,-180);
            }
            private set
            {
                lng = value;
            }
        }
        public CelestialBody Body { get; private set; }
        public SharedObjects Shared { get; set; } // for finding the current CPU's vessel, as per issue #107

        private const int TERRAIN_MASK_BIT = 15;

        public GeoCoordinates()
        {
            GeoCoordsInitializeSuffixes();
        }

        /// <summary>
        ///   Build a GeoCoordinates from the current lat/long of the orbitable
        ///   object passed in.  The object being checked for should be in the same
        ///   SOI as the vessel running the CPU or the results are meaningless.
        /// </summary>
        /// <param name="orb">object to take current coords of</param>
        /// <param name="sharedObj">to know the current CPU's running vessel</param>
        public GeoCoordinates(Orbitable orb, SharedObjects sharedObj) : this()
        {
            Shared = sharedObj;
            Vector p = orb.GetPosition();
            Latitude = orb.PositionToLatitude(p);
            Longitude = orb.PositionToLongitude(p);
            Body = orb.GetParentBody();
        }

        /// <summary>
        ///   Build a GeoCoordinates from any arbitrary lat/long pair of floats.
        /// </summary>
        /// <param name="body">A different celestial body to select a lat/long for that might not be the current one</param>
        /// <param name="sharedObj">to know the current CPU's running vessel</param>
        /// <param name="latitude">latitude</param>
        /// <param name="longitude">longitude</param>
        public GeoCoordinates(CelestialBody body, SharedObjects sharedObj, double latitude, double longitude) : this()
        {
            Latitude = latitude;
            Longitude = longitude;
            Shared = sharedObj;
            Body = body;
        }

        /// <summary>
        ///   Build a GeoCoordinates from any arbitrary lat/long pair of floats.
        /// </summary>
        /// <param name="sharedObj">to know the current CPU's running vessel</param>
        /// <param name="latitude">latitude</param>
        /// <param name="longitude">longitude</param>
        public GeoCoordinates(SharedObjects sharedObj, float latitude, float longitude) :
            this(sharedObj.Vessel.GetOrbit().referenceBody, sharedObj, latitude, longitude)
        {
        }

        /// <summary>
        ///   Build a GeoCoordinates from any arbitrary lat/long pair of doubles.
        /// </summary>
        /// <param name="sharedObj">to know the current CPU's running vessel</param>
        /// <param name="latitude">latitude</param>
        /// <param name="longitude">longitude</param>
        public GeoCoordinates(SharedObjects sharedObj, double latitude, double longitude) : this()
        {
            Latitude = latitude;
            Longitude = longitude;
            Shared = sharedObj;
            Body = Shared.Vessel.GetOrbit().referenceBody;
        }

        /// <summary>
        ///   The bearing from the current CPU vessel to the surface spot with the
        ///   given lat/long coords, relative to the current CPU vessel's heading.
        /// </summary>
        /// <returns> bearing </returns>
        public ScalarValue GetBearing()
        {
            return VesselUtils.AngleDelta(VesselUtils.GetHeading(Shared.Vessel), (float) GetHeadingFrom());
        }

        /// <summary>
        ///  Returns the ground's altitude above sea level at this geo position.
        /// </summary>
        /// <returns></returns>
        public ScalarValue GetTerrainAltitude()
        {
            double alt = 0.0;
            PQS bodyPQS = Body.pqsController;
            if (bodyPQS != null) // The sun has no terrain.  Everything else has a PQScontroller.
            {
                // The PQS controller gives the theoretical ideal smooth surface curve terrain.
                // The actual ground that exists in-game that you land on, however, is the terrain
                // polygon mesh which is built dynamically from the PQS controller's altitude values,
                // and it only approximates the PQS controller.  The discrepancy between the two
                // can be as high as 20 meters on relatively mild rolling terrain and is probably worse
                // in mountainous terrain with steeper slopes.  It also varies with the user terrain detail
                // graphics setting.

                // Therefore the algorithm here is this:  Get the PQS ideal terrain altitude first.
                // Then try using RayCast to get the actual terrain altitude, which will only work
                // if the LAT/LONG is near the active vessel so the relevant terrain polygons are
                // loaded.  If the RayCast hit works, it overrides the PQS altitude.
                                
                // PQS controller ideal altitude value:
                // -------------------------------------

                // The vector the pqs GetSurfaceHeight method expects is a vector in the following
                // reference frame:
                //     Origin = body center.
                //     X axis = LATLNG(0,0), Y axis = LATLNG(90,0)(north pole), Z axis = LATLNG(0,-90).
                // Using that reference frame, you tell GetSurfaceHeight what the "up" vector is pointing through
                // the spot on the surface you're querying for.
                var bodyUpVector = new Vector3d(1,0,0);
                bodyUpVector = QuaternionD.AngleAxis(Latitude, Vector3d.forward/*around Z axis*/) * bodyUpVector;
                bodyUpVector = QuaternionD.AngleAxis(Longitude, Vector3d.down/*around -Y axis*/) * bodyUpVector;

                alt = bodyPQS.GetSurfaceHeight( bodyUpVector ) - bodyPQS.radius ;

                // Terrain polygon raycasting:
                // ---------------------------
                const double HIGH_AGL = 1000.0;
                const double POINT_AGL = 800.0;
                // a point hopefully above the terrain:
                Vector3d worldRayCastStart = Body.GetWorldSurfacePosition( Latitude, Longitude, alt+HIGH_AGL );
                // a point a bit below it, to aim down to the terrain:
                Vector3d worldRayCastStop = Body.GetWorldSurfacePosition( Latitude, Longitude, alt+POINT_AGL );
                RaycastHit hit;
                if (Physics.Raycast(worldRayCastStart, (worldRayCastStop - worldRayCastStart), out hit, float.MaxValue, 1<<TERRAIN_MASK_BIT ))
                {
                    // Ensure hit is on the topside of planet, near the worldRayCastStart, not on the far side.
                    if (Mathf.Abs(hit.distance) < 3000)
                    {
                        // Okay a hit was found, use it instead of PQS alt:
                        alt = ((alt+HIGH_AGL) - hit.distance);
                    }
                }
            }
            return alt;
        }

        /// <summary>
        ///   The compass heading from the current position of the CPU vessel to the
        ///   LAT/LANG position on the SOI body's surface.
        /// </summary>
        /// <returns>compass heading in degrees</returns>
        private ScalarValue GetHeadingFrom()
        {
            var up = Shared.Vessel.upAxis;
            var north = VesselUtils.GetNorthVector(Shared.Vessel);

            var targetWorldCoords = Body.GetWorldSurfacePosition(Latitude, Longitude, GetTerrainAltitude() );

            var vector = Vector3d.Exclude(up, targetWorldCoords - Shared.Vessel.findWorldCenterOfMass()).normalized;
            var headingQ =
                Quaternion.Inverse(Quaternion.Euler(90, 0, 0)*Quaternion.Inverse(Quaternion.LookRotation(vector, up))*
                                   Quaternion.LookRotation(north, up));

            return headingQ.eulerAngles.y;
        }

        /// <summary>
        ///   The distance of the surface point of this LAT/LONG from where
        ///   the current CPU vessel is now.
        /// </summary>
        /// <returns>distance scalar</returns>
        private ScalarValue GetDistanceFrom()
        {
            return GetPosition().Magnitude();
        }
        
        /// <summary>
        ///   The surface point of this LAT/LONG from where
        ///   the current CPU vessel is now.
        /// </summary>
        /// <returns>position vector</returns>
        public Vector GetPosition()
        {
            return GetAltitudePosition(GetTerrainAltitude());
        }
        
        /// <summary>
        ///   The point above or below the surface of this LAT/LONG from where
        ///   the current CPU vessel is now.
        /// </summary>
        /// <param name="altitude">The (sea level) altitude to get a position for</param>>
        /// <returns>position vector</returns>
        public Vector GetAltitudePosition(ScalarValue altitude)
        {
            Vector3d latLongCoords = Body.GetWorldSurfacePosition(Latitude, Longitude, altitude);
            Vector3d hereCoords = Shared.Vessel.findWorldCenterOfMass();
            return new Vector(latLongCoords - hereCoords);
        }

        private void GeoCoordsInitializeSuffixes()
        {
            AddSuffix("LAT", new Suffix<ScalarValue>(()=> Latitude));
            AddSuffix("LNG", new Suffix<ScalarValue>(()=> Longitude));
            AddSuffix("BODY", new Suffix<BodyTarget>(()=> new BodyTarget(Body, Shared)));
            AddSuffix("TERRAINHEIGHT", new Suffix<ScalarValue>(GetTerrainAltitude));
            AddSuffix("DISTANCE", new Suffix<ScalarValue>(GetDistanceFrom));
            AddSuffix("HEADING", new Suffix<ScalarValue>(GetHeadingFrom));
            AddSuffix("BEARING", new Suffix<ScalarValue>(GetBearing));
            AddSuffix("POSITION", new Suffix<Vector>(GetPosition,
                                                     "Get the 3-D space position relative to the ship center, of this lat/long, " +
                                                     "at a point on the terrain surface"));
            AddSuffix("ALTITUDEPOSITION", new OneArgsSuffix<Vector,ScalarValue>(GetAltitudePosition,
                                                                           "Get the 3-D space position relative to the ship center, " +
                                                                           "of this lat/long, at this (sea level) altitude"));
        }

        public override string ToString()
        {
            return "LATLNG(" + Latitude + ", " + Longitude + ")";
        }

        public void SetSharedObjects(SharedObjects sharedObjects)
        {
            Shared = sharedObjects;
        }

        public override Dump Dump()
        {
            var dictionary = new DumpWithHeader
            {
                {DumpLat, lat},
                {DumpLng, lng},
                {DumpBody, new BodyTarget(Body, Shared)}
            };

            return dictionary;
        }

        public override void LoadDump(Dump dump)
        {
            Body = (dump[DumpBody] as BodyTarget).Body;
            lat = Convert.ToDouble(dump[DumpLat]);
            lng = Convert.ToDouble(dump[DumpLng]);
        }
    }
}
