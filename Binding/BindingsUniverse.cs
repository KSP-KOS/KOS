using kOS.Context;
using kOS.Value;

namespace kOS.Binding
{
    [KOSBinding("ksp")]
    public class BindingTimeWarp : Binding
    {
        public override void AddTo(BindingManager manager)
        {
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
                manager.AddGetter(body.name, cpu => new BodyTarget(cBody, cpu));
            }
        }
    }
}
