using System;
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
            _shared.BindingMgr.AddGetter("WARPMODE", cpu =>
                {
                    switch (TimeWarp.WarpMode)
                    {
                        case TimeWarp.Modes.HIGH:
                            return "RAILS";
                        case TimeWarp.Modes.LOW:
                            return "PHYSICS";
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                });
            _shared.BindingMgr.AddSetter("WARPMODE", (cpu, val) =>
                {
                    TimeWarp.Modes toSet;

                    switch (val.ToString())
                    {
                        case "PHYSICS":
                            toSet = TimeWarp.Modes.LOW;
                            break;
                        case "RAILS":
                            toSet = TimeWarp.Modes.HIGH;
                            break;
                        default:
                            throw new Exception(string.Format("WARPMODE '{0}' is not valid", val));
                    }

                    TimeWarp.fetch.Mode = toSet;
                });
            _shared.BindingMgr.AddGetter("WARP", cpu => TimeWarp.CurrentRateIndex);
            _shared.BindingMgr.AddSetter("WARP", delegate(CPU cpu, object val)
                {
                    int newRate;
                    if (int.TryParse(val.ToString(), out newRate))
                    {
                        TimeWarp.SetRate(newRate, false);
                    }
                });
            _shared.BindingMgr.AddGetter("MAPVIEW", cpu => MapView.MapIsEnabled);
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
                _shared.BindingMgr.AddGetter(body.name, cpu => new BodyTarget(cBody, _shared));
            }
        }
    }
}
