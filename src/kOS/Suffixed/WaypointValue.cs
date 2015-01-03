using System;
using kOS.Utilities;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Encapsulation;
using FinePrint; // This is part of KSP's own DLL now.  The Waypoint info is in here.

namespace kOS.Suffixed
{
    public class WaypointValue : Structure
    {
        protected Waypoint wayPoint;
        protected CelestialBody cachedBody;
        protected SharedObjects shared;
        
        public WaypointValue(Waypoint wayPoint, SharedObjects shared)
        {
            this.wayPoint = wayPoint;
            this.shared = shared;
            AddSuffix("DUMP", new NoArgsSuffix<string>(ToVerboseString)); // for debugging
            AddSuffix("NAME", new NoArgsSuffix<string>(() => wayPoint.name, "Name of waypoint as it appears on the map and contract"));
            AddSuffix("BODY", new NoArgsSuffix<BodyTarget>(() => new BodyTarget(GetBody(),shared), "Celestial body the waypoint is attached to"));
            AddSuffix("GEOPOSITION", new NoArgsSuffix<GeoCoordinates>(BuildGeoCoordinates, "the LATLNG of this waypoint"));
            AddSuffix("ALTITUDE", new NoArgsSuffix<double>(BuildSeaLevelAltitude,
                                                           "Altitude of waypoint above sea level.  Warning, this a point somewhere in the " +
                                                           "midst of the contract altitude range, not the edge of the altitude range."));
            AddSuffix("AGL", new NoArgsSuffix<double>(() => wayPoint.altitude,
                                                      "Altitude of waypoint above ground.  Warning, this a point somewhere" +
                                                      "in the midst of the contract altitude range, not the edge of the altitude range."));
            AddSuffix("NEARSURFACE", new NoArgsSuffix<bool>(() => wayPoint.isOnSurface, "True if waypoint is a point near or on the body rather than high in orbit."));
            AddSuffix("GROUNDED", new NoArgsSuffix<bool>(() => wayPoint.landLocked, "True if waypoint is actually glued to the ground."));
        }
        
        public CelestialBody GetBody()
        {
            if (cachedBody == null)
                cachedBody = VesselUtils.GetBodyByName(wayPoint.celestialName);
            return cachedBody;
        }
        
        public GeoCoordinates BuildGeoCoordinates()
        {
            return new GeoCoordinates(GetBody(), shared, wayPoint.latitude, wayPoint.longitude);
        }
        
        public double BuildSeaLevelAltitude()
        {
            GeoCoordinates gCoord = BuildGeoCoordinates();
            return gCoord.GetTerrainAltitude() + wayPoint.altitude;
        }
        
        public override string ToString()
        {
            return String.Format("Waypoint \"{0}\"", wayPoint.name);
        }
        
        public string ToVerboseString()
        {
            // Remember to change this if you alter the suffix names:
            return String.Format("A Waypoint consisting of\n" +
                                 "  name= {0}\n" +
                                 "  body= {1}\n" +
                                 "  latitude= {2}\n" +
                                 "  longitude= {3}\n" +
                                 "  altitutde= {4}\n" +
                                 "  height= {5}\n" +
                                 "  isOnSurface= {6}\n" +
                                 "  worldPosition= {7}\n",
                                 wayPoint.name,
                                 wayPoint.celestialName,
                                 wayPoint.latitude,
                                 wayPoint.longitude,
                                 wayPoint.altitude, // A location inside the contract range's altitude range - and NOT the edge of it.
                                 wayPoint.height,
                                 wayPoint.isOnSurface,
                                 wayPoint.worldPosition);
        }
            
    }
}