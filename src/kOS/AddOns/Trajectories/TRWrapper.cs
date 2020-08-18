using System;
using System.Collections.Generic;
using System.Reflection;
using kOS.Safe.Encapsulation;
using kOS.Safe.Utilities;
using UnityEngine;

namespace kOS.AddOns.TrajectoriesAddon
{
    public static class TRWrapper
    {
        private static bool? wrapped = null;
        private static Type trajectoriesAPIType = null;
        private static MethodInfo trGetTimeTillImpact = null;
        private static MethodInfo trGetImpactPosition = null;
        private static MethodInfo trCorrectedDirection = null;
        private static MethodInfo trPlannedDirection = null;
        private static MethodInfo trHasTarget = null;
        private static MethodInfo trSetTarget = null;
        private static MethodInfo trGetTarget = null;
        private static MethodInfo trClearTarget = null;
        private static MethodInfo trResetDescentProfile = null;
        private static PropertyInfo trAlwaysUpdate = null;
        private static PropertyInfo trProgradeEntry = null;
        private static PropertyInfo trRetrogradeEntry = null;
        private static PropertyInfo trDescentProfileAngles = null;
        private static PropertyInfo trDescentProfileModes = null;
        private static PropertyInfo trDescentProfileGrades = null;

        private static Type GetType(string name)
        {
            Type type = null;
            AssemblyLoader.loadedAssemblies.TypeOperation(t =>
            {
                if (t.FullName == name)
                    type = t;
            });
            return type;
        }

