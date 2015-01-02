using System;
using kOS.Safe.Binding;
using UnityEngine;
using kOS.Safe.Exceptions;
using kOS.Suffixed;
using kOS.Utilities;
using TimeSpan = kOS.Suffixed.TimeSpan;

namespace kOS.Binding
{
    [Binding("ksp")]
    public class FlightStats : Binding
    {
        public override void AddTo(SharedObjects shared)
        {
            shared.BindingMgr.AddGetter("ALT_APOAPSIS", () => shared.Vessel.orbit.ApA);
            shared.BindingMgr.AddGetter("ALT_PERIAPSIS", () => shared.Vessel.orbit.PeA);
            shared.BindingMgr.AddGetter("ALT_RADAR", () => Convert.ToDouble(shared.Vessel.heightFromTerrain > 0 ? Mathf.Min(shared.Vessel.heightFromTerrain, (float)shared.Vessel.altitude) : (float)shared.Vessel.altitude));
            shared.BindingMgr.AddGetter("ANGULARVELOCITY", () => shared.Vessel.transform.InverseTransformDirection(shared.Vessel.rigidbody.angularVelocity));
            shared.BindingMgr.AddGetter("COMMRANGE", () => int.MaxValue);
            shared.BindingMgr.AddGetter("ENCOUNTER", () => VesselUtils.TryGetEncounter(shared.Vessel,shared));
            shared.BindingMgr.AddGetter("ETA_APOAPSIS", () => shared.Vessel.orbit.timeToAp);
            shared.BindingMgr.AddGetter("ETA_PERIAPSIS", () => shared.Vessel.orbit.timeToPe);
            shared.BindingMgr.AddGetter("ETA_TRANSITION", () => shared.Vessel.orbit.EndUT - Planetarium.GetUniversalTime());
            shared.BindingMgr.AddGetter("INCOMMRANGE", () => true);
            shared.BindingMgr.AddGetter("MISSIONTIME", () => shared.Vessel.missionTime);
            shared.BindingMgr.AddGetter("OBT", () => new OrbitInfo(shared.Vessel.orbit,shared));
            shared.BindingMgr.AddGetter("TIME", () => new TimeSpan(Planetarium.GetUniversalTime()));
            shared.BindingMgr.AddGetter("SHIP", () => new VesselTarget(shared));
            shared.BindingMgr.AddGetter("ACTIVESHIP", () => new VesselTarget(FlightGlobals.ActiveVessel, shared));
            shared.BindingMgr.AddGetter("STATUS", () => shared.Vessel.situation.ToString());
            shared.BindingMgr.AddGetter("STAGE", () => new StageValues(shared));

            //DEPRICATED VESSELNAME
            shared.BindingMgr.AddSetter("VESSELNAME",
                val => { throw new KOSException("VESSELNAME is DEPRICATED, use SHIPNAME."); });
            shared.BindingMgr.AddSetter("SHIPNAME", value => shared.Vessel.vesselName = value.ToString());

            shared.BindingMgr.AddGetter("NEXTNODE", () =>
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
                shared.BindingMgr.AddGetter(scName, () => new VesselTarget(shared).GetSuffix(cName));
            }
        }
    }
}
