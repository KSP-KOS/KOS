using System;
using kOS.Utilities;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Encapsulation;
using FinePrint; // This is part of KSP's own DLL now.  The Waypoint info is in here.

namespace kOS.Suffixed
{
    public class WaypointValue : Structure
    {
        protected Waypoint WayPoint { get; set; }
        protected CelestialBody CachedBody { get; set; }
        protected SharedObjects Shared { get; set; }
        
        public WaypointValue(Waypoint wayPoint, SharedObjects shared)
        {
            WayPoint = wayPoint;
            Shared = shared;
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
            return CachedBody ?? (CachedBody = VesselUtils.GetBodyByName(WayPoint.celestialName));
        }

        public GeoCoordinates BuildGeoCoordinates()
        {
            return new GeoCoordinates(GetBody(), Shared, WayPoint.latitude, WayPoint.longitude);
        }
        
        public double BuildSeaLevelAltitude()
        {
            GeoCoordinates gCoord = BuildGeoCoordinates();
            return gCoord.GetTerrainAltitude() + WayPoint.altitude;
        }

        public override bool KOSEquals(object other)
        {
            WaypointValue that = other as WaypointValue;
            if (that == null) return false;
            return this.WayPoint.Equals(that.WayPoint);
        } 

        public override string ToString()
        {
            return String.Format("Waypoint \"{0}\"", WayPoint.name);
        }
        
        public string ToVerboseString()
        {
            // Remember to change this if you alter the suffix names:
            return String.Format("A Waypoint consisting of\n" +
                                 "  name= {0}\n" +
                                 "  body= {1}\n" +
                                 "  latitude= {2}\n" +
                                 "  longitude= {3}\n" +
                                 "  altitude= {4}\n" +
                                 "  height= {5}\n" +
                                 "  isOnSurface= {6}\n" +
                                 "  worldPosition= {7}\n",
                                 WayPoint.name,
                                 WayPoint.celestialName,
                                 WayPoint.latitude,
                                 WayPoint.longitude,
                                 WayPoint.altitude, // A location inside the contract range's altitude range - and NOT the edge of it.
                                 WayPoint.height,
                                 WayPoint.isOnSurface,
                                 WayPoint.worldPosition);
        }
            
    }
}