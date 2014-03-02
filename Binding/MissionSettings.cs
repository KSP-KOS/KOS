using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Suffixed;
using kOS.Utilities;
using kOS.Execution;

namespace kOS.Binding
{
    [kOSBinding("ksp")]
    public class MissionSettings : Binding
    {
        public override void AddTo(SharedObjects shared)
        {
            _shared = shared;

            _shared.BindingMgr.AddSetter("TARGET", delegate(CPU cpu, object val)
                {
                    var targetable = val as IKOSTargetable;
                    if (targetable != null)
                    {
                        VesselUtils.SetTarget(targetable);
                    }
                    else
                    {
                        var body = VesselUtils.GetBodyByName(val.ToString());
                        if (body != null)
                        {
                            VesselUtils.SetTarget(body);
                            return;
                        }

                        var vessel = VesselUtils.GetVesselByName(val.ToString(), _shared.Vessel);
                        if (vessel != null)
                        {
                            VesselUtils.SetTarget(vessel);
                        }
                    }
                });

            _shared.BindingMgr.AddGetter("TARGET", delegate(CPU cpu)
            {
                var currentTarget = FlightGlobals.fetch.VesselTarget;

                    var vessel = currentTarget as Vessel;
                    if (vessel != null)
                    {
                        return new VesselTarget(vessel, _shared.Vessel);
                    }
                    var body = currentTarget as CelestialBody;
                    if (body != null)
                    {
                        return new BodyTarget(body, _shared.Vessel);
                    }

                return null;
            });
        }
    }
}
