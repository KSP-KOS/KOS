using System;
using UnityEngine;
using kOS.Context;
using kOS.Debug;
using kOS.Suffixed;
using kOS.Utilities;
using TimeSpan = kOS.Suffixed.TimeSpan;

namespace kOS.Binding
{
    [KOSBinding("ksp")]
    public class FlightStats : IBinding
    {
        public void BindTo(IBindingManager manager)
        {
            manager.AddGetter("ALT:APOAPSIS", cpu => cpu.Vessel.orbit.ApA);
            manager.AddGetter("ALT:PERIAPSIS", cpu => cpu.Vessel.orbit.PeA);
            manager.AddGetter("ALT:RADAR", cpu => cpu.Vessel.heightFromTerrain > 0 ? Mathf.Min(cpu.Vessel.heightFromTerrain, (float) cpu.Vessel.altitude) : (float) cpu.Vessel.altitude);
            manager.AddGetter("ANGULARVELOCITY", cpu => cpu.Vessel.transform.InverseTransformDirection(cpu.Vessel.rigidbody.angularVelocity));
            manager.AddGetter("COMMRANGE", cpu => VesselUtils.GetCommRange(cpu.Vessel));
            manager.AddGetter("ENCOUNTER", cpu => VesselUtils.TryGetEncounter(cpu.Vessel));
            manager.AddGetter("ETA:APOAPSIS", cpu => cpu.Vessel.orbit.timeToAp);
            manager.AddGetter("ETA:PERIAPSIS", cpu => cpu.Vessel.orbit.timeToPe);
            manager.AddGetter("ETA:TRANSITION", cpu => cpu.Vessel.orbit.EndUT - cpu.Vessel.missionTime);
            manager.AddGetter("INCOMMRANGE", cpu => Convert.ToDouble(CheckCommRange(cpu.Vessel)));
            manager.AddGetter("MISSIONTIME", cpu => cpu.Vessel.missionTime);
            manager.AddGetter("OBT", cpu => new OrbitInfo(cpu.Vessel.orbit, cpu.Vessel));
            manager.AddGetter("TIME", cpu => new TimeSpan(Planetarium.GetUniversalTime()));
            manager.AddGetter("SHIP", cpu => new VesselTarget(cpu.Vessel, cpu));
            manager.AddGetter("STATUS", cpu => cpu.Vessel.situation.ToString());
            manager.AddGetter("STAGE", cpu => new StageValues(cpu.Vessel));
            manager.AddSetter("VESSELNAME", delegate(ICPU cpu, object value) { cpu.Vessel.vesselName = value.ToString(); });

            manager.AddGetter("NEXTNODE", delegate(ICPU cpu)
                {
                    var vessel = cpu.Vessel;
                    if (vessel.patchedConicSolver.maneuverNodes.Count == 0)
                    {
                        throw new KOSException("No maneuver nodes present!");
                    }

                    return Node.FromExisting(vessel, vessel.patchedConicSolver.maneuverNodes[0]);
                });

            // These are now considered shortcuts to SHIP:suffix
            foreach (var scName in VesselTarget.ShortCuttableShipSuffixes)
            {
                var cName = scName;
                manager.AddGetter(scName, cpu => new VesselTarget(cpu.Vessel, cpu).GetSuffix(cName));
            }

        }

        public void Update(float time)
        {
        }

        private static bool CheckCommRange(Vessel vessel)
        {
            return (VesselUtils.GetDistanceToHome(vessel) < VesselUtils.GetCommRange(vessel));
        }
    }
}