        private static void Init()
        {
            SafeHouse.Logger.Log("Attempting to Grab Trajectories Assembly...");
            trajectoriesAPIType = GetType("Trajectories.API");
            if (trajectoriesAPIType == null)
            {
                SafeHouse.Logger.Log("Trajectories API Type is null. Trajectories is not installed or is wrong version.");
                wrapped = false;
                return;
            }

            // Trajectories v2.2.0+ API Has version properties. Checking versions here.
            if (trajectoriesAPIType.GetProperty("GetVersion") == null)
            {
                if (trajectoriesAPIType.GetMethod("HasTarget") == null) // assume pre v2.0.0 (Old API)
                {
                    GetVersion = "";
                    GetVersionMajor = 0;
                    GetVersionMinor = 0;
                    GetVersionPatch = 0;
                    IsVerTwo = false;
                    IsVerTwoTwo = false;
                    IsVerTwoFour = false;
                    SafeHouse.Logger.Log("Checking Trajectories version: API.HasTarget method is null. Assuming version is pre 2.0.0");
                }
                else // assume v2.0.0 and v2.1.0 (API is identical in these versions)
                {
                    GetVersion = "2.0.0";
                    GetVersionMajor = 2;
                    GetVersionMinor = 0;
                    GetVersionPatch = 0;
                    IsVerTwo = true;
                    IsVerTwoTwo = false;
                    IsVerTwoFour = false;
                    SafeHouse.Logger.Log("Checking Trajectories version: API.GetVersion method is null. Assuming version is pre 2.2.0");
                }
            }
            else // assume v2.2.0 and above (New API)
            {
                GetVersion = (string)trajectoriesAPIType.GetProperty("GetVersion").GetValue(null, null);
                Version version = new Version(GetVersion);
                GetVersionMajor = version.Major;
                GetVersionMinor = version.Minor;
                GetVersionPatch = version.Build;
                IsVerTwo = true;
                IsVerTwoTwo = true;
                // check for major versions above v2
                if (version.Major > 2)
                    IsVerTwoFour = true;
                else
                    IsVerTwoFour = (version.Major == 2 && version.Minor >= 4);
                SafeHouse.Logger.Log("Checking Trajectories version: API.GetVersion returned version: v" + GetVersion);
            }

            // Method and property checking.
            // Trajectories 2.0.0 changed the capitalization of this method.  Trying both spellings here to support older Trajectories versions:
            trGetImpactPosition = trajectoriesAPIType.GetMethod("GetImpactPosition") ?? trajectoriesAPIType.GetMethod("getImpactPosition");
            if (trGetImpactPosition == null)
            {
                SafeHouse.Logger.Log("Trajectories.API.GetImpactPosition method is null.");
                wrapped = false;
                return;
            }
            // Trajectories 2.0.0 changed the capitalization of this method.  Trying both spellings here to support older Trajectories versions:
            trCorrectedDirection = trajectoriesAPIType.GetMethod("CorrectedDirection") ?? trajectoriesAPIType.GetMethod("correctedDirection");
            if (trCorrectedDirection == null)
            {
                SafeHouse.Logger.Log("Trajectories.API.CorrectedDirection method is null.");
                wrapped = false;
                return;
            }
            // Trajectories 2.0.0 changed the capitalization of this method.  Trying both spellings here to support older Trajectories versions:
            trPlannedDirection = trajectoriesAPIType.GetMethod("PlannedDirection") ?? trajectoriesAPIType.GetMethod("plannedDirection");
            if (trPlannedDirection == null)
            {
                SafeHouse.Logger.Log("Trajectories.API.PlannedDirection method is null.");
                wrapped = false;
                return;
            }
            // Trajectories 2.0.0 changed the capitalization of this method.  Trying both spellings here to support older Trajectories versions:
            trSetTarget = trajectoriesAPIType.GetMethod("SetTarget") ?? trajectoriesAPIType.GetMethod("setTarget");
            if (trSetTarget == null)
            {
                SafeHouse.Logger.Log("Trajectories.API.SetTarget method is null.");
                wrapped = false;
                return;
            }
            // Trajectories 2.0.0 changed the capitalization of this method.  Trying both spellings here to support older Trajectories versions:
            trAlwaysUpdate = trajectoriesAPIType.GetProperty("AlwaysUpdate") ?? trajectoriesAPIType.GetProperty("alwaysUpdate");
            if (trAlwaysUpdate == null)
            {
                SafeHouse.Logger.Log("Trajectories.API.AlwaysUpdate property is null.");
                wrapped = false;
                return;
            }
            trAlwaysUpdate.SetValue(null, true, null);
            if ((bool)trAlwaysUpdate.GetValue(null, null) == false)
            {
                SafeHouse.Logger.Log("Trajectories.API.AlwaysUpdate was not set.");
                wrapped = false;
                return;
            }

            // Trajectories v2.0.0 HasTarget method
            if (IsVerTwo)
            {
                trHasTarget = trajectoriesAPIType.GetMethod("HasTarget");
                if (trHasTarget == null)
                {
                    SafeHouse.Logger.Log("Trajectories.API.HasTarget method is null");
                    wrapped = false;
                    return;
                }
            }

            // Trajectories v2.2.0 and above methods and properties
            if (IsVerTwoTwo)
            {
                trGetTimeTillImpact = trajectoriesAPIType.GetMethod("GetTimeTillImpact");
                if (trGetTimeTillImpact == null)
                {
                    SafeHouse.Logger.Log("Trajectories.API.GetTimeTillImpact method is null");
                    wrapped = false;
                    return;
                }
                trProgradeEntry = trajectoriesAPIType.GetProperty("ProgradeEntry");
                if (trProgradeEntry == null)
                {
                    SafeHouse.Logger.Log("Trajectories.API.ProgradeEntry property is null");
                    wrapped = false;
                    return;
                }
                trRetrogradeEntry = trajectoriesAPIType.GetProperty("RetrogradeEntry");
                if (trRetrogradeEntry == null)
                {
                    SafeHouse.Logger.Log("Trajectories.API.RetrogradeEntry property is null");
                    wrapped = false;
                    return;
                }
            }

            // Trajectories v2.4.0 and above methods and properties
            if (IsVerTwoFour)
            {
                trGetTarget = trajectoriesAPIType.GetMethod("GetTarget");
                if (trGetTarget == null)
                {
                    SafeHouse.Logger.Log("Trajectories.API.GetTarget method is null");
                    wrapped = false;
                    return;
                }
                trClearTarget = trajectoriesAPIType.GetMethod("ClearTarget");
                if (trClearTarget == null)
                {
                    SafeHouse.Logger.Log("Trajectories.API.ClearTarget method is null");
                    wrapped = false;
                    return;
                }
                trResetDescentProfile = trajectoriesAPIType.GetMethod("ResetDescentProfile");
                if (trResetDescentProfile == null)
                {
                    SafeHouse.Logger.Log("Trajectories.API.ResetDescentProfile method is null");
                    wrapped = false;
                    return;
                }
                trDescentProfileAngles = trajectoriesAPIType.GetProperty("DescentProfileAngles");
                if (trDescentProfileAngles == null)
                {
                    SafeHouse.Logger.Log("Trajectories.API.DescentProfileAngles property is null");
                    wrapped = false;
                    return;
                }
                trDescentProfileModes = trajectoriesAPIType.GetProperty("DescentProfileModes");
                if (trDescentProfileModes == null)
                {
                    SafeHouse.Logger.Log("Trajectories.API.DescentProfileModes property is null");
                    wrapped = false;
                    return;
                }
                trDescentProfileGrades = trajectoriesAPIType.GetProperty("DescentProfileGrades");
                if (trDescentProfileGrades == null)
                {
                    SafeHouse.Logger.Log("Trajectories.API.DescentProfileGrades property is null");
                    wrapped = false;
                    return;
                }
            }

            wrapped = true;
        }

