using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Suffixed;
using UnityEngine;

namespace kOS.AddOns.TrajectoriesAddon
{
    [kOS.Safe.Utilities.KOSNomenclature("TRAddon")]
    public class Addon : Suffixed.Addon
    {
        public Addon(SharedObjects shared) : base("TR", shared)
        {
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("IMPACTPOS", new Suffix<GeoCoordinates>(impactPos, "Get impact position coordinates."));
            AddSuffix("HASIMPACT", new Suffix<BooleanValue>(hasImpact, "Check whether Trajectories has predicted an impact position for the current vessel."));
            AddSuffix("CORRECTEDVECT", new Suffix<Vector>(correctedDirection, "PlannedVect somewhat corrected to glide ship towards target."));
            AddSuffix("PLANNEDVECT", new Suffix<Vector>(plannedDirection, "Direction to point to follow predicted trajectory."));
            AddSuffix("SETTARGET", new OneArgsSuffix<GeoCoordinates>(setTarget, "Set correctedVect target."));
        }

        private kOS.Suffixed.GeoCoordinates impactPos()
        {
            if (shared.Vessel != FlightGlobals.ActiveVessel)
            {
                throw new KOSException("You may only call addons:TR:impactPos from the active vessel. Always check addons:tr:hasImpact");
            }
            if (Available() == true)
            {
                CelestialBody body = shared.Vessel.orbit.referenceBody;
                Vector3? impactVect = TRWrapper.impactVector();
                if (impactVect != null)
                {
                    var worldImpactPos = (Vector3d)impactVect + body.position;
                    var lat = body.GetLatitude(worldImpactPos);
                    var lng = body.GetLongitude(worldImpactPos);
                    while (lng < -180)
                        lng += 360;
                    while (lng > 180)
                        lng -= 360;
                    return new kOS.Suffixed.GeoCoordinates(shared, lat, lng);
                }
                else {
                    throw new KOSException("Impact position is not available. Remember to check addons:tr:hasImpact");
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
        private Vector correctedDirection()
        {
            if (shared.Vessel != FlightGlobals.ActiveVessel)
            {
                throw new KOSException("You may only call addons:TR:correctedVect from the active vessel. Always check addons:tr:hasImpact");
            }
            if (Available() == true)
            {
                Vector3 vect = TRWrapper.correctedDirection();
                return new Vector(vect.x, vect.y, vect.z);
            }
            else
            {
                throw new KOSUnavailableAddonException("correctedDirection", "Trajectories");
            }
        }
        private Vector plannedDirection()
        {
            if (shared.Vessel != FlightGlobals.ActiveVessel)
            {
                throw new KOSException("You may only call addons:TR:plannedVect from the active vessel. Always check addons:tr:hasImpact");
            }
            if (Available() == true)
            {
                Vector3 vect = TRWrapper.plannedDirection();
                return new Vector(vect.x, vect.y, vect.z);
            }
            else
            {
                throw new KOSUnavailableAddonException("plannedDirection", "Trajectories");
            }
        }
        private void setTarget(GeoCoordinates target)
        {
            if (shared.Vessel != FlightGlobals.ActiveVessel)
            {
                throw new KOSException("You may only call addons:TR:setTarget from the active vessel. Always check addons:tr:hasImpact");
            }
            if (Available() == true)
            {
                TRWrapper.setTarget(target.Latitude, target.Longitude, target.GetTerrainAltitude());
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