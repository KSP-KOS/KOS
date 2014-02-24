using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using kOS.Suffixed;
using kOS.Utilities;
using kOS.Execution;

namespace kOS.Bindings
{
    [kOSBinding("ksp")]
    public class BindingFlightControls : Binding
    {
        private Vessel _currentVessel;
        private Dictionary<string, FlightCtrlParam> _flightParams = new Dictionary<string, FlightCtrlParam>();
        
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

                    //foreach (FlightCtrlParam param in _flightParams.Values)
                    //{
                    //    param.UpdateVessel(vessel);
                    //}
                }
            }
        }

        public class FlightCtrlParam
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
                            UpdateThrottle(ref c);
                            break;
                        case "wheelthrottle":
                            UpdateWheelThrottle(ref c);
                            break;
                        case "steering":
                            UpdateSteering(ref c);
                            break;
                        case "wheelsteering":
                            UpdateWheelSteering(ref c);
                            break;
                        default:
                            break;
                    }
                }
            }

            private void UpdateThrottle(ref FlightCtrlState c)
            {
                c.mainThrottle = (float)Convert.ToDouble(_value);
            }

            private void UpdateWheelThrottle(ref FlightCtrlState c)
            {
                c.wheelThrottle = (float)Utils.Clamp(Convert.ToDouble(_value), -1, 1);                
            }

            private void UpdateSteering(ref FlightCtrlState c)
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

            private void UpdateWheelSteering(ref FlightCtrlState c)
            {
                float bearing = 0;

                if (_value is VesselTarget)
                {
                    bearing = VesselUtils.GetTargetBearing(_shared.Vessel, ((VesselTarget)_value).target);
                }
                else if (_value is GeoCoordinates)
                {
                    bearing = ((GeoCoordinates)_value).GetBearing(_shared.Vessel);
                }
                else if (_value is double)
                {
                    bearing = (float)(Math.Round((double)_value) - Mathf.Round(FlightGlobals.ship_heading));
                }

                if (_shared.Vessel.horizontalSrfSpeed > 0.1f)
                {
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

            internal void UpdateVessel(Vessel vessel)
            {
                _shared.Vessel = vessel;
            }
        }
    }
}
