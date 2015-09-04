using kOS.AddOns.RemoteTech;
using kOS.Safe.Binding;
using kOS.Safe.Utilities;
using kOS.Suffixed;
using kOS.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Math = System.Math;

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
                SafeHouse.Logger.LogWarning("FlightControlManager.AddTo Skipped: shared.Vessel== null");
                return;
            }

            if (Shared.Vessel.rootPart == null)
            {
                SafeHouse.Logger.LogWarning("FlightControlManager.AddTo Skipped: shared.Vessel.rootPart == null");
                return;
            }

            SafeHouse.Logger.Log("FlightControlManager.AddTo " + Shared.Vessel.id);

            currentVessel = shared.Vessel;
            currentVessel.OnPreAutopilotUpdate += OnFlyByWire;

            AddNewFlightParam("throttle", Shared);
            AddNewFlightParam("steering", Shared);
            AddNewFlightParam("wheelthrottle", Shared);
            AddNewFlightParam("wheelsteering", Shared);

            shared.BindingMgr.AddSetter("SASMODE", value => SelectAutopilotMode((string)value));
            shared.BindingMgr.AddGetter("SASMODE", () => currentVessel.Autopilot.Mode.ToString().ToUpper());
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
            SafeHouse.Logger.Log(string.Format("FlightControlManager: ToggleFlyByWire: {0} {1}", paramName, enabled));
            if (!flightParameters.ContainsKey(paramName.ToLower())) { Debug.LogError("no such flybywire parameter " + paramName); return; }

            flightParameters[paramName.ToLower()].Enabled = enabled;

            if (!enabled)
            {
                flightParameters[paramName.ToLower()].ClearValue();
            }
        }

        public override void Update()
        {
            UnbindUnloaded();

            // Why the "currentVessel != null checks?
            //   Because of a timing issue where it can still be set to null during the span of one single
            //   update if the new vessel isn't valid and set up yet when the old vessel connection got
            //   broken off.
            //
            if (currentVessel != null && currentVessel.id == Shared.Vessel.id) return;

            // If it gets this far, that means the part the kOSProcessor module is inside of
            // got disconnected from its original vessel and became a member
            // of a new child vessel, either due to undocking, decoupling, or breakage.

            // currentVessel is now a stale reference to the vessel this manager used to be a member of,
            // while Shared.Vessel is the new vessel it is now contained in.

            // Before updating currentVessel, use it to break connection from the old vessel,
            // so this this stops trying to pilot the vessel it's not attached to anymore:
            if (currentVessel != null && VesselIsValid(currentVessel))
            {
                currentVessel.OnPreAutopilotUpdate -= OnFlyByWire;
                currentVessel = null;
            }

            // If the new vessel isn't ready for it, then don't attach to it yet (wait for a future update):
            if (! VesselIsValid(Shared.Vessel)) return;
            
            // Now attach to the new vessel:
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
                SafeHouse.Logger.Log("Unloading " + value.Vessel.vesselName);
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
            SteeringManager.RemoveInstance(currentVessel.id);
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

        public void SelectAutopilotMode(object autopilotMode)
        {
            if (autopilotMode is Direction)
            {
                //TODO: implment use of direction subclasses.
            }
            else SelectAutopilotMode((string)autopilotMode);
        }

        public void SelectAutopilotMode(VesselAutopilot.AutopilotMode autopilotMode)
        {
            if (currentVessel.Autopilot.Mode != autopilotMode)
            {
                if (!currentVessel.Autopilot.CanSetMode(autopilotMode))
                {
                    // throw an exception if the mode is not available
                    throw new Safe.Exceptions.KOSException(
                        string.Format("Cannot set autopilot value, pilot/probe does not support {0}, or there is no node/target", autopilotMode));
                }
                currentVessel.Autopilot.SetMode(autopilotMode);
                //currentVessel.Autopilot.Enable();
                // change the autopilot indicator
                ((Module.kOSProcessor)Shared.Processor).SetAutopilotMode((int)autopilotMode);
                if (RemoteTechHook.IsAvailable(currentVessel.id))
                {
                    Debug.Log(string.Format("kOS: Adding RemoteTechPilot: autopilot For : " + currentVessel.id));
                    // TODO: figure out how to make RemoteTech allow the built in autopilot control.  This may require modification to RemoteTech itself.
                }
            }
        }

        public void SelectAutopilotMode(string autopilotMode)
        {
            // handle a null/empty value in case of an unset command or setting to empty string to clear.
            if (string.IsNullOrEmpty(autopilotMode))
            {
                SelectAutopilotMode(VesselAutopilot.AutopilotMode.StabilityAssist);
            }
            else
            {
                // determine the AutopilotMode to use
                switch (autopilotMode.ToLower())
                {
                    case "maneuver":
                        SelectAutopilotMode(VesselAutopilot.AutopilotMode.Maneuver);
                        break;
                    case "prograde":
                        SelectAutopilotMode(VesselAutopilot.AutopilotMode.Prograde);
                        break;
                    case "retrograde":
                        SelectAutopilotMode(VesselAutopilot.AutopilotMode.Retrograde);
                        break;
                    case "normal":
                        SelectAutopilotMode(VesselAutopilot.AutopilotMode.Normal);
                        break;
                    case "antinormal":
                        SelectAutopilotMode(VesselAutopilot.AutopilotMode.Antinormal);
                        break;
                    case "radialin":
                        SelectAutopilotMode(VesselAutopilot.AutopilotMode.RadialIn);
                        break;
                    case "radialout":
                        SelectAutopilotMode(VesselAutopilot.AutopilotMode.RadialOut);
                        break;
                    case "target":
                        SelectAutopilotMode(VesselAutopilot.AutopilotMode.Target);
                        break;
                    case "antitarget":
                        SelectAutopilotMode(VesselAutopilot.AutopilotMode.AntiTarget);
                        break;
                    case "stability":
                    case "stabilityassist":
                        SelectAutopilotMode(VesselAutopilot.AutopilotMode.StabilityAssist);
                        break;
                    default:
                        // If the mode is not recognised, thrown an exception rather than continuing or using a default setting
                        throw new Safe.Exceptions.KOSException(
                            string.Format("kOS does not recognize the SAS mode setting of {0}", autopilotMode));
                }
            }
        }

        private class FlightCtrlParam : IDisposable
        {
            private readonly string name;
            private FlightControl control;
            private readonly BindingManager binding;
            private object value;
            private bool enabled;
            SharedObjects shared;
            SteeringManager steeringManager;

            public FlightCtrlParam(string name, SharedObjects sharedObjects)
            {
                this.name = name;
                shared = sharedObjects;
                control = GetControllerByVessel(sharedObjects.Vessel);
                
                binding = sharedObjects.BindingMgr;
                Enabled = false;
                value = null;

                if (string.Equals(name, "steering", StringComparison.CurrentCultureIgnoreCase))
                {
                    steeringManager = SteeringManager.GetInstance(sharedObjects);
                }

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
                    SafeHouse.Logger.Log(string.Format("FlightCtrlParam: Enabled: {0} {1} => {2}", name, enabled, value));

                    enabled = value;
                    if (steeringManager != null)
                    {
                        if (enabled) steeringManager.EnableControl(this.shared);
                        else steeringManager.DisableControl();
                        //steeringManager.Enabled = enabled;
                    }
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
                    SafeHouse.Logger.Log(string.Format("Adding RemoteTechPilot: " + name + " For : " + control.Vessel.id));
                    RemoteTechHook.Instance.AddSanctionedPilot(control.Vessel.id, action);
                }
                else
                {
                    SafeHouse.Logger.Log(string.Format("Removing RemoteTechPilot: " + name + " For : " + control.Vessel.id));
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
                steeringManager.Value = this.value;
                steeringManager.OnFlyByWire(c);
                //if (value is string && ((string)value).ToUpper() == "KILL")
                //{
                //    SteeringHelper.KillRotation(c, control.Vessel);
                //}
                //else if (value is Direction)
                //{
                //    SteeringHelper.SteerShipToward((Direction)value, c, control.Vessel);
                //}
                //else if (value is Vector)
                //{
                //    //SteeringHelper.SteerShipToward(((Vector)value).ToDirection(), c, control.Vessel);
                //    SteeringHelper.SteerShipToward(
                //        Direction.LookRotation((Vector)value, control.Vessel.mainBody.position - control.Vessel.GetWorldPos3D()), 
                //        c, control.Vessel);
                //}
                //else if (value is Node)
                //{
                //    SteeringHelper.SteerShipToward(((Node)value).GetBurnVector().ToDirection(), c, control.Vessel);
                //}
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
                if (steeringManager != null)
                {
                    steeringManager.RemoveInstance(shared);
                    steeringManager = null;
                }
            }

            public void UpdateFlightControl(Vessel vessel)
            {
                control = GetControllerByVessel(vessel);
                if (steeringManager != null)
                {
                    steeringManager = SteeringManager.SwapInstance(shared, steeringManager);
                    steeringManager.Update(vessel);
                }
            }
            
            public override string ToString() // added to aid in debugging.
            {
                return "FlightCtrlParam: name="+name+" enabled="+Enabled;
            }
 
        }
    }
}