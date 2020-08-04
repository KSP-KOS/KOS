using System.Collections.Generic;
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
            AddSuffix("RETROGRADE", new SetSuffix<BooleanValue>(IsRetrograde, SetRetrograde, "Check all the descent profile nodes are Retrograde or Reset all the descent profile nodes to Retrograde."));
            AddSuffix("PROGRADE", new SetSuffix<BooleanValue>(IsPrograde, SetPrograde, "Check all the descent profile nodes are Prograde or Reset all the descent profile nodes to Prograde."));
            AddSuffix("GETTARGET", new Suffix<GeoCoordinates>(GetTarget, "Get the currently set target position coordinates."));
            AddSuffix("CLEARTARGET", new NoArgsVoidSuffix(ClearTarget, "Clear the current target."));
            AddSuffix("RESETDESCENTPROFILE", new OneArgsSuffix<ScalarValue>(ResetDescentProfile, "Reset the descent profile to the passed AoA value in radians."));
            AddSuffix("DESCENTPROFILEANGLES", new SetSuffix<ListValue<ScalarValue>>(GetProfileAngles, SetProfileAngles, "Descent profile angles in radians, also sets Retrograde if any values are greater than ±90°, List(entry, high altitude, low altitude, final approach)."));
            AddSuffix("DESCENTPROFILEMODES", new SetSuffix<ListValue<BooleanValue>>(GetProfileModes, SetProfileModes, "Descent profile modes, true = AoA, false = Horizon, List(entry, high altitude, low altitude, final approach)."));
            AddSuffix("DESCENTPROFILEGRADES", new SetSuffix<ListValue<BooleanValue>>(GetProfileGrades, SetProfileGrades, "Descent profile grades, true = Retrograde, false = Prograde, List(entry, high altitude, low altitude, final approach)."));
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
                TRWrapper.ProgradeEntry = value;
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
                TRWrapper.RetrogradeEntry = value;
                return;
            }
            throw new KOSUnavailableAddonException("RETROGRADE", "Trajectories");
        }

        // v2.4.0 and above suffixes.
        private GeoCoordinates GetTarget()
        {
            if (shared.Vessel != FlightGlobals.ActiveVessel)
                throw new KOSException("You may only call addons:TR:GetTarget from the active vessel and must also have a trajectories target set." +
                    " Always check addons:tr:HasTarget");
            if (Available())
            {
                Vector3d? result = TRWrapper.GetTarget();
                if (result != null)
                    return new GeoCoordinates(shared, result.Value.x, result.Value.y);

                throw new KOSException("GetTarget is not available or no target is set. Remember to check addons:tr:HasTarget." +
                    " Also GetTarget was added in Trajectories v2.4.0. and your version might be older." +
                    " Check addons:tr:IsVerTwoFour or addons:tr:GetVersion");
            }
            throw new KOSUnavailableAddonException("GETTARGET", "Trajectories");
        }

        private void ClearTarget()
        {
            if (shared.Vessel != FlightGlobals.ActiveVessel)
                throw new KOSException("You may only call addons:TR:ClearTarget from the active vessel.");
            if (Available())
            {
                TRWrapper.ClearTarget();
                return;
            }
            throw new KOSUnavailableAddonException("CLEARTARGET", "Trajectories");
        }

        private void ResetDescentProfile(ScalarValue aoa)
        {
            if (shared.Vessel != FlightGlobals.ActiveVessel)
                throw new KOSException("You may only call addons:TR:ResetDescentProfile from the active vessel.");
            if (Available())
            {
                TRWrapper.ResetDescentProfile(aoa);
                return;
            }
            throw new KOSUnavailableAddonException("RESETDESCENTPROFILE", "Trajectories");
        }

        private ListValue<ScalarValue> GetProfileAngles()
        {
            if (shared.Vessel != FlightGlobals.ActiveVessel)
                throw new KOSException("You may only call addons:TR:DescentProfileAngles from the active vessel.");
            if (Available())
            {
                List<double> result = TRWrapper.DescentProfileAngles;
                if (result != null && result.Count > 3)
                {
                    return new ListValue<ScalarValue>
                    {
                        result[0],    // atmospheric entry node
                        result[1],    // high altitude node
                        result[2],    // low altitude node
                        result[3]     // final approach node
                    };
                }
                throw new KOSException("DescentProfileAngles is not available. It was added in Trajectories v2.4.0. and your version might be older." +
                    " Check addons:tr:IsVerTwoFour or addons:tr:GetVersion");
            }
            throw new KOSUnavailableAddonException("DESCENTPROFILEANGLES", "Trajectories");
        }

        private void SetProfileAngles(ListValue<ScalarValue> aoa)
        {
            if (shared.Vessel != FlightGlobals.ActiveVessel)
                throw new KOSException("You may only call addons:TR:DescentProfileAngles from the active vessel.");
            if (Available())
            {
                if (aoa != null && aoa.Count > 3)
                {
                    TRWrapper.DescentProfileAngles = new List<double>
                    {
                        aoa[0],    // atmospheric entry node
                        aoa[1],    // high altitude node
                        aoa[2],    // low altitude node
                        aoa[3]     // final approach node
                    };
                }
                throw new KOSException("DescentProfileAngles was passed an invalid list, make sure to have at least 4 values in the list.");
            }
            throw new KOSUnavailableAddonException("DESCENTPROFILEANGLES", "Trajectories");
        }

        private ListValue<BooleanValue> GetProfileModes()
        {
            if (shared.Vessel != FlightGlobals.ActiveVessel)
                throw new KOSException("You may only call addons:TR:DescentProfileModes from the active vessel.");
            if (Available())
            {
                List<bool> result = TRWrapper.DescentProfileModes;
                if (result != null && result.Count > 3)
                {
                    return new ListValue<BooleanValue>
                    {
                        result[0],    // atmospheric entry node
                        result[1],    // high altitude node
                        result[2],    // low altitude node
                        result[3]     // final approach node
                    };
                }
                throw new KOSException("DescentProfileModes is not available. It was added in Trajectories v2.4.0. and your version might be older." +
                    " Check addons:tr:IsVerTwoFour or addons:tr:GetVersion");
            }
            throw new KOSUnavailableAddonException("DESCENTPROFILEMODES", "Trajectories");
        }

        private void SetProfileModes(ListValue<BooleanValue> modes)
        {
            if (shared.Vessel != FlightGlobals.ActiveVessel)
                throw new KOSException("You may only call addons:TR:DescentProfileModes from the active vessel.");
            if (Available())
            {
                if (modes != null && modes.Count > 3)
                {
                    TRWrapper.DescentProfileModes = new List<bool>
                    {
                        modes[0],    // atmospheric entry node
                        modes[1],    // high altitude node
                        modes[2],    // low altitude node
                        modes[3]     // final approach node
                    };
                }
                throw new KOSException("DescentProfileModes was passed an invalid list, make sure to have at least 4 values in the list.");
            }
            throw new KOSUnavailableAddonException("DESCENTPROFILEMODES", "Trajectories");
        }

        private ListValue<BooleanValue> GetProfileGrades()
        {
            if (shared.Vessel != FlightGlobals.ActiveVessel)
                throw new KOSException("You may only call addons:TR:DescentProfileGrades from the active vessel.");
            if (Available())
            {
                List<bool> result = TRWrapper.DescentProfileGrades;
                if (result != null && result.Count > 3)
                {
                    return new ListValue<BooleanValue>
                    {
                        result[0],    // atmospheric entry node
                        result[1],    // high altitude node
                        result[2],    // low altitude node
                        result[3]     // final approach node
                    };
                }
                throw new KOSException("DescentProfileGrades is not available. It was added in Trajectories v2.4.0. and your version might be older." +
                    " Check addons:tr:IsVerTwoFour or addons:tr:GetVersion");
            }
            throw new KOSUnavailableAddonException("DESCENTPROFILEGRADES", "Trajectories");
        }

        private void SetProfileGrades(ListValue<BooleanValue> grades)
        {
            if (shared.Vessel != FlightGlobals.ActiveVessel)
                throw new KOSException("You may only call addons:TR:DescentProfileGrades from the active vessel.");
            if (Available())
            {
                if (grades != null && grades.Count > 3)
                {
                    TRWrapper.DescentProfileGrades = new List<bool>
                    {
                        grades[0],    // atmospheric entry node
                        grades[1],    // high altitude node
                        grades[2],    // low altitude node
                        grades[3]     // final approach node
                    };
                }
                throw new KOSException("DescentProfileGrades was passed an invalid list, make sure to have at least 4 values in the list.");
            }
            throw new KOSUnavailableAddonException("DESCENTPROFILEGRADES", "Trajectories");
        }

        public override BooleanValue Available() => TRWrapper.Wrapped();
    }
}