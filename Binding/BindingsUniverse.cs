using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Suffixed;
using kOS.Execution;

namespace kOS.Binding
{
    [kOSBinding("ksp")]
    public class BindingTimeWarp : Binding
    {
        public override void AddTo(SharedObjects shared)
        {
            _shared = shared;

            _shared.BindingMgr.AddGetter("LOADDISTANCE", cpu => Vessel.loadDistance );
            _shared.BindingMgr.AddSetter("LOADDISTANCE", delegate(CPU cpu, object val)
                {
                    var distance = (float) val;
                    Vessel.loadDistance = distance;
                    Vessel.unloadDistance = distance - 250;
                });
            _shared.BindingMgr.AddGetter("WARP", cpu => TimeWarp.fetch.current_rate_index);
            _shared.BindingMgr.AddSetter("WARP", delegate(CPU cpu, object val)
                {
                    int newRate;
                    if (int.TryParse(val.ToString(), out newRate))
                    {
                        TimeWarp.SetRate(newRate, false);
                    }
                });
            _shared.BindingMgr.AddGetter("MAPVIEW", delegate(CPU cpu) { return MapView.MapIsEnabled; } );
            _shared.BindingMgr.AddSetter("MAPVIEW", delegate(CPU cpu, object val)
                {
                    if( Convert.ToBoolean( val ) )
                    {
                        MapView.EnterMapView();
                    }
                    else
                    {
                        MapView.ExitMapView();
                    }
                });
            foreach (var body in FlightGlobals.fetch.bodies)
            {
                var cBody = body;
                _shared.BindingMgr.AddGetter(body.name, cpu => new BodyTarget(cBody, _shared.Vessel));
            }
        }
    }
}
