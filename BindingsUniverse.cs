using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS
{
    [kRISCBinding("ksp")]
    public class BindingTimeWarp : Binding
    {
        public override void AddTo(SharedObjects shared)
        {
            _shared = shared;

            _shared.BindingMgr.AddGetter("WARP", delegate(CPU cpu) { return TimeWarp.fetch.current_rate_index; });
            _shared.BindingMgr.AddSetter("WARP", delegate(CPU cpu, object val)
            {
                int newRate;
                if (int.TryParse(val.ToString(), out newRate))
                {
                    TimeWarp.SetRate(newRate, false);
                }
            });

            foreach (CelestialBody body in FlightGlobals.fetch.bodies)
            {
                _shared.BindingMgr.AddGetter(body.name, delegate(CPU cpu) { return new BodyTarget(body, _shared.Vessel); });
            }
        }
    }
}
