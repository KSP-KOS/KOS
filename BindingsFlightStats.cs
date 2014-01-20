using System;
using System.Linq;
using UnityEngine;
using kOS.Debug;

namespace kOS
{

    [kOSBinding("ksp")]
    public class BindingsFlightStats : Binding
    {
        public override void AddTo(BindingManager manager)
        {
            manager.AddGetter("ALT:RADAR", cpu => cpu.Vessel.heightFromTerrain > 0 ? Mathf.Min(cpu.Vessel.heightFromTerrain, (float) cpu.Vessel.altitude) : (float) cpu.Vessel.altitude);
            manager.AddGetter("ALT:APOAPSIS", cpu => cpu.Vessel.orbit.ApA);
            manager.AddGetter("ALT:PERIAPSIS", cpu => cpu.Vessel.orbit.PeA);
            manager.AddGetter("ETA:APOAPSIS", cpu => cpu.Vessel.orbit.timeToAp);
            manager.AddGetter("ETA:PERIAPSIS", cpu => cpu.Vessel.orbit.timeToPe);
            manager.AddGetter("ETA:TRANSITION", cpu => cpu.Vessel.orbit.EndUT - cpu.Vessel.missionTime);

            manager.AddGetter("MISSIONTIME", cpu => cpu.Vessel.missionTime);
            manager.AddGetter("TIME", cpu => new TimeSpan(Planetarium.GetUniversalTime()));

            manager.AddGetter("STATUS", cpu => cpu.Vessel.situation.ToString().Replace("_", " "));
            manager.AddGetter("COMMRANGE", cpu => VesselUtils.GetCommRange(cpu.Vessel));
            manager.AddGetter("INCOMMRANGE", cpu => Convert.ToDouble(CheckCommRange(cpu.Vessel)));

            manager.AddGetter("AV", cpu => cpu.Vessel.transform.InverseTransformDirection(cpu.Vessel.rigidbody.angularVelocity));
            manager.AddGetter("STAGE", cpu => new StageValues(cpu.Vessel));

            manager.AddGetter("ENCOUNTER", cpu => VesselUtils.TryGetEncounter(cpu.Vessel));

            manager.AddGetter("NEXTNODE",       delegate(CPU cpu)
            {
                var vessel = cpu.Vessel;
                if (!vessel.patchedConicSolver.maneuverNodes.Any()) { throw new KOSException("No maneuver nodes present!"); }

                return Node.FromExisting(vessel, vessel.patchedConicSolver.maneuverNodes[0]);
            });

            // Things like altitude, mass, maxthrust are now handled the same for other ships as the current ship
            manager.AddGetter("SHIP", cpu => new VesselTarget(cpu.Vessel, cpu));

            // These are now considered shortcuts to SHIP:suffix
            foreach (var scName in VesselTarget.ShortCuttableShipSuffixes)
            {
                var cName = scName;
                manager.AddGetter(scName, cpu => new VesselTarget(cpu.Vessel, cpu).GetSuffix(cName));
            }

            manager.AddSetter("VESSELNAME", delegate(CPU cpu, object value) { cpu.Vessel.vesselName = value.ToString(); });
            }

            private static float getLattitude(CPU cpu)
            {
                var retVal = (float)cpu.Vessel.latitude;

                if (retVal > 90) return 90;
                if (retVal < -90) return -90;

                return retVal;
            }

            private static float getLongitude(CPU cpu)
            {
                var retVal = (float)cpu.Vessel.longitude;

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
