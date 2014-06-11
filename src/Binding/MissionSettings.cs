using kOS.Execution;
using kOS.Suffixed;
using kOS.Suffixed.Part;
using kOS.Utilities;

namespace kOS.Binding
{
    [kOSBinding("ksp")]
    public class MissionSettings : Binding
    {
        public override void AddTo(SharedObjects shared)
        {
            Shared = shared;

            Shared.BindingMgr.AddSetter("TARGET", delegate(CPU cpu, object val)
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

                    var vessel = VesselUtils.GetVesselByName(val.ToString(), Shared.Vessel);
                    if (vessel != null)
                    {
                        VesselUtils.SetTarget(vessel);
                        return;
                    }
                    //Target not found, if we have a target we clear it
                    VesselUtils.UnsetTarget();
                });

            Shared.BindingMgr.AddGetter("TARGET", delegate
                {
                    var currentTarget = FlightGlobals.fetch.VesselTarget;

                    var vessel = currentTarget as Vessel;
                    if (vessel != null)
                    {
                        return new VesselTarget(vessel, Shared);
                    }
                    var body = currentTarget as CelestialBody;
                    if (body != null)
                    {
                        return new BodyTarget(body, Shared);
                    }
                    var dockingNode = currentTarget as ModuleDockingNode;
                    if (dockingNode != null)
                    {
                        return new DockingPortValue(dockingNode, _shared);
                    }

                    return null;
                });
        }
    }
}