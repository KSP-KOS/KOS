using System;
using System.Collections.Generic;
using UnityEngine;
using kOS.Suffixed;
using kOS.Utilities;
using kOS.Execution;

namespace kOS.Binding
{
    [kOSBinding("ksp")]
    public class FlightControlManager : Binding
    {
        private Vessel _currentVessel;
        private Dictionary<string, FlightCtrlParam> _flightParams = new Dictionary<string, FlightCtrlParam>();
        readonly static Dictionary<uint, FlightControl> flightControls = new Dictionary<uint, FlightControl>();

        public override void AddTo(SharedObjects shared)
        {
            _shared = shared;

            AddNewFlightParam("throttle", shared);
            AddNewFlightParam("steering", shared);
            AddNewFlightParam("wheelthrottle", shared);
            AddNewFlightParam("wheelsteering", shared);

            if (shared.Vessel != null)
            {
                _currentVessel = shared.Vessel;
                _currentVessel.OnFlyByWire += OnFlyByWire;
            }
        }

        private void AddNewFlightParam(string name, SharedObjects shared)
        {
            _flightParams.Add(name, new FlightCtrlParam(name, shared));
        }

        public void OnFlyByWire(FlightCtrlState c)
        {
            foreach (FlightCtrlParam param in _flightParams.Values)
            {
                if (param.enabled)
                {
                    param.OnFlyByWire(ref c);
                }
            }
        }

        public void ToggleFlyByWire(string paramName, bool enabled)
        {
            if (_flightParams.ContainsKey(paramName))
            {
                _flightParams[paramName].enabled = enabled;
                if (!enabled)
                {
                    _flightParams[paramName].ClearValue();
                }
            }
        }

        public override void Update()
        {
            UnbindUnloaded();

            if (_currentVessel != _shared.Vessel)
            {
                // Try to re-establish connection to vessel
                if (_currentVessel != null)
                {
                    _currentVessel.OnFlyByWire -= OnFlyByWire;
                    _currentVessel = null;
                }

                if (_shared.Vessel != null)
                {
                    _currentVessel = _shared.Vessel;
                    _currentVessel.OnFlyByWire += OnFlyByWire;
                }
            }
        }

        public static FlightControl GetControllerByVessel(Vessel target)
        {
            FlightControl flightControl;
            if (!flightControls.TryGetValue(target.rootPart.flightID, out flightControl))
            {
                flightControl = new FlightControl(target);
                flightControls.Add(target.rootPart.flightID, flightControl);
            }
            return flightControl;
        }

        public static void UnbindUnloaded()
        {
            var keys = flightControls.Keys;
            foreach (var key in keys)
            {
                var value = flightControls[key];
                if (value.Vessel.loaded) continue;
                flightControls.Remove(key);
                value.Dispose();
            }
        }

        private class FlightCtrlParam
        {
            public string name;
            public bool enabled;
            private SharedObjects _shared;
            private object _value;
            
            public FlightCtrlParam(string name, SharedObjects shared)
            {
                this.name = name;
                enabled = false;
                _shared = shared;
                _value = null;

                shared.BindingMgr.AddGetter(name, delegate(CPU c) { return _value; });
                shared.BindingMgr.AddSetter(name, delegate(CPU c, object val) { _value = val; });
            }

            public void ClearValue()
            {
                _value = null;
            }

            public void OnFlyByWire(ref FlightCtrlState c)
            {
                if (_value != null)
                {
                    switch (name)
                    {
                        case "throttle":
                            UpdateThrottle(c);
                            break;
                        case "wheelthrottle":
                            UpdateWheelThrottle(c);
                            break;
                        case "steering":
                            SteerByWire(c);
                            break;
                        case "wheelsteering":
                            WheelSteer(c);
                            break;
                        default:
                            break;
                    }
                }
            }

            private void UpdateThrottle(FlightCtrlState c)
            {
                c.mainThrottle = (float)Convert.ToDouble(_value);
            }

            private void UpdateWheelThrottle(FlightCtrlState c)
            {
                c.wheelThrottle = (float)Utils.Clamp(Convert.ToDouble(_value), -1, 1);                
            }

            private void SteerByWire(FlightCtrlState c)
            {
                if (_value is string && ((string)_value).ToUpper() == "KILL")
                {
                    SteeringHelper.KillRotation(c, _shared.Vessel);
                }
                else if (_value is Direction)
                {
                    SteeringHelper.SteerShipToward((Direction)_value, c, _shared.Vessel);
                }
                else if (_value is Vector)
                {
                    SteeringHelper.SteerShipToward(((Vector)_value).ToDirection(), c, _shared.Vessel);
                }
                else if (_value is Node)
                {
                    SteeringHelper.SteerShipToward(((Node)_value).GetBurnVector().ToDirection(), c, _shared.Vessel);
                }
            }

            private void WheelSteer(FlightCtrlState c)
            {
                float bearing = 0;

                if (_value is VesselTarget)
                {
                    bearing = VesselUtils.GetTargetBearing(_shared.Vessel, ((VesselTarget)_value).Vessel);
                }
                else if (_value is GeoCoordinates)
                {
                    bearing = ((GeoCoordinates)_value).GetBearing(_shared.Vessel);
                }
                else if (_value is double)
                {
                    bearing = (float)(Math.Round((double)_value) - Mathf.Round(FlightGlobals.ship_heading));
                }

                if (!(_shared.Vessel.horizontalSrfSpeed > 0.1f)) return;

                if (Mathf.Abs(VesselUtils.AngleDelta(VesselUtils.GetHeading(_shared.Vessel), VesselUtils.GetVelocityHeading(_shared.Vessel))) <= 90)
                {
                    c.wheelSteer = Mathf.Clamp(bearing / -10, -1, 1);
                }
                else
                {
                    c.wheelSteer = -Mathf.Clamp(bearing / -10, -1, 1);
                }
            }
        }
    }
}