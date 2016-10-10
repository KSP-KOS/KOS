using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Suffixed;
using kOS.Utilities;
using UnityEngine;

namespace kOS.AddOns.TrajectoriesAddon
{
    [kOSAddon("TR")]
    [Safe.Utilities.KOSNomenclature("TRAddon")]
    public class Addon : Suffixed.Addon
    {
        public Addon(SharedObjects shared) : base(shared)
        {
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("IMPACTPOS", new Suffix<GeoCoordinates>(ImpactPos, "Get impact position coordinates."));
            AddSuffix("HASIMPACT", new Suffix<BooleanValue>(HasImpact, "Check whether Trajectories has predicted an impact position for the current vessel."));
            AddSuffix(new string[] { "CORRECTEDVEC", "CORRECTEDVECTOR" }, new Suffix<Vector>(CorrectedVector, "Offset plus PlannedVect, somewhat corrected to glide ship towards target."));
            AddSuffix(new string[] { "PLANNEDVEC", "PLANNEDVECTOR" }, new Suffix<Vector>(PlannedVector, "Vector at which to point to follow predicted trajectory."));
            AddSuffix("SETTARGET", new OneArgsSuffix<GeoCoordinates>(SetTarget, "Set correctedVect target."));
        }

        private GeoCoordinates ImpactPos()
        {
            if (shared.Vessel != FlightGlobals.ActiveVessel)
            {
                throw new KOSException("You may only call addons:TR:impactPos from the active vessel. Always check addons:tr:hasImpact");
            }
            if (Available())
            {
                CelestialBody body = shared.Vessel.orbit.referenceBody;
                Vector3? impactVect = TRWrapper.ImpactVector();
                if (impactVect != null)
                {
                    var worldImpactPos = (Vector3d)impactVect + body.position;
                    var lat = body.GetLatitude(worldImpactPos);
                    var lng = Utils.DegreeFix(body.GetLongitude(worldImpactPos), -180);
                    return new GeoCoordinates(shared, lat, lng);
                }
                else
                {
                    throw new KOSException("Impact position is not available. Remember to check addons:tr:hasImpact");
                }
            }
            throw new KOSUnavailableAddonException("IMPACTPOS", "Trajectories");
        }

        private BooleanValue HasImpact()
        {
            if (Available())
            {
                return shared.Vessel == FlightGlobals.ActiveVessel && TRWrapper.ImpactVector().HasValue;
            }
            throw new KOSUnavailableAddonException("HASIMPACT", "Trajectories");
        }

        private Vector CorrectedVector()
        {
            if (shared.Vessel != FlightGlobals.ActiveVessel)
            {
                throw new KOSException("You may only call addons:TR:correctedVect from the active vessel. Always check addons:tr:hasImpact");
            }
            if (Available())
            {
                Vector3 vect = TRWrapper.CorrectedDirection();
                return new Vector(vect.x, vect.y, vect.z);
            }
            throw new KOSUnavailableAddonException("CORRECTEDDIRECTION", "Trajectories");
        }

        private Vector PlannedVector()
        {
            if (shared.Vessel != FlightGlobals.ActiveVessel)
            {
                throw new KOSException("You may only call addons:TR:plannedVect from the active vessel. Always check addons:tr:hasImpact");
            }
            if (Available())
            {
                Vector3 vect = TRWrapper.PlannedDirection();
                return new Vector(vect.x, vect.y, vect.z);
            }
            throw new KOSUnavailableAddonException("PLANNEDDIRECTION", "Trajectories");
        }

        private void SetTarget(GeoCoordinates target)
        {
            if (shared.Vessel != FlightGlobals.ActiveVessel)
            {
                throw new KOSException("You may only call addons:TR:setTarget from the active vessel. Always check addons:tr:hasImpact");
            }
            if (Available())
            {
                TRWrapper.SetTarget(target.Latitude, target.Longitude, target.GetTerrainAltitude());
            }
            else
            {
                throw new KOSUnavailableAddonException("SETTARGET", "Trajectories");
            }
        }

        public override BooleanValue Available()
        {
            return TRWrapper.Wrapped();
        }
    }
}