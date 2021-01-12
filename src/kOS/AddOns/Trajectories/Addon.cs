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
            AddSuffix("GETVERSIONMAJOR", new Suffix<ScalarValue>(GetVersionMajor, "Get version Major."));
            AddSuffix("GETVERSIONMINOR", new Suffix<ScalarValue>(GetVersionMinor, "Get version Minor."));
            AddSuffix("GETVERSIONPATCH", new Suffix<ScalarValue>(GetVersionPatch, "Get version Patch."));
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
            AddSuffix("RESETDESCENTPROFILE", new OneArgsSuffix<ScalarValue>(ResetDescentProfile, "Reset the descent profile to the passed AoA value in degrees."));
            AddSuffix("DESCENTANGLES", new SetSuffix<ListValue>(GetProfileAngles, SetProfileAngles, "Descent profile angles in degrees, also sets Retrograde if any values are greater than ±90°, List(entry, high altitude, low altitude, final approach)."));
            AddSuffix("DESCENTMODES", new SetSuffix<ListValue>(GetProfileModes, SetProfileModes, "Descent profile modes, true = AoA, false = Horizon, List(entry, high altitude, low altitude, final approach)."));
            AddSuffix("DESCENTGRADES", new SetSuffix<ListValue>(GetProfileGrades, SetProfileGrades, "Descent profile grades, true = Retrograde, false = Prograde, List(entry, high altitude, low altitude, final approach)."));
        }

        // Version checking suffixes.
        private StringValue GetVersion()
        {
            if (Available())
                return TRWrapper.GetVersion;
            throw new KOSUnavailableAddonException("GETVERSION", "Trajectories");
        }

        private ScalarValue GetVersionMajor()
        {
            if (Available())
                return TRWrapper.GetVersionMajor;
            throw new KOSUnavailableAddonException("GETVERSIONMAJOR", "Trajectories");
        }

        private ScalarValue GetVersionMinor()
        {
            if (Available())
                return TRWrapper.GetVersionMinor;
            throw new KOSUnavailableAddonException("GETVERSIONMINOR", "Trajectories");
        }

        private ScalarValue GetVersionPatch()
        {
            if (Available())
                return TRWrapper.GetVersionPatch;
            throw new KOSUnavailableAddonException("GETVERSIONPATCH", "Trajectories");
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
                throw new KOSException("You may only call addons:tr:IMPACTPOS from the active vessel. Always check addons:tr:HASIMPACT");
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
                throw new KOSException("IMPACTPOS is not available. Remember to check addons:tr:HASIMPACT");
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
                throw new KOSException("You may only call addons:tr:CORRECTEDVECT from the active vessel and must also have a trajectories target set." +
                    " Always check addons:tr:HASIMPACT and addons:tr:HASTARGET");
            if (Available())
            {
                Vector3? vect = TRWrapper.CorrectedDirection();
                if (vect != null)
                {
                    Vector3 vector = (Vector3)vect;
                    return new Vector(vector.x, vector.y, vector.z);
                }
                throw new KOSException("CORRECTEDVECT is not available. Remember to check addons:tr:HASIMPACT and addons:tr:HASTARGET");
            }
            throw new KOSUnavailableAddonException("CORRECTEDVECT", "Trajectories");
        }

        private Vector PlannedVector()
        {
            if (shared.Vessel != FlightGlobals.ActiveVessel)
                throw new KOSException("You may only call addons:tr:PLANNEDVECT from the active vessel and must also have a trajectories target set." +
                    " Always check addons:tr:HASIMPACT and addons:tr:HASTARGET");
            if (Available())
            {
                Vector3? vect = TRWrapper.PlannedDirection();
                if (vect != null)
                {
                    Vector3 vector = (Vector3)vect;
                    return new Vector(vector.x, vector.y, vector.z);
                }
                throw new KOSException("PLANNEDVECT is not available. Remember to check addons:tr:HASIMPACT and addons:tr:HASTARGET");
            }
            throw new KOSUnavailableAddonException("PLANNEDVECT", "Trajectories");
        }

        private void SetTarget(GeoCoordinates target)
        {
            if (shared.Vessel != FlightGlobals.ActiveVessel)
                throw new KOSException("You may only call addons:tr:SETTARGET from the active vessel.");
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
                throw new KOSException("You may only call addons:tr:HASTARGET from the active vessel.");
            if (Available())
            {
                bool? result = TRWrapper.HasTarget();
                if (result != null)
                    return result;
                throw new KOSException("HASTARGET is not available. It was added in Trajectories v2.0.0. and your version might be older." +
                    " Check addons:tr:ISVERTWO or addons:tr:GETVERSION");
            }
            throw new KOSUnavailableAddonException("HASTARGET", "Trajectories");
        }

        // v2.2.0 and above suffixes.
        private ScalarValue TimeTillImpact()
        {
            if (shared.Vessel != FlightGlobals.ActiveVessel)
                throw new KOSException("You may only call addons:tr:TIMETILLIMPACT from the active vessel.");
            if (Available())
            {
                double? result = TRWrapper.GetTimeTillImpact();
                if (result != null)
                    return result;
                throw new KOSException("TIMETILLIMPACT is not available. Remember to check addons:tr:HASIMPACT." +
                    " Also TIMETILLIMPACT was added in Trajectories v2.2.0. and your version might be older." +
                    " Check addons:tr:ISVERTWOTWO or addons:tr:GETVERSION");
            }
            throw new KOSUnavailableAddonException("TIMETILLIMPACT", "Trajectories");
        }

        private BooleanValue IsPrograde()
        {
            if (shared.Vessel != FlightGlobals.ActiveVessel)
                throw new KOSException("You may only call addons:tr:PROGRADE from the active vessel.");
            if (Available())
            {
                bool? result = TRWrapper.ProgradeEntry;
                if (result != null)
                    return result;
                throw new KOSException("PROGRADE is not available. It was added in Trajectories v2.2.0. and your version might be older." +
                    " Check addons:tr:ISVERTWOTWO or addons:tr:GETVERSION");
            }
            throw new KOSUnavailableAddonException("PROGRADE", "Trajectories");
        }

        private void SetPrograde(BooleanValue value)
        {
            if (shared.Vessel != FlightGlobals.ActiveVessel)
                throw new KOSException("You may only call addons:tr:PROGRADE from the active vessel.");
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
                throw new KOSException("You may only call addons:tr:RETROGRADE from the active vessel.");
            if (Available())
            {
                bool? result = TRWrapper.RetrogradeEntry;
                if (result != null)
                    return result;
                throw new KOSException("RETROGRADE is not available. It was added in Trajectories v2.2.0. and your version might be older." +
                    " Check addons:tr:ISVERTWOTWO or addons:tr:GETVERSION");
            }
            throw new KOSUnavailableAddonException("RETROGRADE", "Trajectories");
        }

        private void SetRetrograde(BooleanValue value)
        {
            if (shared.Vessel != FlightGlobals.ActiveVessel)
                throw new KOSException("You may only call addons:tr:RETROGRADE from the active vessel.");
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
                throw new KOSException("You may only call addons:tr:GETTARGET from the active vessel and must also have a trajectories target set." +
                    " Always check addons:tr:HASTARGET");
            if (Available())
            {
                Vector3d? result = TRWrapper.GetTarget();
                if (result != null)
                    return new GeoCoordinates(shared, result.Value.x, result.Value.y);

                throw new KOSException("GETTARGET is not available or no target is set. Remember to check addons:tr:HASTARGET." +
                    " Also GETTARGET was added in Trajectories v2.4.0. and your version might be older." +
                    " Check addons:tr:ISVERTWOFOUR or addons:tr:GETVERSION");
            }
            throw new KOSUnavailableAddonException("GETTARGET", "Trajectories");
        }

        private void ClearTarget()
        {
            if (shared.Vessel != FlightGlobals.ActiveVessel)
                throw new KOSException("You may only call addons:tr:CLEARTARGET from the active vessel.");
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
                throw new KOSException("You may only call addons:tr:RESETDESCENTPROFILE from the active vessel.");
            if (Available())
            {
                TRWrapper.ResetDescentProfile(aoa * Mathf.Deg2Rad);
                return;
            }
            throw new KOSUnavailableAddonException("RESETDESCENTPROFILE", "Trajectories");
        }

        private ListValue GetProfileAngles()
        {
            if (shared.Vessel != FlightGlobals.ActiveVessel)
                throw new KOSException("You may only call addons:tr:DESCENTANGLES from the active vessel.");
            if (Available())
            {
                List<double> result = TRWrapper.DescentProfileAngles;
                if (result != null && result.Count > 3)
                {
                    return new ListValue
                    {
                        (ScalarValue)result[0] * Mathf.Rad2Deg,    // atmospheric entry node
                        (ScalarValue)result[1] * Mathf.Rad2Deg,    // high altitude node
                        (ScalarValue)result[2] * Mathf.Rad2Deg,    // low altitude node
                        (ScalarValue)result[3] * Mathf.Rad2Deg     // final approach node
                    };
                }
                throw new KOSException("DESCENTANGLES is not available. It was added in Trajectories v2.4.0. and your version might be older." +
                    " Check addons:tr:ISVERTWOFOUR or addons:tr:GETVERSION");
            }
            throw new KOSUnavailableAddonException("DESCENTANGLES", "Trajectories");
        }

        private void SetProfileAngles(ListValue aoa)
        {
            if (shared.Vessel != FlightGlobals.ActiveVessel)
                throw new KOSException("You may only call addons:tr:DESCENTANGLES from the active vessel.");
            if (Available())
            {
                if (aoa != null && aoa.Count > 3)
                {
                    // check for correct types
                    foreach (Structure item in aoa)
                    {
                        if (!(item.GetType() == typeof(ScalarIntValue) || item.GetType() == typeof(ScalarDoubleValue)))
                            throw new KOSException("DESCENTANGLES was passed an invalid type in its list, it requires a list of ScalarValues.");
                    }

                    TRWrapper.DescentProfileAngles = new List<double>
                    {
                        ((ScalarValue)aoa[0]) * Mathf.Deg2Rad,    // atmospheric entry node
                        ((ScalarValue)aoa[1]) * Mathf.Deg2Rad,    // high altitude node
                        ((ScalarValue)aoa[2]) * Mathf.Deg2Rad,    // low altitude node
                        ((ScalarValue)aoa[3]) * Mathf.Deg2Rad     // final approach node
                    };
                    return;
                }
                throw new KOSException("DESCENTANGLES was passed an invalid list, make sure to have at least 4 items in the list.");
            }
            throw new KOSUnavailableAddonException("DESCENTANGLES", "Trajectories");
        }

        private ListValue GetProfileModes()
        {
            if (shared.Vessel != FlightGlobals.ActiveVessel)
                throw new KOSException("You may only call addons:tr:DESCENTMODES from the active vessel.");
            if (Available())
            {
                List<bool> result = TRWrapper.DescentProfileModes;
                if (result != null && result.Count > 3)
                {
                    return new ListValue
                    {
                        (BooleanValue)result[0],    // atmospheric entry node
                        (BooleanValue)result[1],    // high altitude node
                        (BooleanValue)result[2],    // low altitude node
                        (BooleanValue)result[3]     // final approach node
                    };
                }
                throw new KOSException("DESCENTMODES is not available. It was added in Trajectories v2.4.0. and your version might be older." +
                    " Check addons:tr:IsVerTwoFour or addons:tr:GETVERSION");
            }
            throw new KOSUnavailableAddonException("DESCENTMODES", "Trajectories");
        }

        private void SetProfileModes(ListValue modes)
        {
            if (shared.Vessel != FlightGlobals.ActiveVessel)
                throw new KOSException("You may only call addons:tr:DESCENTMODES from the active vessel.");
            if (Available())
            {
                if (modes != null && modes.Count > 3)
                {
                    // check for correct types
                    foreach (Structure item in modes)
                    {
                        if (item.GetType() != typeof(BooleanValue))
                            throw new KOSException("DESCENTMODES was passed an invalid type in its list, it requires a list of BooleanValues.");
                    }

                    TRWrapper.DescentProfileModes = new List<bool>
                    {
                        (BooleanValue)modes[0],    // atmospheric entry node
                        (BooleanValue)modes[1],    // high altitude node
                        (BooleanValue)modes[2],    // low altitude node
                        (BooleanValue)modes[3]     // final approach node
                    };
                    return;
                }
                throw new KOSException("DESCENTMODES was passed an invalid list, make sure to have at least 4 items in the list.");
            }
            throw new KOSUnavailableAddonException("DESCENTMODES", "Trajectories");
        }

        private ListValue GetProfileGrades()
        {
            if (shared.Vessel != FlightGlobals.ActiveVessel)
                throw new KOSException("You may only call addons:tr:DESCENTGRADES from the active vessel.");
            if (Available())
            {
                List<bool> result = TRWrapper.DescentProfileGrades;
                if (result != null && result.Count > 3)
                {
                    return new ListValue
                    {
                        (BooleanValue)result[0],    // atmospheric entry node
                        (BooleanValue)result[1],    // high altitude node
                        (BooleanValue)result[2],    // low altitude node
                        (BooleanValue)result[3]     // final approach node
                    };
                }
                throw new KOSException("DESCENTGRADES is not available. It was added in Trajectories v2.4.0. and your version might be older." +
                    " Check addons:tr:ISVERTWOFOUR or addons:tr:GETVERSION");
            }
            throw new KOSUnavailableAddonException("DESCENTGRADES", "Trajectories");
        }

        private void SetProfileGrades(ListValue grades)
        {
            if (shared.Vessel != FlightGlobals.ActiveVessel)
                throw new KOSException("You may only call addons:tr:DESCENTGRADES from the active vessel.");
            if (Available())
            {
                if (grades != null && grades.Count > 3)
                {
                    // check for correct types
                    foreach (Structure item in grades)
                    {
                        if (item.GetType() != typeof(BooleanValue))
                            throw new KOSException("DESCENTGRADES was passed an invalid type in its list, it requires a list of BooleanValues.");
                    }

                    TRWrapper.DescentProfileGrades = new List<bool>
                    {
                        (BooleanValue)grades[0],    // atmospheric entry node
                        (BooleanValue)grades[1],    // high altitude node
                        (BooleanValue)grades[2],    // low altitude node
                        (BooleanValue)grades[3]     // final approach node
                    };
                    return;
                }
                throw new KOSException("DESCENTGRADES was passed an invalid list, make sure to have at least 4 items in the list.");
            }
            throw new KOSUnavailableAddonException("DESCENTGRADES", "Trajectories");
        }

        public override BooleanValue Available() => TRWrapper.Wrapped();
    }
}