        // Version checking properties
        public static string GetVersion { get; private set; }
        public static int GetVersionMajor { get; private set; }
        public static int GetVersionMinor { get; private set; }
        public static int GetVersionPatch { get; private set; }
        public static bool IsVerTwo { get; private set; }
        public static bool IsVerTwoTwo { get; private set; }
        public static bool IsVerTwoFour { get; private set; }

        // Standard methods
        public static Vector3? ImpactVector() => (Vector3?)trGetImpactPosition.Invoke(null, new object[] { });

        public static Vector3? CorrectedDirection() => (Vector3?)trCorrectedDirection.Invoke(null, new object[] { });

        public static Vector3? PlannedDirection() => (Vector3?)trPlannedDirection.Invoke(null, new object[] { });

        public static void SetTarget(double lat, double lon, double alt) => trSetTarget.Invoke(null, new object[] { lat, lon, alt });

        // Trajectories v2.0.0 HasTarget method
        public static bool? HasTarget()
        {
            if (trHasTarget == null)
                return null;
            return (bool?)trHasTarget.Invoke(null, new object[] { });
        }

        // Trajectories v2.2.0 and above methods and properties
        public static double? GetTimeTillImpact()
        {
            if (trGetTimeTillImpact == null)
                return null;
            return (double?)trGetTimeTillImpact.Invoke(null, new object[] { });
        }

        public static bool? ProgradeEntry
        {
            get
            {
                if (trProgradeEntry == null) // will be null if TR version too low.
                    return null;
                return (bool?)trProgradeEntry.GetValue(null, null);
            }
            set
            {
                if (trProgradeEntry != null) // will be null if TR version too low.
                    trProgradeEntry.SetValue(null, value, null);
            }
        }

        public static bool? RetrogradeEntry
        {
            get
            {
                if (trRetrogradeEntry == null) // will be null if TR version too low.
                    return null;
                return (bool?)trRetrogradeEntry.GetValue(null, null);
            }
            set
            {
                if (trRetrogradeEntry != null) // will be null if TR version too low.
                    trRetrogradeEntry.SetValue(null, value, null);
            }
        }

        // Trajectories v2.4.0 and above methods and properties
        public static Vector3d? GetTarget()
        {
            if (trGetTarget == null)
                return null;
            return (Vector3d?)trGetTarget.Invoke(null, new object[] { });
        }

        public static void ClearTarget()
        {
            if (trClearTarget == null)
                return;
            trClearTarget.Invoke(null, new object[] { });
        }

        public static void ResetDescentProfile(double AoA)
        {
            if (trResetDescentProfile == null)
                return;
            trResetDescentProfile.Invoke(null, new object[] { AoA });
        }

        public static List<double> DescentProfileAngles
        {
            get
            {
                if (trDescentProfileAngles == null)
                    return null;
                return (List<double>)trDescentProfileAngles.GetValue(null, null);
            }
            set
            {
                if (trDescentProfileAngles != null)
                    trDescentProfileAngles.SetValue(null, value, null);
            }
        }

        public static List<bool> DescentProfileModes
        {
            get
            {
                if (trDescentProfileModes == null)
                    return null;
                return (List<bool>)trDescentProfileModes.GetValue(null, null);
            }
            set
            {
                if (trDescentProfileModes != null)
                    trDescentProfileModes.SetValue(null, value, null);
            }
        }

        public static List<bool> DescentProfileGrades
        {
            get
            {
                if (trDescentProfileGrades == null)
                    return null;
                return (List<bool>)trDescentProfileGrades.GetValue(null, null);
            }
            set
            {
                if (trDescentProfileGrades != null)
                    trDescentProfileGrades.SetValue(null, value, null);
            }
        }

        public static BooleanValue Wrapped()
        {
            if (wrapped != null)
            {
                return wrapped;
            }
            else //if wrapped == null
            {
                Init();
                return wrapped;
            }
        }
    }
}