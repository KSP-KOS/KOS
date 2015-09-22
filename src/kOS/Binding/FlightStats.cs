using kOS.Safe.Binding;
using kOS.Safe.Exceptions;
using kOS.Suffixed;
using kOS.Utilities;

namespace kOS.Binding
{
    [Binding("ksp")]
    public class FlightStats : Binding
    {
        public override void AddTo(SharedObjects shared)
        {
            shared.BindingMgr.AddGetter("ALT", () => new VesselAlt(shared));
            shared.BindingMgr.AddGetter("ANGULARVELOCITY", () => shared.Vessel.transform.InverseTransformDirection(shared.Vessel.rigidbody.angularVelocity));
            shared.BindingMgr.AddGetter("COMMRANGE", () => int.MaxValue);
            shared.BindingMgr.AddGetter("ENCOUNTER", () => VesselUtils.TryGetEncounter(shared.Vessel,shared));
            shared.BindingMgr.AddGetter("ETA", () => new VesselEta(shared));
            shared.BindingMgr.AddGetter("INCOMMRANGE", () => { throw new KOSDeprecationException("0.17.0", "INCOMMRANGE", "ADDONS:RT:HASCONNECTION(VESSEL)", @"http://ksp-kos.github.io/KOS_DOC/addons/RemoteTech.html"); });
            shared.BindingMgr.AddGetter("MISSIONTIME", () => shared.Vessel.missionTime);
            shared.BindingMgr.AddGetter(new [] { "OBT" , "ORBIT"}, () => new OrbitInfo(shared.Vessel.orbit,shared));
            shared.BindingMgr.AddGetter("TIME", () => new TimeSpan(Planetarium.GetUniversalTime()));
            shared.BindingMgr.AddGetter("SHIP", () => new VesselTarget(shared));
            shared.BindingMgr.AddGetter("ACTIVESHIP", () => new VesselTarget(FlightGlobals.ActiveVessel, shared));
            shared.BindingMgr.AddGetter("STATUS", () => shared.Vessel.situation.ToString());
            shared.BindingMgr.AddGetter("STAGE", () => new StageValues(shared));

            shared.BindingMgr.AddSetter("SHIPNAME", value => shared.Vessel.vesselName = value.ToString());

            shared.BindingMgr.AddGetter("NEXTNODE", () =>
            {
                var vessel = shared.Vessel;
                if (vessel.patchedConicSolver == null)
                    throw new KOSSituationallyInvalidException(
                        "A KSP limitation makes it impossible to access the manuever nodes of this vessel at this time. " +
                        "(perhaps it's not the active vessel?)");
                if (vessel.patchedConicSolver.maneuverNodes.Count == 0)
                    throw new KOSSituationallyInvalidException("No maneuver nodes present!");

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
