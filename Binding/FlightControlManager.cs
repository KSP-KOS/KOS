using System;
using System.Collections.Generic;
using kOS.AddOns.RemoteTech2;
using UnityEngine;
using kOS.Suffixed;
using kOS.Utilities;
using kOS.Execution;

namespace kOS.Binding
{
    [kOSBinding("ksp")]
    public class FlightControlManager : Binding
    {
        private Vessel currentVessel;
        private readonly Dictionary<string, FlightCtrlParam> flightParameters = new Dictionary<string, FlightCtrlParam>();
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
                currentVessel = shared.Vessel;
                currentVessel.OnFlyByWire += OnFlyByWire;
            }
        }

        public void OnFlyByWire(FlightCtrlState c)
        {
            foreach (var param in flightParameters.Values)
            {
                if (param.Enabled)
                {
                    param.OnFlyByWire(ref c);
                }
            }
        }

        public void ToggleFlyByWire(string paramName, bool enabled)
        {
            if (flightParameters.ContainsKey(paramName))
            {
                flightParameters[paramName].Enabled = enabled;
                if (!enabled)
                {
                    flightParameters[paramName].ClearValue();
                }
            }
        }

        public override void Update()
        {
            UnbindUnloaded();

            if (currentVessel != _shared.Vessel)
            {
                // Try to re-establish connection to vessel
                if (currentVessel != null)
                {
                    currentVessel.OnFlyByWire -= OnFlyByWire;
                    currentVessel = null;
                }

                if (_shared.Vessel != null)
                {
                    currentVessel = _shared.Vessel;
                    currentVessel.OnFlyByWire += OnFlyByWire;
                    if (currentVessel)
                    {
                        
                    }
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

        private void AddNewFlightParam(string name, SharedObjects shared)
        {
            flightParameters.Add(name, new FlightCtrlParam(name, GetControllerByVessel(shared.Vessel), shared.BindingMgr));
        }

        private class FlightCtrlParam
        {
            private readonly string name;
            private readonly FlightControl control;
            private object value;
            
            public FlightCtrlParam(string name, FlightControl control, BindingManager binding)
            {
                this.name = name;
                this.control = control;
                Enabled = false;
                value = null;

                binding.AddGetter(name, c => value);
                binding.AddSetter(name, delegate(CPU c, object val) { value = val; });
            }

            public bool Enabled { get; set; }

            public void ClearValue()
            {
                value = null;
            }

            public void OnFlyByWire(ref FlightCtrlState c)
            {
                if (value == null) return;

                var action = ChooseAction();
                if (action == null)
                {
                    return;
                }

                if (RemoteTechHook.IsAvailable(control.Vessel.id))
                {
                    if (Enabled)
                    {
                        RemoteTechHook.Instance.AddSanctionedPilot(control.Vessel.id, action);
                    }
                    else
                    {
                        RemoteTechHook.Instance.RemoveSanctionedPilot(control.Vessel.id, action);
                    }
                }
                else
                {
                    action.Invoke(c);
                }
            }

            private Action<FlightCtrlState> ChooseAction()
            {
                Action<FlightCtrlState> action;
                switch (name)
                {
                    case "throttle":
                        action = UpdateThrottle;
                        break;
                    case "wheelthrottle":
                        action = UpdateWheelThrottle;
                        break;
                    case "steering":
                        action = SteerByWire;
                        break;
                    case "wheelsteering":
                        action = WheelSteer;
                        break;
                    default:
                        action = null;
                        break;
                }
                return action;
            }

            private void UpdateThrottle(FlightCtrlState c)
            {
                double doubleValue = Convert.ToDouble(value);
                if (!double.IsNaN(doubleValue))
                    c.mainThrottle = (float)Utils.Clamp(doubleValue, 0, 1);
            }

            private void UpdateWheelThrottle(FlightCtrlState c)
            {
                double doubleValue = Convert.ToDouble(value);
                if (!double.IsNaN(doubleValue))
                    c.wheelThrottle = (float)Utils.Clamp(doubleValue, -1, 1);
            }

            private void SteerByWire(FlightCtrlState c)
            {
                if (value is string && ((string)value).ToUpper() == "KILL")
                {
                    SteeringHelper.KillRotation(c, control.Vessel);
                }
                else if (value is Direction)
                {
                    SteeringHelper.SteerShipToward((Direction)value, c, control.Vessel);
                }
                else if (value is Vector)
                {
                    SteeringHelper.SteerShipToward(((Vector)value).ToDirection(), c, control.Vessel);
                }
                else if (value is Node)
                {
                    SteeringHelper.SteerShipToward(((Node)value).GetBurnVector().ToDirection(), c, control.Vessel);
                }
            }

            private void WheelSteer(FlightCtrlState c)
            {
                float bearing = 0;

                if (value is VesselTarget)
                {
                    bearing = VesselUtils.GetTargetBearing(control.Vessel, ((VesselTarget)value).Vessel);
                }
                else if (value is GeoCoordinates)
                {
                    bearing = ((GeoCoordinates)value).GetBearing(control.Vessel);
                }
                else if (value is double)
                {
                    double doubleValue = Convert.ToDouble(value);
                    if (Utils.IsValidNumber(doubleValue))
                        bearing = (float)(Math.Round(doubleValue) - Mathf.Round(FlightGlobals.ship_heading));
                }

                if (!(control.Vessel.horizontalSrfSpeed > 0.1f)) return;

                if (Mathf.Abs(VesselUtils.AngleDelta(VesselUtils.GetHeading(control.Vessel), VesselUtils.GetVelocityHeading(control.Vessel))) <= 90)
                {
                    c.wheelSteer = Mathf.Clamp(bearing / -10, -1, 1);
                }
                else
                {
                    c.wheelSteer = -Mathf.Clamp(bearing / -10, -1, 1);
                }
            }
        }

        public void UnBind()
        {
            foreach (var parameter in flightParameters)
            {
                parameter.Value.Enabled = false;
            }
            FlightControl flightControl;
            if (flightControls.TryGetValue(currentVessel.rootPart.flightID, out flightControl))
            {
                flightControl.Unbind();
            }
        }
    }
}