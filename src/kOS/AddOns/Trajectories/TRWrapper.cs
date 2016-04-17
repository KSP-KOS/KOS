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
        private static Type trajectoriesType = null;
        private static object trFetch = null;
        private static MethodInfo trComputeMethod = null;
        private static PropertyInfo patches = null;
        private static PropertyInfo patchImpact = null;
        private static Type descentProfileType = null;
        private static object descentProfileFetch = null;
        
        private static void init()
        {
            Type trajectoriesMapOverlay = AssemblyLoader.loadedAssemblies
                .Select(a => a.assembly.GetExportedTypes())
                .SelectMany(t => t)
                .FirstOrDefault(t => t.FullName == "Trajectories.MapOverlay"); //MapOverlay is public, use it to get the Trajectories assembly
            if (trajectoriesMapOverlay == null) //Check everything because who knows when Trajectories mod classes will change
            {
                Debug.Log("Trajectories MapOverlay Type is null. Trajectories not installed or is wrong version.");
                wrapped = false;
                return;
            }
            Assembly trajectories = trajectoriesMapOverlay.Assembly;
            if (trajectories == null)
            {
                Debug.Log("Trajectories assembly is null.");
                wrapped = false;
                return;
            }
            trajectoriesType = trajectories.GetType("Trajectories.Trajectory");
            if (trajectoriesType == null)
            {
                Debug.Log("Trajectories.Trajectory Type is Null. Incompatible Trajectories version.");
                wrapped = false;
                return;
            }
            descentProfileType = trajectories.GetType("Trajectories.DescentProfile");
            if (descentProfileType == null)
            {
                Debug.Log("Trajectories.DescentProfile Type is Null. Incompatible Trajectories version.");
                wrapped = false;
                return;
            }
            trFetch = trajectoriesType.GetProperty("fetch").GetValue(null, null);
            if (trFetch == null)
            {
                Debug.Log("Trajectories.Trajectory fetch failed.");
                wrapped = false;
                return;
            }
            trComputeMethod = trajectoriesType.GetMethod("ComputeTrajectory", new[] { typeof(Vessel), descentProfileType, typeof(bool) });
            if (trComputeMethod == null)
            {
                Debug.Log("Trajectories.Trajectory.ComputeTrajectory method is null.");
                wrapped = false;
                return;
            }
            patches = trajectoriesType.GetProperty("patches");
            if (patches == null)
            {
                Debug.Log("Trajectories.Trajectory.patches PropertyInfo is null.");
                wrapped = false;
                return;
            }
            patchImpact = patches.PropertyType.GetGenericArguments()[0].GetProperty("impactPosition");
            if (patchImpact == null)
            {
                Debug.Log("Trajectories.Trajectory.patch.impactPosition PropertyInfo is null.");
                wrapped = false;
                return;
            }
            descentProfileFetch = descentProfileType.GetProperty("fetch").GetValue(null, null);
            if (descentProfileFetch == null)
            {
                Debug.Log("Trajectories.DescentProfile fetch failed.");
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
                return (BooleanValue)wrapped;
            }
            else //if available == null
            {
                init();
                return (BooleanValue)wrapped;
            }
        }
    }
}