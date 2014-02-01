using UnityEngine;
using kOS.Utilities;

namespace kOS.Suffixed
{
    public class GeoCoordinates : SpecialValue
    {
        public CelestialBody Body;
        public double Lat;
        public double Lng;
        public Vessel Vessel;

        public GeoCoordinates(Vessel vessel)
        {
            Lat = VesselUtils.GetVesselLattitude(vessel);
            Lng = VesselUtils.GetVesselLongitude(vessel);
            Vessel = vessel;

            Body = vessel.mainBody;
        }

        public GeoCoordinates(Vessel vessel, float lat, float lng)
        {
            Lat = lat;
            Lng = lng;
            Vessel = vessel;

            Body = vessel.mainBody;
        }

        public GeoCoordinates(Vessel vessel, double lat, double lng)
        {
            Lat = lat;
            Lng = lng;
            Vessel = vessel;

            Body = vessel.mainBody;
        }

        public float GetBearing(Vessel vessel)
        {
            return VesselUtils.AngleDelta(VesselUtils.GetHeading(vessel), GetHeadingFromVessel(vessel));
        }

        public float GetHeadingFromVessel(Vessel vessel)
        {
            var up = vessel.upAxis;
            var north = VesselUtils.GetNorthVector(vessel);

            var targetWorldCoords = vessel.mainBody.GetWorldSurfacePosition(Lat, Lng, vessel.altitude);

            var vector = Vector3d.Exclude(vessel.upAxis, targetWorldCoords - vessel.GetWorldPos3D()).normalized;
            var headingQ =
                Quaternion.Inverse(Quaternion.Euler(90, 0, 0)*Quaternion.Inverse(Quaternion.LookRotation(vector, up))*
                                   Quaternion.LookRotation(north, up));

            return headingQ.eulerAngles.y;
        }

        public double DistanceFrom(Vessel vessel)
        {
            return Vector3d.Distance(vessel.GetWorldPos3D(), Body.GetWorldSurfacePosition(Lat, Lng, vessel.altitude));
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
                    return DistanceFrom(Vessel);
                case "HEADING":
                    return (double) GetHeadingFromVessel(Vessel);
                case "BEARING":
                    return (double) GetBearing(Vessel);
            }

            return base.GetSuffix(suffixName);
        }

        public override string ToString()
        {
            return "LATLNG(" + Lat + ", " + Lng + ")";
        }
    }
}