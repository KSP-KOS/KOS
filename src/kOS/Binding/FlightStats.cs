using System;
using kOS.Safe.Binding;
using UnityEngine;
using kOS.Safe.Exceptions;
using kOS.Suffixed;
using kOS.Utilities;
using kOS.Execution;
using TimeSpan = kOS.Suffixed.TimeSpan;

namespace kOS.Binding
{
    [kOSBinding("ksp")]
    public class FlightStats : Binding
    {
        public override void AddTo(SharedObjects shared)
        {

            shared.BindingMgr.AddGetter("ALT_APOAPSIS", cpu => shared.Vessel.orbit.ApA);
            shared.BindingMgr.AddGetter("ALT_PERIAPSIS", cpu => shared.Vessel.orbit.PeA);
            shared.BindingMgr.AddGetter("ALT_RADAR", cpu => Convert.ToDouble(shared.Vessel.heightFromTerrain > 0 ? Mathf.Min(shared.Vessel.heightFromTerrain, (float)shared.Vessel.altitude) : (float)shared.Vessel.altitude));
            shared.BindingMgr.AddGetter("ANGULARVELOCITY", cpu => shared.Vessel.transform.InverseTransformDirection(shared.Vessel.rigidbody.angularVelocity));
            shared.BindingMgr.AddGetter("COMMRANGE", cpu => int.MaxValue);
            shared.BindingMgr.AddGetter("ENCOUNTER", cpu => VesselUtils.TryGetEncounter(shared.Vessel,shared));
            shared.BindingMgr.AddGetter("ETA_APOAPSIS", cpu => shared.Vessel.orbit.timeToAp);
            shared.BindingMgr.AddGetter("ETA_PERIAPSIS", cpu => shared.Vessel.orbit.timeToPe);
            shared.BindingMgr.AddGetter("ETA_TRANSITION", cpu => shared.Vessel.orbit.EndUT - Planetarium.GetUniversalTime());
            shared.BindingMgr.AddGetter("INCOMMRANGE", cpu => true);
            shared.BindingMgr.AddGetter("MISSIONTIME", cpu => shared.Vessel.missionTime);
            shared.BindingMgr.AddGetter("OBT", cpu => new OrbitInfo(shared.Vessel.orbit,shared));
            shared.BindingMgr.AddGetter("TIME", cpu => new TimeSpan(Planetarium.GetUniversalTime()));
            shared.BindingMgr.AddGetter("SHIP", cpu => new VesselTarget(shared));
            shared.BindingMgr.AddGetter("ACTIVESHIP", cpu => new VesselTarget(FlightGlobals.ActiveVessel, shared));
            shared.BindingMgr.AddGetter("STATUS", cpu => shared.Vessel.situation.ToString());
            shared.BindingMgr.AddGetter("STAGE", cpu => new StageValues(shared.Vessel));

            //DEPRICATED VESSELNAME
            shared.BindingMgr.AddSetter("VESSELNAME", delegate { throw new KOSException("VESSELNAME is DEPRICATED, use SHIPNAME.");});
            shared.BindingMgr.AddSetter("SHIPNAME", delegate(CPU cpu, object value) { shared.Vessel.vesselName = value.ToString(); });

            shared.BindingMgr.AddGetter("NEXTNODE", delegate
                {
                    var vessel = shared.Vessel;
                    if (vessel.patchedConicSolver.maneuverNodes.Count == 0)
                    {
                        throw new Exception("No maneuver nodes present!");
                    }

                    return Node.FromExisting(vessel, vessel.patchedConicSolver.maneuverNodes[0], shared);
                });

            // These are now considered shortcuts to SHIP:suffix
            foreach (var scName in VesselTarget.ShortCuttableShipSuffixes)
            {
                var cName = scName;
                shared.BindingMgr.AddGetter(scName, cpu => new VesselTarget(shared).GetSuffix(cName));
            }
        }
    }
}
