using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

namespace kOS
{

    [kOSBinding("ksp")]
    public class BindingsFlightStats : Binding
    {
        public override void AddTo(BindingManager manager)
        {


            manager.AddGetter("ALT:RADAR",      delegate(CPU cpu) { return cpu.Vessel.heightFromTerrain > 0 ? Mathf.Min(cpu.Vessel.heightFromTerrain, (float)cpu.Vessel.altitude) : (float)cpu.Vessel.altitude; });
            manager.AddGetter("ALT:APOAPSIS",   delegate(CPU cpu) { return cpu.Vessel.orbit.ApA; });
            manager.AddGetter("ALT:PERIAPSIS",  delegate(CPU cpu) { return cpu.Vessel.orbit.PeA; });
            manager.AddGetter("ETA:APOAPSIS",   delegate(CPU cpu) { return cpu.Vessel.orbit.timeToAp; });
            manager.AddGetter("ETA:PERIAPSIS",  delegate(CPU cpu) { return cpu.Vessel.orbit.timeToPe; });

            manager.AddGetter("MISSIONTIME",    delegate(CPU cpu) { return cpu.Vessel.missionTime; });
            manager.AddGetter("TIME",           delegate(CPU cpu) { return new kOS.TimeSpan(Planetarium.GetUniversalTime()); });

            manager.AddGetter("STATUS",         delegate(CPU cpu) { return cpu.Vessel.situation.ToString().Replace("_", " "); });
			manager.AddGetter("COMMRANGE",      delegate(CPU cpu) { return VesselUtils.GetCommRange(cpu.Vessel); });
			manager.AddGetter("INCOMMRANGE",    delegate(CPU cpu) { return Convert.ToDouble(CheckCommRange(cpu.Vessel)); });


            

            manager.AddGetter("SHIP",           delegate(CPU cpu) { return new VesselTarget(cpu.Vessel, cpu); });


            manager.AddGetter("AV", delegate(CPU cpu) { return cpu.Vessel.transform.InverseTransformDirection(cpu.Vessel.rigidbody.angularVelocity); });
            manager.AddGetter("STAGE", delegate(CPU cpu) { return new StageValues(cpu.Vessel); });
            

            manager.AddGetter("ENCOUNTER",      delegate(CPU cpu) { return VesselUtils.TryGetEncounter(cpu.Vessel); });

            manager.AddGetter("NEXTNODE",       delegate(CPU cpu)
            {
                var vessel = cpu.Vessel;
                if (!vessel.patchedConicSolver.maneuverNodes.Any()) { throw new kOSException("No maneuver nodes present!"); }

                return Node.FromExisting(vessel, vessel.patchedConicSolver.maneuverNodes[0]);
            });

            // These are now considered shortcuts to SHIP:suffix
            foreach (String scName in VesselTarget.ShortCuttableShipSuffixes)
            {
                manager.AddGetter(scName, delegate(CPU cpu) { return new VesselTarget(cpu.Vessel, cpu).GetSuffix(scName); });
            }
            
            manager.AddSetter("VESSELNAME", delegate(CPU cpu, object value) { cpu.Vessel.vesselName = value.ToString(); });
        }

        private static float getLattitude(CPU cpu)
        {
            float retVal = (float)cpu.Vessel.latitude;

            if (retVal > 90) return 90;
            if (retVal < -90) return -90;
                
            return retVal;
        }

        private static float getLongitude(CPU cpu)
        {
            float retVal = (float)cpu.Vessel.longitude;

            while (retVal > 180) retVal -= 360;
            while (retVal < -180) retVal += 360;

            return retVal;
        }

		private static bool CheckCommRange(Vessel vessel)
		{
			return (VesselUtils.GetDistanceToKerbinSurface(vessel) < VesselUtils.GetCommRange(vessel));
		}
    }
}
