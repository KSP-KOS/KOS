using kOS.Safe.Binding;
using kOS.Suffixed;
using kOS.Suffixed.Part;
using kOS.Utilities;
using kOS.Module;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Binding
{
    [Binding("ksp")]
    public class MissionSettings : Binding
    {
        private SharedObjects sharedObj;
        private VesselTarget ship => VesselTarget.CreateOrGetExisting(sharedObj);

        public override void AddTo(SharedObjects shared)
        {
            sharedObj = shared;

            shared.BindingMgr.AddGetter("CORE", () => new Core((kOSProcessor)shared.Processor, shared));
            shared.BindingMgr.AddGetter("SHIP", () => ship);
            // These are now considered shortcuts to SHIP:suffix
            foreach (var scName in VesselTarget.ShortCuttableShipSuffixes)
            {
                shared.BindingMgr.AddGetter(scName, () => VesselShortcutGetter(scName));
            }

            shared.BindingMgr.AddSetter("TARGET", val =>
            {
                if (shared.Vessel != FlightGlobals.ActiveVessel)
                {
                    throw new kOS.Safe.Exceptions.KOSSituationallyInvalidException("TARGET can only be set for the Active Vessel");
                }
                var targetable = val as IKOSTargetable;
                if (targetable != null)
                {
                    VesselUtils.SetTarget(targetable, shared.Vessel);
                    return;
                }

                if (!string.IsNullOrEmpty(val.ToString().Trim()))
                {
                    var body = VesselUtils.GetBodyByName(val.ToString());
                    if (body != null)
                    {
                        VesselUtils.SetTarget(body, shared.Vessel);
                        return;
                    }

                    var vessel = VesselUtils.GetVesselByName(val.ToString(), shared.Vessel);
                    if (vessel != null)
                    {
                        VesselUtils.SetTarget(vessel, shared.Vessel);
                        return;
                    }
                }
                //Target not found, if we have a target we clear it
                VesselUtils.UnsetTarget();
            });

            shared.BindingMgr.AddGetter("TARGET", () =>
            {
                var target = (shared.Vessel == FlightGlobals.ActiveVessel) ? FlightGlobals.fetch.VesselTarget : shared.Vessel.targetObject;

                var vessel = target as Vessel;
                if (vessel != null)
                    return VesselTarget.CreateOrGetExisting(vessel, shared);
                var body = target as CelestialBody;
                if (body != null)
                    return BodyTarget.CreateOrGetExisting(body, shared);
                var dockingNode = target as ModuleDockingNode;
                if (dockingNode != null)
                    return VesselTarget.CreateOrGetExisting(dockingNode.vessel, shared)[dockingNode.part];

                throw new kOS.Safe.Exceptions.KOSSituationallyInvalidException("No TARGET is selected");
            });

            shared.BindingMgr.AddGetter("HASTARGET", () =>
            {
                // the ship has a target if the object does not equal null.
                return (shared.Vessel == FlightGlobals.ActiveVessel ?
                    FlightGlobals.fetch.VesselTarget : shared.Vessel.targetObject) != null;
            });

        }

        public object VesselShortcutGetter(string name)
        {
            ISuffixResult suffix = ship.GetSuffix(name);
            if (!suffix.HasValue)
                suffix.Invoke(sharedObj.Cpu);
            return suffix.Value;
        }
    }
}
