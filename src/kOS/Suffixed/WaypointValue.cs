﻿using System;
using System.Collections.Generic;
using kOS.Utilities;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Encapsulation;
using FinePrint; // This is part of KSP's own DLL now.  The Waypoint info is in here.

namespace kOS.Suffixed
{
    public class WaypointValue : Structure
    {
        protected Waypoint WrappedWaypoint { get; set; }
        protected CelestialBody CachedBody { get; set; }
        protected SharedObjects Shared { get; set; }
        private static Dictionary<string,int> greekMap;
        
        public WaypointValue(Waypoint wayPoint, SharedObjects shared)
        {
            WrappedWaypoint = wayPoint;
            Shared = shared;
            InitializeSuffixes();

            // greekMap is static, so whichever waypoint instance's constructor happens to
            // get called first will make it, and from then on other waypoints don't need to
            // keep re-initializing it:
            if (greekMap == null)
                InitializeGreekMap();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("DUMP", new NoArgsSuffix<string>(ToVerboseString)); // for debugging
            AddSuffix("NAME", new NoArgsSuffix<string>(CookedName, "Name of waypoint as it appears on the map and contract"));
            AddSuffix("BODY", new NoArgsSuffix<BodyTarget>(() => new BodyTarget(GetBody(), Shared), "Celestial body the waypoint is attached to"));
            AddSuffix("GEOPOSITION", new NoArgsSuffix<GeoCoordinates>(BuildGeoCoordinates, "the LATLNG of this waypoint"));
            AddSuffix("POSITION", new NoArgsSuffix<Vector>(() => GetPosition() - new Vector(Shared.Vessel.findWorldCenterOfMass())));
            AddSuffix("ALTITUDE", new NoArgsSuffix<double>(BuildSeaLevelAltitude, "Altitude of waypoint above sea level.  Warning, this a point somewhere in the " + "midst of the contract altitude range, not the edge of the altitude range."));
            AddSuffix("AGL", new NoArgsSuffix<double>(() => WrappedWaypoint.altitude, "Altitude of waypoint above ground.  Warning, this a point somewhere" + "in the midst of the contract altitude range, not the edge of the altitude range."));
            AddSuffix("NEARSURFACE", new NoArgsSuffix<bool>(() => WrappedWaypoint.isOnSurface, "True if waypoint is a point near or on the body rather than high in orbit."));
            AddSuffix("GROUNDED", new NoArgsSuffix<bool>(() => WrappedWaypoint.landLocked, "True if waypoint is actually glued to the ground.")); 
            AddSuffix("INDEX", new NoArgsSuffix<int>(() => WrappedWaypoint.index, "Number of this waypoint if this is a grouped waypoint (i.e. alpha/beta/gamma..")); 
            AddSuffix("CLUSTERED", new NoArgsSuffix<bool>(() => WrappedWaypoint.isClustered, "True if this is a member of a cluster of waypoints (i.e. alpha/beta/gamma.."));
        }

        private static void InitializeGreekMap()
        {
            greekMap = new Dictionary<string, int>();
            for (int i = 0 ; i < 20 ; ++i)
                greekMap.Add(FinePrint.Utilities.StringUtilities.IntegerToGreek(i).ToLower(), i);
        }
        
        public CelestialBody GetBody()
        {
            return CachedBody ?? (CachedBody = VesselUtils.GetBodyByName(WrappedWaypoint.celestialName));
        }

        public GeoCoordinates BuildGeoCoordinates()
        {
            return new GeoCoordinates(GetBody(), Shared, WrappedWaypoint.latitude, WrappedWaypoint.longitude);
        }
        
        public Vector GetPosition()
        {
            return new Vector(GetBody().GetWorldSurfacePosition(WrappedWaypoint.latitude, WrappedWaypoint.longitude, BuildSeaLevelAltitude()));
        }
        
        public double BuildSeaLevelAltitude()
        {
            GeoCoordinates gCoord = BuildGeoCoordinates();
            return gCoord.GetTerrainAltitude() + WrappedWaypoint.altitude;
        }
        
        public override string ToString()
        {
            return String.Format("Waypoint \"{0}\"", CookedName() );
        }
        
        public string CookedName()
        {
            return String.Format("{0}{1}",
                                 WrappedWaypoint.name,
                                 (
                                     WrappedWaypoint.isClustered ?
                                     (" " + FinePrint.Utilities.StringUtilities.IntegerToGreek(WrappedWaypoint.index)) :
                                      ""
                                 )
                                );
        }
        
        public string ToVerboseString()
        {
            // Remember to change this if you alter the suffix names:
            return String.Format("A Waypoint consisting of\n" +
                                 "  name= {0}\n" +
                                 "  body= {1}\n" +
                                 "  geoposition= {2}\n" +
                                 "  agl= {3}\n" +
                                 "  altitude= {4}\n" +
                                 "  nearsurface= {5}\n" +
                                 "  position= {6}\n" +
                                 "  index= {7}\n" +
                                 "  clustered= {8}\n",
                                 CookedName(),
                                 WrappedWaypoint.celestialName,
                                 BuildGeoCoordinates(),
                                 WrappedWaypoint.altitude, // A location inside the contract range's altitude range - and NOT the edge of it.
                                 BuildSeaLevelAltitude(),
                                 WrappedWaypoint.isOnSurface,
                                 GetPosition(),
                                 WrappedWaypoint.index,
                                 WrappedWaypoint.isClustered);
        }
        
        /// <summary>
        /// Return an integer to go with the alphabet position of the
        /// string given in greek lettering.  For example, input "alpha" and 
        /// get out 0.  input "beta" and get out 1.  If no match is found,
        /// -1 is returned.
        /// <br/>
        /// This is used by waypoints so that you can figure out that a waypoint
        /// named like "Jebadiah's lament Gamma" is really the 3rd (index 2) member
        /// of the "Jebadiah's lament" cluster of waypoints.
        /// <br/>
        /// Because that is its purpose, it actually operates on the LASTMOST word
        /// in the string it is given.  given a string like "foo bar baz", it will 
        /// check if "baz" is a greek letter, not "foo".  Thus you can pass in exactly
        /// the name as it appears onscreen.
        /// </summary>
        /// <param name="greekLetterName">string name to check.  Case insensitively.</param>
        /// <param name="index">integer position in alphabet.  -1 if no match.</param>
        /// <param name="baseName">the name after the greek suffix has been stripped off, if there is one.</param>
        /// <returns>true if there was a greek letter suffix</returns>
        public static bool GreekToInteger(string greekLetterName, out int index, out string baseName )
        {
            // Get lastmost word (or whole string if there's no spaces):
            int lastSpace = greekLetterName.LastIndexOf(' ');
            string lastTerm;
            if (lastSpace >= 0 && lastSpace < greekLetterName.Length - 1)
            {
                // last space is real, and isn't the lastmost char but actually has
                // nonspaces that follow it:
                lastTerm = greekLetterName.Substring(lastSpace+1).ToLower(); // ToLower for the dictionary hashmap lookup
                baseName = greekLetterName.Substring(0,lastSpace);
            }
            else
            {
                lastTerm = greekLetterName.ToLower(); // ToLower for the dictionary hashmap lookup
                baseName = greekLetterName;
            }
            
            bool worked = greekMap.TryGetValue(lastTerm, out index);
            if (!worked)
                index = -1;
            return worked;
        }
            
    }
}