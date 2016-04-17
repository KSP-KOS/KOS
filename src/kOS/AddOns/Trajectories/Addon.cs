using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;

namespace kOS.AddOns.TrajectoriesAddon
{
    public class Addon : Suffixed.Addon
    {
        public Addon(SharedObjects shared) : base("TR", shared)
        {
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("IMPACTPOS", new Suffix<kOS.Suffixed.GeoCoordinates>(impactPos, "Get impact position coordinates."));
            AddSuffix("HASIMPACT", new Suffix<BooleanValue>(hasImpact, "Check whether impactPos is available."));
        }

        private kOS.Suffixed.GeoCoordinates impactPos()
        {
            if (shared.Vessel != FlightGlobals.ActiveVessel)
            {
                throw new KOSException("You may only call addons:TR:impactPos from the active vessel");
            }
            if (Available() == true)
            {
                var ship = FlightGlobals.ActiveVessel;
                var body = ship.orbit.referenceBody;
                
                if (TRWrapper.impactVector() != null)
                {
                    var worldImpactPos = (Vector3d)TRWrapper.impactVector() + body.position;
                    var lat = body.GetLatitude(worldImpactPos);
                    var lng = body.GetLongitude(worldImpactPos);
                    while (lng < -180)
                        lng += 360;
                    while (lng > 180)
                        lng -= 360;
                    return new kOS.Suffixed.GeoCoordinates(shared, lat, lng);
                }
                else {
                    throw new KOSException("Impact position is not available. Remember to check using addons:tr:hasImpact");
                }
            }
            else
            {
                throw new KOSUnavailableAddonException("impactPos", "Trajectories");
            }
        }
        private BooleanValue hasImpact()
        {
            if (Available() == true)
            {
                if (shared.Vessel != FlightGlobals.ActiveVessel || TRWrapper.impactVector() == null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                throw new KOSUnavailableAddonException("hasImpact", "Trajectories");
            }
        }
        public override BooleanValue Available()
        {
            return TRWrapper.Wrapped();
        }
    }
}