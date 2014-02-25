using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Suffixed;
using kOS.Utilities;
using kOS.Execution;

namespace kOS.Bindings
{
    [kOSBinding("ksp")]
    public class MissionSettings : Binding
    {
        public override void AddTo(SharedObjects shared)
        {
            _shared = shared;

            _shared.BindingMgr.AddSetter("TARGET", delegate(CPU cpu, object val)
            {
                if (val is ITargetable)
                {
                    VesselUtils.SetTarget((ITargetable)val);
                }
                else if (val is VesselTarget)
                {
                    VesselUtils.SetTarget(((VesselTarget)val).target);
                }
                else if (val is BodyTarget)
                {
                    VesselUtils.SetTarget(((BodyTarget)val).target);
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
                        return;
                    }
                }
            });

            _shared.BindingMgr.AddGetter("TARGET", delegate(CPU cpu)
            {
                var currentTarget = FlightGlobals.fetch.VesselTarget;

                if (currentTarget is Vessel)
                {
                    return new VesselTarget((Vessel)currentTarget, _shared.Vessel);
                }
                else if (currentTarget is CelestialBody)
                {
                    return new BodyTarget((CelestialBody)currentTarget, _shared.Vessel);
                }

                return null;
            });
        }
    }
}
