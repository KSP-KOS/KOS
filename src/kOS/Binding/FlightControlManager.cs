using kOS.AddOns.RemoteTech;
using kOS.Safe.Binding;
using kOS.Safe.Utilities;
using kOS.Safe.Exceptions;
using kOS.Suffixed;
using kOS.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Math = System.Math;
using kOS.Control;
using kOS.Module;
using kOS.Communication;
using kOS.Safe.Encapsulation;

namespace kOS.Binding
{
    [Binding("ksp")]
    public class FlightControlManager : Binding
    {
        private Vessel currentVessel;
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

            shared.BindingMgr.AddBoundVariable("THROTTLE", GetThrottleValue, SetThrottleValue);
            shared.BindingMgr.AddBoundVariable("STEERING", GetSteeringValue, SetSteeringValue);
            shared.BindingMgr.AddBoundVariable("WHEELSTEERING", GetWheelSteeringValue, SetWheelSteeringValue);
            shared.BindingMgr.AddBoundVariable("WHEELTHROTTLE", GetWheelThrottleValue, SetWheelThrottleValue);

            shared.BindingMgr.AddBoundVariable("SASMODE", GetAutopilotModeName, SelectAutopilotMode);
            shared.BindingMgr.AddBoundVariable("NAVMODE", GetNavModeName, SetNavMode);

            shared.BindingMgr.AddBoundVariable("WHEELSTEERINGPID", () =>
            {
                return ((WheelSteeringManager)kOSVesselModule.GetInstance(shared.Vessel).GetFlightControlParameter("wheelsteering")).SteeringPID;
            }, value =>
            {
                if (value is PIDLoop pidLoop)
                {
                    ((WheelSteeringManager)kOSVesselModule.GetInstance(shared.Vessel).GetFlightControlParameter("wheelsteering")).SteeringPID = pidLoop;
                }
                else throw new KOSCastException(value.GetType(), typeof(PIDLoop));
            });
        }

        private object GetThrottleValue()
        {
            var throttleManager = kOSVesselModule.GetInstance(Shared.Vessel).GetFlightControlParameter("throttle"); // will throw its own exception if the parameter doesn't exist
            return throttleManager.GetValue();
        }

        private void SetThrottleValue(object val)
        {
            var throttleManager = kOSVesselModule.GetInstance(Shared.Vessel).GetFlightControlParameter("throttle");
            throttleManager.UpdateValue(val, Shared);
        }

        private object GetSteeringValue()
        {
            var steeringManager = kOSVesselModule.GetInstance(Shared.Vessel).GetFlightControlParameter("steering");
            return steeringManager.GetValue();
        }

        private void SetSteeringValue(object val)
        {
            var steeringManager = kOSVesselModule.GetInstance(Shared.Vessel).GetFlightControlParameter("steering");
            steeringManager.UpdateValue(val, Shared);
        }

        private object GetWheelSteeringValue()
        {
            var wheelSteeringManager = kOSVesselModule.GetInstance(Shared.Vessel).GetFlightControlParameter("wheelsteering");
            return wheelSteeringManager.GetValue();
        }

        private void SetWheelSteeringValue(object val)
        {
            var wheelSteeringManager = kOSVesselModule.GetInstance(Shared.Vessel).GetFlightControlParameter("wheelsteering");
            wheelSteeringManager.UpdateValue(val, Shared);
        }

        private object GetWheelThrottleValue()
        {
            var wheelThrottleManager = kOSVesselModule.GetInstance(Shared.Vessel).GetFlightControlParameter("wheelthrottle");
            return wheelThrottleManager.GetValue();
        }

        private void SetWheelThrottleValue(object val)
        {
            var wheelThrottleManager = kOSVesselModule.GetInstance(Shared.Vessel).GetFlightControlParameter("wheelthrottle");
            wheelThrottleManager.UpdateValue(val, Shared);
        }

        public void ToggleFlyByWire(string paramName, bool enabled)
        {
            SafeHouse.Logger.Log(string.Format("FlightControlManager: ToggleFlyByWire: {0} {1}", paramName, enabled));
            var param = kOSVesselModule.GetInstance(Shared.Vessel).GetFlightControlParameter(paramName); // will throw its own exception if the parameter doesn't exist
            if (enabled)
            {
                param.EnableControl(Shared);
            }
            else
            {
                param.DisableControl(Shared);
            }
        }

        public void SelectAutopilotMode(object autopilotMode)
        {
            autopilotMode = Safe.Encapsulation.Structure.FromPrimitiveWithAssert(autopilotMode);
            if ((autopilotMode is Safe.Encapsulation.StringValue))
            {
                SelectAutopilotMode(autopilotMode.ToString());
            }
            else if (autopilotMode is Direction)
            {
                //TODO: implment use of direction subclasses.
                throw new KOSException(
                    string.Format("Cannot set SAS mode to a direction. Should use the name of the mode (as string, e.g. \"PROGRADE\", not PROGRADE) for SASMODE. Alternatively, can use LOCK STEERING TO Direction instead of using SAS"));
            }
            else
            {
                throw new KOSWrongControlValueTypeException(
                  "SASMODE", KOSNomenclature.GetKOSName(autopilotMode.GetType()), "name of the SAS mode (as string)");
            }
        }

