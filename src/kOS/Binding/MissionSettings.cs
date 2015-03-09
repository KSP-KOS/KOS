using kOS.Safe.Binding;
using kOS.Safe.Encapsulation;
using kOS.Suffixed;
using kOS.Suffixed.Part;
using kOS.Utilities;

namespace kOS.Binding
{
    [Binding("ksp")]
    public class MissionSettings : Binding
    {
        public override void AddTo(SharedObjects shared)
        {
            shared.BindingMgr.AddSetter("TARGET", val =>
            {
                var targetable = val as IKOSTargetable;
                if (targetable != null)
                {
                    VesselUtils.SetTarget(targetable);
                    return;
                }

                var body = VesselUtils.GetBodyByName(val.ToString());
                if (body != null)
                {
                    VesselUtils.SetTarget(body);
                    return;
                }

                var vessel = VesselUtils.GetVesselByName(val.ToString(), shared.Vessel);
                if (vessel != null)
                {
                    VesselUtils.SetTarget(vessel);
                    return;
                }
                //Target not found, if we have a target we clear it
                VesselUtils.UnsetTarget();
            });

            shared.BindingMgr.AddGetter("TARGET", () =>
            {
                var currentTarget = FlightGlobals.fetch.VesselTarget;

                var vessel = currentTarget as Vessel;
                if (vessel != null)
                {
                    return new VesselTarget(vessel, shared);
                }
                var body = currentTarget as CelestialBody;
                if (body != null)
                {
                    return new BodyTarget(body, shared);
                }
                var dockingNode = currentTarget as ModuleDockingNode;
                if (dockingNode != null)
                {
                    return new DockingPortValue(dockingNode, shared);
                }

                return null;
            });
        }
    }
}