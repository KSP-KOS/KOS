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
            trComputeMethod.Invoke(trFetch, new object[] { FlightGlobals.ActiveVessel, descentProfileFetch, true }); //Update Trajectories prediction
            foreach (object patch in (IEnumerable)patches.GetValue(trFetch, null)) //Check if each patch (prediction segment) has an impactPosition
            {
                Vector3? impactPosition = (Vector3?)patchImpact.GetValue(patch, null);
                if (impactPosition != null) return impactPosition;
            };
            return null;
        }
        public static BooleanValue Wrapped()
        {
            if (wrapped != null)
            {
                return wrapped;
            }
            else //if available == null
            {
                init();
                return (BooleanValue)wrapped;
            }
        }
    }
}