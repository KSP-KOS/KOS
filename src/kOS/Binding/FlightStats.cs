using System.Collections.Generic;
using kOS.Module;
using kOS.Control;
using kOS.Safe.Binding;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Suffixed;
using kOS.Utilities;
using UnityEngine;
using kOS.Safe.Encapsulation;
using System.Linq;

namespace kOS.Binding
{
    [Binding("ksp")]
    public class FlightStats : Binding
    {
        private StageValues stageValue;

        public override void AddTo(SharedObjects shared)
        {
            shared.BindingMgr.AddGetter("ALT", () => new VesselAlt(shared));
            shared.BindingMgr.AddGetter("ANGULARVELOCITY", () => shared.Vessel.transform.InverseTransformDirection(shared.Vessel.GetComponent<Rigidbody>().angularVelocity));
            shared.BindingMgr.AddGetter("ENCOUNTER", () => VesselUtils.TryGetEncounter(shared.Vessel,shared));
            shared.BindingMgr.AddGetter("ETA", () => new OrbitEta(shared.Vessel.orbit, shared));  // shortcut for SHIP:ORBIT:ETA
            shared.BindingMgr.AddGetter("MISSIONTIME", () => shared.Vessel.missionTime);
            shared.BindingMgr.AddGetter(new [] { "OBT" , "ORBIT"}, () => new OrbitInfo(shared.Vessel.orbit,shared));
            // Note: "TIME" is both a bound variable AND a built-in function now.
            // While it would be cleaner to make it JUST a built -in function,
            // the bound variable had to be retained for backward compatibility with scripts
            // that call TIME without parentheses:
            shared.BindingMgr.AddGetter("TIME", () => new TimeStamp(Planetarium.GetUniversalTime()));
            shared.BindingMgr.AddGetter("ACTIVESHIP", () => VesselTarget.CreateOrGetExisting(FlightGlobals.ActiveVessel, shared));
            shared.BindingMgr.AddGetter("STATUS", () => shared.Vessel.situation.ToString());
            shared.BindingMgr.AddGetter("STAGE", () => shared.VesselTarget.StageValues);

            shared.BindingMgr.AddSetter("SHIPNAME", value => shared.Vessel.vesselName = value.ToString());

            shared.BindingMgr.AddGetter("STEERINGMANAGER", () => (SteeringManager)kOSVesselModule.GetInstance(shared.Vessel).GetFlightControlParameter("steering"));

            shared.BindingMgr.AddGetter("NEXTNODE", () =>
            {
                var vessel = shared.Vessel;
                if (vessel.patchedConicSolver == null)
                    throw new KOSSituationallyInvalidException(
                        "A KSP limitation makes it impossible to access the maneuver nodes of this vessel at this time. " +
                        "(perhaps it's not the active vessel?)");
                if (vessel.patchedConicSolver.maneuverNodes.Count == 0)
                    throw new KOSSituationallyInvalidException("No maneuver nodes present!");

                return Node.FromExisting(vessel, vessel.patchedConicSolver.maneuverNodes[0], shared);
            });
            shared.BindingMgr.AddGetter("HASNODE", () =>
            {
                var vessel = shared.Vessel;
                if (vessel.patchedConicSolver == null)
                    return false; // Since there is no solver, there can be no node.
                return vessel.patchedConicSolver.maneuverNodes.Count > 0;
            });
            shared.BindingMgr.AddGetter("ALLNODES", () => GetAllNodes(shared));
        }

        public ListValue GetAllNodes(SharedObjects shared)
        {
            var vessel = shared.Vessel;
            if (vessel.patchedConicSolver == null || vessel.patchedConicSolver.maneuverNodes.Count == 0)
                return new ListValue();
            var ret = new ListValue(vessel.patchedConicSolver.maneuverNodes.Select(e => Node.FromExisting(vessel, e, shared)));
            return ret;
        }
    }
}
