using kOS.Safe.Encapsulation;
using kOS.Safe.Utilities;
using System;
using System.Reflection;
using UnityEngine;

namespace kOS.AddOns.TrajectoriesAddon
{
    public class TRWrapper
    {
        private static bool? wrapped = null;
        private static Type trajectoriesAPIType = null;
        private static MethodInfo trGetImpactPosition = null;
        private static MethodInfo trCorrectedDirection = null;
        private static MethodInfo trPlannedDirection = null;
        private static MethodInfo trHasTarget = null;
        private static MethodInfo trSetTarget = null;
        private static PropertyInfo trAlwaysUpdate = null;

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


        private static void init()
        {
            SafeHouse.Logger.Log("Attempting to Grab Trajectories Assembly...");
            trajectoriesAPIType = GetType("Trajectories.API");
            if (trajectoriesAPIType == null)
            {
                SafeHouse.Logger.Log("Trajectories API Type is null. Trajectories not installed or is wrong version.");
                wrapped = false;
                return;
            }
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
            trHasTarget = trajectoriesAPIType.GetMethod("HasTarget") ?? trajectoriesAPIType.GetMethod("hasTarget");
            if (trHasTarget == null)
            {
                SafeHouse.Logger.Log("Trajectories.API.HasTarget method is null.");
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
            wrapped = true;
        }

        public static Vector3? ImpactVector()
        {
            return (Vector3?)trGetImpactPosition.Invoke(null, new object[] { });
        }

        public static Vector3? CorrectedDirection()
        {
            return (Vector3?)trCorrectedDirection.Invoke(null, new object[] { });
        }

        public static Vector3? PlannedDirection()
        {
            return (Vector3?)trPlannedDirection.Invoke(null, new object[] { });
        }

        public static bool? HasTarget()
        {
            if (trHasTarget == null)
                return null;
            return (bool)trHasTarget.Invoke(null, new object[] { });
        }

        public static void SetTarget(double lat, double lon, double alt)
        {
            trSetTarget.Invoke(null, new object[] { lat, lon, alt });
        }

        public static BooleanValue Wrapped()
        {
            if (wrapped != null)
            {
                return wrapped;
            }
            else //if wrapped == null
            {
                init();
                return wrapped;
            }
        }
    }
}