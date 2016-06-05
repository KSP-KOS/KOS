using kOS.Safe.Encapsulation;
using System;
using System.Collections;
using System.Linq;
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
        private static MethodInfo trSetTarget = null;

        private static void init()
        {
            trajectoriesAPIType = AssemblyLoader.loadedAssemblies
                .Select(a => a.assembly.GetExportedTypes())
                .SelectMany(t => t)
                .FirstOrDefault(t => t.FullName == "Trajectories.API");
            if (trajectoriesAPIType == null)
            {
                Debug.Log("[kOS] Trajectories API Type is null. Trajectories not installed or is wrong version.");
                wrapped = false;
                return;
            }
            trGetImpactPosition = trajectoriesAPIType.GetMethod("getImpactPosition");
            if (trGetImpactPosition == null)
            {
                Debug.Log("[kOS] Trajectories.API.getImpactPosition method is null.");
                wrapped = false;
                return;
            }
            trCorrectedDirection = trajectoriesAPIType.GetMethod("correctedDirection");
            if (trCorrectedDirection == null)
            {
                Debug.Log("[kOS] Trajectories.API.correctedDirection method is null.");
                wrapped = false;
                return;
            }
            trPlannedDirection = trajectoriesAPIType.GetMethod("plannedDirection");
            if (trPlannedDirection == null)
            {
                Debug.Log("[kOS] Trajectories.API.plannedDirection method is null.");
                wrapped = false;
                return;
            }
            trSetTarget = trajectoriesAPIType.GetMethod("setTarget");
            if (trSetTarget == null)
            {
                Debug.Log("[kOS] Trajectories.API.setTarget method is null.");
                wrapped = false;
                return;
            }
            wrapped = true;
        }
        public static Vector3? impactVector()
        {
            return (Vector3?)trGetImpactPosition.Invoke(null, new object[] {});
        }
        public static Vector3 correctedDirection()
        {
            return (Vector3)trCorrectedDirection.Invoke(null, new object[] {});
        }
        public static Vector3 plannedDirection()
        {
            return (Vector3)trPlannedDirection.Invoke(null, new object[] { });
        }
        public static void setTarget(double lat, double lon, double alt)
        {
            trSetTarget.Invoke(null, new object[] {lat,lon,alt});
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