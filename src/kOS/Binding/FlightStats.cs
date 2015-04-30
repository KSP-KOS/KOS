using System;
using kOS.Safe.Binding;
using UnityEngine;
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
            shared.BindingMgr.AddGetter("ALT", () => new Alt(shared));
            shared.BindingMgr.AddGetter("ALT_APOAPSIS", () => (new Alt(shared)).GetApoapais());
            shared.BindingMgr.AddGetter("ALT_PERIAPSIS", () => (new Alt(shared)).GetPeriapsis());
            shared.BindingMgr.AddGetter("ALT_RADAR", () => (new Alt(shared)).GetRadar());
            shared.BindingMgr.AddGetter("ANGULARVELOCITY", () => shared.Vessel.transform.InverseTransformDirection(shared.Vessel.rigidbody.angularVelocity));
            shared.BindingMgr.AddGetter("COMMRANGE", () => int.MaxValue);
            shared.BindingMgr.AddGetter("ENCOUNTER", () => VesselUtils.TryGetEncounter(shared.Vessel,shared));
            shared.BindingMgr.AddGetter("ETA", () => new Eta(shared));
            shared.BindingMgr.AddGetter("ETA_APOAPSIS", () => (new Eta(shared)).GetApoapsis());
            shared.BindingMgr.AddGetter("ETA_PERIAPSIS", () => (new Eta(shared)).GetPeriapsis());
            shared.BindingMgr.AddGetter("ETA_TRANSITION", () => (new Eta(shared)).GetTransition());
            shared.BindingMgr.AddGetter("INCOMMRANGE", () => { throw new kOS.Safe.Exceptions.KOSDeprecationException("0.17.0", "INCOMMRANGE", "RTADDON:HASCONNECTION(VESSEL)", @"http://ksp-kos.github.io/KOS_DOC/addons/RemoteTech.html"); });
            shared.BindingMgr.AddGetter("MISSIONTIME", () => shared.Vessel.missionTime);
            shared.BindingMgr.AddGetter("OBT", () => new OrbitInfo(shared.Vessel.orbit,shared));
            shared.BindingMgr.AddGetter("TIME", () => new TimeSpan(Planetarium.GetUniversalTime()));
            shared.BindingMgr.AddGetter("SHIP", () => new VesselTarget(shared));
            shared.BindingMgr.AddGetter("ACTIVESHIP", () => new VesselTarget(FlightGlobals.ActiveVessel, shared));
            shared.BindingMgr.AddGetter("STATUS", () => shared.Vessel.situation.ToString());
            shared.BindingMgr.AddGetter("STAGE", () => new StageValues(shared));

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
