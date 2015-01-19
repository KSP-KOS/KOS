using kOS.AddOns.RemoteTech2;
using kOS.Safe.Binding;
using kOS.Suffixed;
using kOS.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace kOS.Binding
{
    [Binding("ksp")]
    public class FlightControlManager : Binding , IDisposable
    {
        private Vessel currentVessel;
        private readonly Dictionary<string, FlightCtrlParam> flightParameters = new Dictionary<string, FlightCtrlParam>();
        private static readonly Dictionary<uint, FlightControl> flightControls = new Dictionary<uint, FlightControl>();
        public SharedObjects Shared { get; set; }

        public override void AddTo(SharedObjects shared)
        {
            Shared = shared;

            if (Shared.Vessel == null)
            {
                Debug.LogWarning("kOS: FlightControlManager.AddTo Skipped: shared.Vessel== null");
                return;
            }

            if (Shared.Vessel.rootPart == null)
            {
                Debug.LogWarning("kOS: FlightControlManager.AddTo Skipped: shared.Vessel.rootPart == null");
                return;
            }

            Debug.Log("kOS: FlightControlManager.AddTo " + Shared.Vessel.id);

            currentVessel = shared.Vessel;
            currentVessel.OnPreAutopilotUpdate += OnFlyByWire;

            AddNewFlightParam("throttle", Shared);
            AddNewFlightParam("steering", Shared);
            AddNewFlightParam("wheelthrottle", Shared);
            AddNewFlightParam("wheelsteering", Shared);
        }


        private void OnFlyByWire(FlightCtrlState c)
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
            Debug.Log(string.Format("kOS: FlightControlManager: ToggleFlyByWire: {0} {1}", paramName, enabled));
            if (!flightParameters.ContainsKey(paramName.ToLower())) { UnityEngine.Debug.LogError("kOS: no such flybywire parameter " + paramName); return; }

            flightParameters[paramName.ToLower()].Enabled = enabled;

            if (!enabled)
            {
                flightParameters[paramName.ToLower()].ClearValue();
            }
        }

        public override void Update()
        {
            UnbindUnloaded();

            if (currentVessel.id == Shared.Vessel.id) return;

            // Try to re-establish connection to vessel
            if (VesselIsValid(currentVessel))
            {
                currentVessel.OnPreAutopilotUpdate -= OnFlyByWire;
                currentVessel = null;
            }

            if (VesselIsValid(Shared.Vessel)) return;

            currentVessel = Shared.Vessel;
            currentVessel.OnPreAutopilotUpdate += OnFlyByWire;

            foreach (var param in flightParameters.Values)
                param.UpdateFlightControl(currentVessel);
        }

        public static FlightControl GetControllerByVessel(Vessel target)
        {
            FlightControl flightControl;
            if (!flightControls.TryGetValue(target.rootPart.flightID, out flightControl))
            {
                flightControl = new FlightControl(target);
                flightControls.Add(target.rootPart.flightID, flightControl);
            }

            if (flightControl.Vessel == null)
                flightControl.UpdateVessel(target);

            return flightControl;
        }

        private static void UnbindUnloaded()
        {
            var toRemove = new List<uint>();
            foreach (var key in flightControls.Keys)
            {
                var value = flightControls[key];
                if (value.Vessel.loaded) continue;
                Debug.Log("kOS: Unloading " + value.Vessel.vesselName);
                toRemove.Add(key);
                value.Dispose();
            }

            foreach (var key in toRemove)
            {
                flightControls.Remove(key);
            }
        }

        private void AddNewFlightParam(string name, SharedObjects shared)
        {
            flightParameters.Add(name, new FlightCtrlParam(name, shared));
        }

        public void UnBind()
        {
            foreach (var parameter in flightParameters)
            {
                parameter.Value.Enabled = false;
            }
            if (!VesselIsValid(currentVessel)) return;

            FlightControl flightControl;
            if (flightControls.TryGetValue(currentVessel.rootPart.flightID, out flightControl))
            {
                flightControl.Unbind();
            }
        }

        public void Dispose()
        {
            flightParameters.Clear();
            if (!VesselIsValid(currentVessel)) return;

            UnBind();
            flightControls.Remove(currentVessel.rootPart.flightID);
        }

        private bool VesselIsValid(Vessel vessel)
        {
            return vessel != null && vessel.rootPart != null;
        }

        private class FlightCtrlParam : IDisposable
        {
            private readonly string name;
            private FlightControl control;
            private readonly BindingManager binding;
            private object value;
            private bool enabled;

            public FlightCtrlParam(string name, SharedObjects sharedObjects)
            {
                this.name = name;
                control = GetControllerByVessel(sharedObjects.Vessel);
                
                binding = sharedObjects.BindingMgr;
                Enabled = false;
                value = null;

                HookEvents();
            }

            private void HookEvents()
            {
                binding.AddGetter(name, () => value);
                binding.AddSetter(name, val => value = val);
            }


            public bool Enabled
            {
                get { return enabled; }
                set
                {
                    Debug.Log(string.Format("kOS: FlightCtrlParam: Enabled: {0} {1} => {2}", name, enabled, value));

                    enabled = value;
                    if (RemoteTechHook.IsAvailable(control.Vessel.id))
                    {
                        HandleRemoteTechPilot();
                    }
                }
            }

            private void HandleRemoteTechPilot()
            {
                var action = ChooseAction();
                if (action == null)
                {
                    return;
                }
                if (Enabled)
                {
                    Debug.Log(string.Format("kOS: Adding RemoteTechPilot: " + name + " For : " + control.Vessel.id));
                    RemoteTechHook.Instance.AddSanctionedPilot(control.Vessel.id, action);
                }
                else
                {
                    Debug.Log(string.Format("kOS: Removing RemoteTechPilot: " + name + " For : " + control.Vessel.id));
                    RemoteTechHook.Instance.RemoveSanctionedPilot(control.Vessel.id, action);
                }
            }

            public void ClearValue()
            {
                value = null;
            }

            public void OnFlyByWire(ref FlightCtrlState c)
            {
                if (value == null || !Enabled) return;

                var action = ChooseAction();
                if (action == null)
                {
                    return;
                }

                if (!RemoteTechHook.IsAvailable(control.Vessel.id))
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
                if (!Enabled) return;
                double doubleValue = Convert.ToDouble(value);
                if (!double.IsNaN(doubleValue))
                    c.mainThrottle = (float)Safe.Utilities.Math.Clamp(doubleValue, 0, 1);
            }

            private void UpdateWheelThrottle(FlightCtrlState c)
            {
                if (!Enabled) return;
                double doubleValue = Convert.ToDouble(value);
                if (!double.IsNaN(doubleValue))
                    c.wheelThrottle = (float)Safe.Utilities.Math.Clamp(doubleValue, -1, 1);
            }

            private void SteerByWire(FlightCtrlState c)
            {
                if (!Enabled) return;
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
                if (!Enabled) return;
                float bearing = 0;

                if (value is VesselTarget)
                {
                    bearing = VesselUtils.GetTargetBearing(control.Vessel, ((VesselTarget)value).Vessel);
                }
                else if (value is GeoCoordinates)
                {
                    bearing = (float) ((GeoCoordinates)value).GetBearing();
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

            public void Dispose()
            {
                Enabled = false;
            }

            public void UpdateFlightControl(Vessel vessel)
            {
                control = GetControllerByVessel(vessel);
            }
        }
    }
}