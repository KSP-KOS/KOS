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
        public Addon(SharedObjects shared) : base(shared) => InitializeSuffixes();

        private void InitializeSuffixes()
        {
            AddSuffix("GETVERSION", new Suffix<StringValue>(GetVersion, "Get version string (Major.Minor.Patch)."));
            AddSuffix("ISVERTWO", new Suffix<BooleanValue>(IsVerTwo, "Check whether Trajectories is v2.0.0 and above."));
            AddSuffix("ISVERTWOTWO", new Suffix<BooleanValue>(IsVerTwoTwo, "Check whether Trajectories is v2.2.0 and above."));
            AddSuffix("ISVERTWOFOUR", new Suffix<BooleanValue>(IsVerTwoFour, "Check whether Trajectories is v2.4.0 and above."));
            AddSuffix("IMPACTPOS", new Suffix<GeoCoordinates>(ImpactPos, "Get impact position coordinates."));
            AddSuffix("HASIMPACT", new Suffix<BooleanValue>(HasImpact, "Check whether Trajectories has predicted an impact position for the current vessel."));
            AddSuffix(new string[] { "CORRECTEDVEC", "CORRECTEDVECTOR" }, new Suffix<Vector>(CorrectedVector, "Offset plus PlannedVect, somewhat corrected to glide ship towards target."));
            AddSuffix(new string[] { "PLANNEDVEC", "PLANNEDVECTOR" }, new Suffix<Vector>(PlannedVector, "Vector at which to point to follow predicted trajectory."));
            AddSuffix("SETTARGET", new OneArgsSuffix<GeoCoordinates>(SetTarget, "Set CorrectedVect target."));
            AddSuffix("HASTARGET", new Suffix<BooleanValue>(HasTarget, "Check whether Trajectories has a target position set."));
            AddSuffix("TIMETILLIMPACT", new Suffix<ScalarValue>(TimeTillImpact, "Remaining time until Impact in seconds."));
            AddSuffix("RETROGRADE", new SetSuffix<BooleanValue>(IsRetrograde, SetRetrograde, "Check the descent profile is Retrograde or Set the descent profile to Retrograde."));
            AddSuffix("PROGRADE", new SetSuffix<BooleanValue>(IsPrograde, SetPrograde, "Check the descent profile is Prograde or Set the descent profile to Prograde."));
        }

        // Version checking suffixes.
        private StringValue GetVersion()
        {
            if (Available())
                return TRWrapper.GetVersion;
            throw new KOSUnavailableAddonException("GETVERSION", "Trajectories");
        }

        private BooleanValue IsVerTwo()
        {
            if (Available())
                return TRWrapper.IsVerTwo;
            throw new KOSUnavailableAddonException("ISVERTWO", "Trajectories");
        }

        private BooleanValue IsVerTwoTwo()
        {
            if (Available())
                return TRWrapper.IsVerTwoTwo;
            throw new KOSUnavailableAddonException("ISVERTWOTWO", "Trajectories");
        }

        private BooleanValue IsVerTwoFour()
        {
            if (Available())
                return TRWrapper.IsVerTwoFour;
            throw new KOSUnavailableAddonException("ISVERTWOFOUR", "Trajectories");
        }

        // Older suffixes.
        private GeoCoordinates ImpactPos()
        {
            if (shared.Vessel != FlightGlobals.ActiveVessel)
                throw new KOSException("You may only call addons:TR:ImpactPos from the active vessel. Always check addons:tr:HasImpact");
            if (Available())
            {
                CelestialBody body = shared.Vessel.orbit.referenceBody;
                Vector3? impactVect = TRWrapper.ImpactVector();
                if (impactVect != null)
                {
                    Vector3d worldImpactPos = (Vector3d)impactVect + body.position;
                    double lat = body.GetLatitude(worldImpactPos);
                    double lng = Utils.DegreeFix(body.GetLongitude(worldImpactPos), -180);
                    return new GeoCoordinates(shared, lat, lng);
                }
                throw new KOSException("Impact position is not available. Remember to check addons:tr:HasImpact");
            }
            throw new KOSUnavailableAddonException("IMPACTPOS", "Trajectories");
        }

        private BooleanValue HasImpact()
        {
            if (Available())
                return shared.Vessel == FlightGlobals.ActiveVessel && TRWrapper.ImpactVector().HasValue;
            throw new KOSUnavailableAddonException("HASIMPACT", "Trajectories");
        }

        private Vector CorrectedVector()
        {
            if (shared.Vessel != FlightGlobals.ActiveVessel)
                throw new KOSException("You may only call addons:TR:CorrectedVect from the active vessel and must also have a trajectories target set." +
                    " Always check addons:tr:HasImpact and addons:tr:HasTarget");
            if (Available())
            {
                Vector3? vect = TRWrapper.CorrectedDirection();
                if (vect != null)
                {
                    Vector3 vector = (Vector3)vect;
                    return new Vector(vector.x, vector.y, vector.z);
                }
                throw new KOSException("Corrected Vector is not available. Remember to check addons:tr:HasImpact and addons:tr:HasTarget");
            }
            throw new KOSUnavailableAddonException("CORRECTEDDIRECTION", "Trajectories");
        }

        private Vector PlannedVector()
        {
            if (shared.Vessel != FlightGlobals.ActiveVessel)
                throw new KOSException("You may only call addons:TR:plannedVect from the active vessel and must also have a trajectories target set." +
                    " Always check addons:tr:HasImpact and addons:tr:HasTarget");
            if (Available())
            {
                Vector3? vect = TRWrapper.PlannedDirection();
                if (vect != null)
                {
                    Vector3 vector = (Vector3)vect;
                    return new Vector(vector.x, vector.y, vector.z);
                }
                throw new KOSException("Planned Vector is not available. Remember to check addons:tr:HasImpact and addons:tr:HasTarget");
            }
            throw new KOSUnavailableAddonException("PLANNEDDIRECTION", "Trajectories");
        }

        private void SetTarget(GeoCoordinates target)
        {
            if (shared.Vessel != FlightGlobals.ActiveVessel)
                throw new KOSException("You may only call addons:TR:SetTarget from the active vessel.");
            if (Available())
            {
                TRWrapper.SetTarget(target.Latitude, target.Longitude, target.GetTerrainAltitude());
                return;
            }
            throw new KOSUnavailableAddonException("SETTARGET", "Trajectories");
        }

        // v2.0.0 HasTarget suffix.
        private BooleanValue HasTarget()
        {
            if (shared.Vessel != FlightGlobals.ActiveVessel)
                throw new KOSException("You may only call addons:TR:HasTarget from the active vessel.");
            if (Available())
            {
                bool? result = TRWrapper.HasTarget();
                if (result != null)
                    return result;
                throw new KOSException("HasTarget is not available. It was added in Trajectories v2.0.0. and your version might be older." +
                    " Check addons:tr:IsVerTwo or addons:tr:GetVersion");
            }
            throw new KOSUnavailableAddonException("HASTARGET", "Trajectories");
        }

        // v2.2.0 and above suffixes.
        private ScalarValue TimeTillImpact()
        {
            if (shared.Vessel != FlightGlobals.ActiveVessel)
                throw new KOSException("You may only call addons:TR:TimeTillImpact from the active vessel.");
            if (Available())
            {
                double? result = TRWrapper.GetTimeTillImpact();
                if (result != null)
                    return result;
                throw new KOSException("TimeTillImpact is not available. Remember to check addons:tr:HasImpact." +
                    " Also TimeTillImpact was added in Trajectories v2.2.0. and your version might be older." +
                    " Check addons:tr:IsVerTwoTwo or addons:tr:GetVersion");
            }
            throw new KOSUnavailableAddonException("TIMETILLIMPACT", "Trajectories");
        }

        private BooleanValue IsPrograde()
        {
            if (shared.Vessel != FlightGlobals.ActiveVessel)
                throw new KOSException("You may only call addons:TR:Prograde from the active vessel.");
            if (Available())
            {
                bool? result = TRWrapper.ProgradeEntry;
                if (result != null)
                    return result;
                throw new KOSException("Prograde is not available. It was added in Trajectories v2.2.0. and your version might be older." +
                    " Check addons:tr:IsVerTwoTwo or addons:tr:GetVersion");
            }
            throw new KOSUnavailableAddonException("PROGRADE", "Trajectories");
        }

        private void SetPrograde(BooleanValue value)
        {
            if (shared.Vessel != FlightGlobals.ActiveVessel)
                throw new KOSException("You may only call addons:TR:Prograde from the active vessel.");
            if (Available())
            {
                if (value)
                    TRWrapper.ProgradeEntry = true;
                else
                    TRWrapper.RetrogradeEntry = true;
                return;
            }
            throw new KOSUnavailableAddonException("PROGRADE", "Trajectories");
        }

        private BooleanValue IsRetrograde()
        {
            if (shared.Vessel != FlightGlobals.ActiveVessel)
                throw new KOSException("You may only call addons:TR:Retrograde from the active vessel.");
            if (Available())
            {
                bool? result = TRWrapper.RetrogradeEntry;
                if (result != null)
                    return result;
                throw new KOSException("Retrograde is not available. It was added in Trajectories v2.2.0. and your version might be older." +
                    " Check addons:tr:IsVerTwoTwo or addons:tr:GetVersion");
            }
            throw new KOSUnavailableAddonException("RETROGRADE", "Trajectories");
        }

        private void SetRetrograde(BooleanValue value)
        {
            if (shared.Vessel != FlightGlobals.ActiveVessel)
                throw new KOSException("You may only call addons:TR:Retrograde from the active vessel.");
            if (Available())
            {
                if (value)
                    TRWrapper.RetrogradeEntry = true;
                else
                    TRWrapper.ProgradeEntry = true;
                return;
            }
            throw new KOSUnavailableAddonException("RETROGRADE", "Trajectories");
        }


        public override BooleanValue Available() => TRWrapper.Wrapped();
    }
}