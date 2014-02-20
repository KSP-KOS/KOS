using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace kOS
{
    [kRISCBinding("ksp")]
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

    [kRISCBinding("ksp")]
    public class BindingActionGroups : Binding
    {
        public override void AddTo(SharedObjects shared)
        {
            _shared = shared;

            _shared.BindingMgr.AddSetter("SAS", delegate(CPU cpu, object val) { _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, (bool)val); });
            _shared.BindingMgr.AddSetter("GEAR", delegate(CPU cpu, object val) { _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Gear, (bool)val); });
            _shared.BindingMgr.AddSetter("LEGS", delegate(CPU cpu, object val) { VesselUtils.LandingLegsCtrl(_shared.Vessel, (bool)val); });
            _shared.BindingMgr.AddSetter("CHUTES", delegate(CPU cpu, object val) { VesselUtils.DeployParachutes(_shared.Vessel, (bool)val); });
            _shared.BindingMgr.AddSetter("LIGHTS", delegate(CPU cpu, object val) { _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Light, (bool)val); });
            _shared.BindingMgr.AddSetter("PANELS", delegate(CPU cpu, object val) { VesselUtils.SolarPanelCtrl(_shared.Vessel, (bool)val); });
            _shared.BindingMgr.AddSetter("BRAKES", delegate(CPU cpu, object val) { _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, (bool)val); });
            _shared.BindingMgr.AddSetter("RCS", delegate(CPU cpu, object val) { _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, (bool)val); });
            _shared.BindingMgr.AddSetter("ABORT", delegate(CPU cpu, object val) { _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Abort, (bool)val); });
            _shared.BindingMgr.AddSetter("AG1", delegate(CPU cpu, object val) { _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom01, (bool)val); });
            _shared.BindingMgr.AddSetter("AG2", delegate(CPU cpu, object val) { _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom02, (bool)val); });
            _shared.BindingMgr.AddSetter("AG3", delegate(CPU cpu, object val) { _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom03, (bool)val); });
            _shared.BindingMgr.AddSetter("AG4", delegate(CPU cpu, object val) { _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom04, (bool)val); });
            _shared.BindingMgr.AddSetter("AG5", delegate(CPU cpu, object val) { _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom05, (bool)val); });
            _shared.BindingMgr.AddSetter("AG6", delegate(CPU cpu, object val) { _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom06, (bool)val); });
            _shared.BindingMgr.AddSetter("AG7", delegate(CPU cpu, object val) { _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom07, (bool)val); });
            _shared.BindingMgr.AddSetter("AG8", delegate(CPU cpu, object val) { _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom08, (bool)val); });
            _shared.BindingMgr.AddSetter("AG9", delegate(CPU cpu, object val) { _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom09, (bool)val); });
            _shared.BindingMgr.AddSetter("AG10", delegate(CPU cpu, object val) { _shared.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom10, (bool)val); });

            _shared.BindingMgr.AddGetter("SAS", delegate(CPU cpu) { return _shared.Vessel.ActionGroups[KSPActionGroup.SAS]; });
            _shared.BindingMgr.AddGetter("GEAR", delegate(CPU cpu) { return _shared.Vessel.ActionGroups[KSPActionGroup.Gear]; });
            _shared.BindingMgr.AddGetter("LEGS", delegate(CPU cpu) { return VesselUtils.GetLandingLegStatus(_shared.Vessel); });
            _shared.BindingMgr.AddGetter("CHUTES", delegate(CPU cpu) { return VesselUtils.GetChuteStatus(_shared.Vessel); });
            _shared.BindingMgr.AddGetter("LIGHTS", delegate(CPU cpu) { return _shared.Vessel.ActionGroups[KSPActionGroup.Light]; });
            _shared.BindingMgr.AddGetter("PANELS", delegate(CPU cpu) { return VesselUtils.GetSolarPanelStatus(_shared.Vessel); });
            _shared.BindingMgr.AddGetter("BRAKES", delegate(CPU cpu) { return _shared.Vessel.ActionGroups[KSPActionGroup.Brakes]; });
            _shared.BindingMgr.AddGetter("RCS", delegate(CPU cpu) { return _shared.Vessel.ActionGroups[KSPActionGroup.RCS]; });
            _shared.BindingMgr.AddGetter("ABORT", delegate(CPU cpu) { return _shared.Vessel.ActionGroups[KSPActionGroup.Abort]; });
            _shared.BindingMgr.AddGetter("AG1", delegate(CPU cpu) { return _shared.Vessel.ActionGroups[KSPActionGroup.Custom01]; });
            _shared.BindingMgr.AddGetter("AG2", delegate(CPU cpu) { return _shared.Vessel.ActionGroups[KSPActionGroup.Custom02]; });
            _shared.BindingMgr.AddGetter("AG3", delegate(CPU cpu) { return _shared.Vessel.ActionGroups[KSPActionGroup.Custom03]; });
            _shared.BindingMgr.AddGetter("AG4", delegate(CPU cpu) { return _shared.Vessel.ActionGroups[KSPActionGroup.Custom04]; });
            _shared.BindingMgr.AddGetter("AG5", delegate(CPU cpu) { return _shared.Vessel.ActionGroups[KSPActionGroup.Custom05]; });
            _shared.BindingMgr.AddGetter("AG6", delegate(CPU cpu) { return _shared.Vessel.ActionGroups[KSPActionGroup.Custom06]; });
            _shared.BindingMgr.AddGetter("AG7", delegate(CPU cpu) { return _shared.Vessel.ActionGroups[KSPActionGroup.Custom07]; });
            _shared.BindingMgr.AddGetter("AG8", delegate(CPU cpu) { return _shared.Vessel.ActionGroups[KSPActionGroup.Custom08]; });
            _shared.BindingMgr.AddGetter("AG9", delegate(CPU cpu) { return _shared.Vessel.ActionGroups[KSPActionGroup.Custom09]; });
            _shared.BindingMgr.AddGetter("AG10", delegate(CPU cpu) { return _shared.Vessel.ActionGroups[KSPActionGroup.Custom10]; });
        }
    }

    [kRISCBinding("ksp")]
    public class BindingFlightSettings : Binding
    {
        public override void AddTo(SharedObjects shared)
        {
            _shared = shared;
            
            _shared.BindingMgr.AddSetter("TARGET", delegate(CPU cpu, object val) 
            {
                if (val is ITargetable)
                {
                    VesselUtils.SetTarget((ITargetable)val);
                }
                else if (val is VesselTarget)
                {
                    VesselUtils.SetTarget(((VesselTarget)val).target);
                }
                else if (val is BodyTarget)
                {
                    VesselUtils.SetTarget(((BodyTarget)val).target);
                }
                else
                {
                    var body = VesselUtils.GetBodyByName(val.ToString());
                    if (body != null)
                    {
                        VesselUtils.SetTarget(body);
                        return;
                    }

                    var vessel = VesselUtils.GetVesselByName(val.ToString(), _shared.Vessel);
                    if (vessel != null)
                    {
                        VesselUtils.SetTarget(vessel);
                        return;
                    }
                }
            });

            _shared.BindingMgr.AddGetter("TARGET", delegate(CPU cpu) 
            {
                var currentTarget = FlightGlobals.fetch.VesselTarget;

                if (currentTarget is Vessel)
                {
                    return new VesselTarget((Vessel)currentTarget, _shared.Vessel);
                }
                else if (currentTarget is CelestialBody)
                {
                    return new BodyTarget((CelestialBody)currentTarget, _shared.Vessel);
                }

                return null;
            });
        }
    }
}
