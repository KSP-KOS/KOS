using UnityEngine;
using kOS.Utilities;

namespace kOS.Suffixed
{
    public class GeoCoordinates : SpecialValue
    {
        public double Lat { get; private set; }
        public double Lng { get; private set; }
        public SharedObjects Shared { get; set; } // for finding the current CPU's vessel, as per issue #107
        
        /// <summary>
        ///   Build a GeoCoordinates from the current lat/long of the orbitable
        ///   object passed in.  The object being checked for should be in the same
        ///   SOI as the vessel running the CPU or the results are meaningless.
        /// </summary>
        /// <param name="orb">object to take current coords of</param>
        /// <param name="sharedObj">to know the current CPU's running vessel</param>
        public GeoCoordinates(Orbitable orb, SharedObjects sharedObj)
        {
            Shared = sharedObj;
            Vector p = orb.GetPosition();
            Lat = orb.PositionToLatitude(p);
            Lng = orb.PositionToLongitude(p);
        }

        /// <summary>
        ///   Build a GeoCoordinates from any arbitrary lat/long pair of floats.
        /// </summary>
        /// <param name="sharedObj">to know the current CPU's running vessel</param>
        /// <param name="lat">latitude</param>
        /// <param name="lng">longitude</param>
        public GeoCoordinates(SharedObjects sharedObj, float lat, float lng)
        {
            Lat = lat;
            Lng = lng;
            Shared = sharedObj;
        }

        /// <summary>
        ///   Build a GeoCoordinates from any arbitrary lat/long pair of doubles.
        /// </summary>
        /// <param name="sharedObj">to know the current CPU's running vessel</param>
        /// <param name="lat">latitude</param>
        /// <param name="lng">longitude</param>
        public GeoCoordinates(SharedObjects sharedObj, double lat, double lng)
        {
            Lat = lat;
            Lng = lng;
            Shared = sharedObj;
        }

        /// <summary>
        ///   The bearing from the current CPU vessel to the surface spot with the
        ///   given lat/long coords, relative to the current CPU vessel's heading.
        /// </summary>
        /// <returns> bearing </returns>
        public double GetBearing()
        {
            return VesselUtils.AngleDelta(VesselUtils.GetHeading(Shared.Vessel), (float) GetHeadingFrom());
        }

        /// <summary>
        ///   The compass heading from the current position of the CPU vessel to the
        ///   LAT/LANG position on the SOI body's surface.
        /// </summary>
        /// <returns>compass heading in degrees</returns>
        private double GetHeadingFrom()
        {
            var up = Shared.Vessel.upAxis;
            var north = VesselUtils.GetNorthVector(Shared.Vessel);

            CelestialBody parent = Shared.Vessel.mainBody;
            if (parent==null) // Can only happen if current object is Sun, which is probably impossible
                return 0.0;

            var targetWorldCoords = parent.GetWorldSurfacePosition(Lat, Lng, Shared.Vessel.altitude);

            var vector = Vector3d.Exclude(up, targetWorldCoords - Shared.Vessel.GetWorldPos3D()).normalized;
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
        private double DistanceFrom()
        {
            CelestialBody parent = Shared.Vessel.mainBody;
            if (parent==null) // Can only happen if current object is Sun, which is probably impossible
                return 0.0;
            Vector3d latLongCoords = parent.GetWorldSurfacePosition( Lat, Lng, 0.0 );
            Vector3d hereCoords = Shared.Vessel.GetWorldPos3D();
            return Vector3d.Distance( latLongCoords, hereCoords );
        }

        public override object GetSuffix(string suffixName)
        {
            switch (suffixName)
            {
                case "LAT":
                    return Lat;
                case "LNG":
                    return Lng;
                case "DISTANCE":
                    return DistanceFrom();
                case "HEADING":
                    return GetHeadingFrom();
                case "BEARING":
                    return GetBearing();
            }

            return base.GetSuffix(suffixName);
        }

        public override string ToString()
        {
            return "LATLNG(" + Lat + ", " + Lng + ")";
        }
    }
}
