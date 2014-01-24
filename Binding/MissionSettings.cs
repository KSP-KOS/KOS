using kOS.Context;
using kOS.Suffixed;
using kOS.Utilities;

namespace kOS.Binding
{
    [KOSBinding("ksp")]
    public class MissionSettings : IBinding
    {
        public void BindTo(IBindingManager manager)
        {
            manager.AddSetter("TARGET", delegate(ICPU cpu, object val) 
                {
                    if (val is ITargetable)
                    {
                        VesselUtils.SetTarget((ITargetable)val);
                    }
                    else if (val is VesselTarget)
                    {
                        VesselUtils.SetTarget(((VesselTarget)val).Target);
                    }
                    else if (val is BodyTarget)
                    {
                        VesselUtils.SetTarget(((BodyTarget)val).Target);
                    }
                    else
                    {
                        var body = VesselUtils.GetBodyByName(val.ToString());
                        if (body != null)
                        {
                            VesselUtils.SetTarget(body);
                            return;
                        }

                        var vessel = VesselUtils.GetVesselByName(val.ToString(), cpu.Vessel);
                        if (vessel != null)
                        {
                            VesselUtils.SetTarget(vessel);
                        }
                    }
                });

            manager.AddGetter("TARGET", delegate(ICPU cpu) 
                {
                    var currentTarget = FlightGlobals.fetch.VesselTarget;

                    if (currentTarget is Vessel)
                    {
                        return new VesselTarget((Vessel)currentTarget, cpu);
                    }
                    if (currentTarget is CelestialBody)
                    {
                        return new BodyTarget((CelestialBody)currentTarget, cpu.Vessel);
                    }

                    return null;
                });
        }

        public void Update(float time)
        {
        }
    }
}