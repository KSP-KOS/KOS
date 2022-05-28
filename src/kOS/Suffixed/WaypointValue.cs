using System;
using System.Collections.Generic;
using kOS.Utilities;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;
using FinePrint; // This is part of KSP's own DLL now.  The Waypoint info is in here.

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("Waypoint")]
    public class WaypointValue : Structure
    {
        protected Waypoint WrappedWaypoint { get; set; }
        protected CelestialBody CachedBody { get; set; }
        protected SharedObjects Shared { get; set; }
        private static Dictionary<string,int> greekMap;
        
        private WaypointValue(Waypoint wayPoint, SharedObjects shared)
        {
            WrappedWaypoint = wayPoint;
            Shared = shared;
            InitializeSuffixes();
        }

        public static WaypointValue CreateWaypointValueWithCheck(Waypoint wayPoint, SharedObjects shared, bool failOkay)
        {
            string bodyName = wayPoint.celestialName;
            CelestialBody bod = VesselUtils.GetBodyByName(bodyName);
            if (bod == null)
            {
                if (failOkay)
                    return null;
                else
                    throw new KOSInvalidArgumentException("WAYPOINT constructor", bodyName, "Body not found in this solar system");
            }
            WaypointValue wp = new WaypointValue(wayPoint, shared);
            wp.CachedBody = bod;
            return wp;
        }

        private void InitializeSuffixes()
        {
            AddSuffix("DUMP", new NoArgsSuffix<StringValue>(ToVerboseString)); // for debugging
            AddSuffix("NAME", new NoArgsSuffix<StringValue>(CookedName, "Name of waypoint as it appears on the map and contract"));
            AddSuffix("BODY", new NoArgsSuffix<BodyTarget>(() => BodyTarget.CreateOrGetExisting(GetBody(), Shared), "Celestial body the waypoint is attached to"));
            AddSuffix("GEOPOSITION", new NoArgsSuffix<GeoCoordinates>(BuildGeoCoordinates, "the LATLNG of this waypoint"));
            AddSuffix("POSITION", new NoArgsSuffix<Vector>(() => GetPosition() - new Vector(Shared.Vessel.CoMD)));
            AddSuffix("ALTITUDE", new NoArgsSuffix<ScalarValue>(BuildSeaLevelAltitude, "Altitude of waypoint above sea level.  Warning, this a point somewhere in the " + "midst of the contract altitude range, not the edge of the altitude range."));
            AddSuffix("AGL", new NoArgsSuffix<ScalarValue>(() => WrappedWaypoint.altitude, "Altitude of waypoint above ground.  Warning, this a point somewhere" + "in the midst of the contract altitude range, not the edge of the altitude range."));
            AddSuffix("NEARSURFACE", new NoArgsSuffix<BooleanValue>(() => WrappedWaypoint.isOnSurface, "True if waypoint is a point near or on the body rather than high in orbit."));
            AddSuffix("GROUNDED", new NoArgsSuffix<BooleanValue>(() => WrappedWaypoint.landLocked, "True if waypoint is actually glued to the ground.")); 
            AddSuffix("INDEX", new NoArgsSuffix<ScalarValue>(() => WrappedWaypoint.index, "Number of this waypoint if this is a grouped waypoint (i.e. alpha/beta/gamma..")); 
            AddSuffix("CLUSTERED", new NoArgsSuffix<BooleanValue>(() => WrappedWaypoint.isClustered, "True if this is a member of a cluster of waypoints (i.e. alpha/beta/gamma.."));
            AddSuffix("ISSELECTED", new NoArgsSuffix<BooleanValue>(() => Shared.Vessel.navigationWaypoint == WrappedWaypoint, "True if navigation has been activated on this waypoint."));
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
        
        public ScalarValue BuildSeaLevelAltitude()
        {
            GeoCoordinates gCoord = BuildGeoCoordinates();
            return gCoord.GetTerrainAltitude() + WrappedWaypoint.altitude;
        }
        
        public override string ToString()
        {
            return string.Format("Waypoint \"{0}\"", CookedName() );
        }
        
        public StringValue CookedName()
        {
            return string.Format("{0}{1}",
                                 WrappedWaypoint.name,
                                 WrappedWaypoint.isClustered ?
                                     " " + FinePrint.Utilities.StringUtilities.IntegerToGreek(WrappedWaypoint.index) :
                                     ""
                                );
        }
        
        public StringValue ToVerboseString()
        {
            // Remember to change this if you alter the suffix names:
            return string.Format("A Waypoint consisting of\n" +
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
        /// <param name="baseName">the name after the last term has been stripped off, if there are
        /// space separated terms. Note that if the return value of this method is false, this
        /// shouldn't be used and you should stick with the original full name.</param>
        /// <returns>true if there was a greek letter suffix</returns>
        public static bool GreekToInteger(string greekLetterName, out int index, out string baseName )
        {
            // greekMap is static, and we only need to populate it once in
            // the lifetime of the KSP process.  We'll do so the first time
            // this method (the only one that uses it) gets called:
            if (greekMap == null)
                InitializeGreekMap();

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