        public void SelectAutopilotMode(VesselAutopilot.AutopilotMode autopilotMode)
        {
            if (currentVessel.Autopilot.Mode != autopilotMode)
            {
                if (!currentVessel.Autopilot.CanSetMode(autopilotMode))
                {
                    // throw an exception if the mode is not available
                    throw new KOSSituationallyInvalidException(
                        string.Format("Cannot set autopilot value, pilot/probe does not support {0}, or there is no node/target", autopilotMode));
                }
                currentVessel.Autopilot.SetMode(autopilotMode);
                //currentVessel.Autopilot.Enable();
                // change the autopilot indicator
                ((kOSProcessor)Shared.Processor).SetAutopilotMode((int)autopilotMode);
                if (RemoteTechHook.IsAvailable(currentVessel.id))
                {
                    //Debug.Log(string.Format("kOS: Adding RemoteTechPilot: autopilot For : " + currentVessel.id));
                    // TODO: figure out how to make RemoteTech allow the built in autopilot control.  This may require modification to RemoteTech itself.
                }
            }
        }

        public string GetAutopilotModeName()
        {
            // TODO: As of KSP 1.1.2, RadialIn and RadialOut are still swapped.  Check if still true in future versions.
            if (currentVessel.Autopilot.Mode == VesselAutopilot.AutopilotMode.RadialOut) { return "RADIALIN"; }
            if (currentVessel.Autopilot.Mode == VesselAutopilot.AutopilotMode.RadialIn) { return "RADIALOUT"; }

            return currentVessel.Autopilot.Mode.ToString().ToUpper();
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
                        // TODO: As of KSP 1.0.4, RadialIn and RadialOut are swapped.  Check if still true in future versions.
                        SelectAutopilotMode(VesselAutopilot.AutopilotMode.RadialOut);
                        break;
                    case "radialout":
                        SelectAutopilotMode(VesselAutopilot.AutopilotMode.RadialIn);
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
                        // If the mode is not recognised, throw an exception rather than continuing or using a default setting
                        throw new KOSException(
                            string.Format("kOS does not recognize the SAS mode setting of {0}", autopilotMode));
                }
            }
        }

        public string GetNavModeName()
        {
            return GetNavMode().ToString().ToUpper();
        }


        public FlightGlobals.SpeedDisplayModes GetNavMode()
        {
            if (Shared.Vessel != FlightGlobals.ActiveVessel)
            {
                throw new KOSSituationallyInvalidException("NAVMODE can only be accessed for the Active Vessel");
            }
            return FlightGlobals.speedDisplayMode;
        }   

        public void SetNavMode(FlightGlobals.SpeedDisplayModes navMode)
        {
            FlightGlobals.SetSpeedMode(navMode);
        }

        public void SetNavMode(object navMode)
        {
            navMode = Safe.Encapsulation.Structure.FromPrimitiveWithAssert(navMode);
            if (!(navMode is Safe.Encapsulation.StringValue))
            {
                throw new KOSWrongControlValueTypeException(
                  "NAVMODE", KOSNomenclature.GetKOSName(navMode.GetType()), "string (\"ORBIT\", \"SURFACE\" or \"TARGET\")");
            }
            SetNavMode(navMode.ToString());
        }

        public void SetNavMode(string navMode)
        {
            if (Shared.Vessel != FlightGlobals.ActiveVessel)
            {
                throw new KOSSituationallyInvalidException("NAVMODE can only be accessed for the Active Vessel");
            }
            // handle a null/empty value in case of an unset command or setting to empty string to clear.
            if (string.IsNullOrEmpty(navMode))
            {
                SetNavMode(FlightGlobals.SpeedDisplayModes.Orbit);
            }
            else
            {
                // determine the navigation mode to use
                switch (navMode.ToLower())
                {
                    case "orbit":
                        SetNavMode(FlightGlobals.SpeedDisplayModes.Orbit);
                        break;
                    case "surface":
                        SetNavMode(FlightGlobals.SpeedDisplayModes.Surface);
                        break;
                    case "target":
                        if(FlightGlobals.fetch.VesselTarget== null) {
                            throw new KOSException("Cannot set navigation mode: there is no target");
                        }
                        SetNavMode(FlightGlobals.SpeedDisplayModes.Target);
                        break;
                    default:
                        // If the mode is not recognised, throw an exception rather than continuing or using a default setting
                        throw new KOSException(
                            string.Format("kOS does not recognize the navigation mode setting of {0}", navMode));
                }
            }
        }
    }
}
