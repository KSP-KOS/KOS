using kOS.Context;
using kOS.Suffixed;

namespace kOS.Binding
{
    [KOSBinding("ksp")]
    public class BindingTimeWarp : IBinding
    {
        public void BindTo(IBindingManager manager)
        {
            manager.AddGetter("LOADDISTANCE", cpu => Vessel.loadDistance );
            manager.AddSetter("LOADDISTANCE", delegate(ICPU cpu, object val)
                {
                    var distance = (float) val;
                    Vessel.loadDistance = distance;
                    Vessel.unloadDistance = distance - 250;
                });
            manager.AddGetter("WARP", cpu => TimeWarp.fetch.current_rate_index);
            manager.AddSetter("WARP", delegate(ICPU cpu, object val)
                {
                    int newRate;
                    if (int.TryParse(val.ToString(), out newRate))
                    {
                        TimeWarp.SetRate(newRate, false);
                    }
                });

            foreach (var body in FlightGlobals.fetch.bodies)
            {
                var cBody = body;
                manager.AddGetter(body.name, cpu => new BodyTarget(cBody, cpu.Vessel));
            }
        }

        public void Update(float time)
        {
        }
    }
}