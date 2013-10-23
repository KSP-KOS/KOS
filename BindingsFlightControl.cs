using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using UnityEngine;

namespace kOS
{
    [kOSBinding("ksp")]
    public class BindingFlightControls : Binding
    {
        private Vessel vessel;
        private CPU cpu;
        private List<LockableControl> controls = new List<LockableControl>();

        public override void AddTo(BindingManager manager)
        {
            cpu = manager.cpu;
            vessel = manager.cpu.Vessel;

            controls.Add(new LockableControl("THROTTLE", "throttle", cpu, manager));
            controls.Add(new LockableControl("STEERING", "steering", cpu, manager));

            controls.Add(new LockableControl("WHEELSTEERING", "wheelsteering", cpu, manager));
            controls.Add(new LockableControl("WHEELTHROTTLE", "wheelthrottle", cpu, manager));

            vessel.OnFlyByWire += OnFlyByWire;
        }
        
        public void OnFlyByWire(FlightCtrlState c)
        {
            foreach (LockableControl control in controls)
            {
                control.OnFlyByWire(ref c);
            }
        }

        public override void Update(float time)
        {
            if (vessel != cpu.Vessel)
            {
                // Try to re-establish connection to vessel
                if (vessel != null)
                {
                    vessel.OnFlyByWire -= OnFlyByWire;
                    vessel = null;
                }

                if (cpu.Vessel != null)
                {
                    vessel = cpu.Vessel;
                    vessel.OnFlyByWire += OnFlyByWire;

                    foreach (LockableControl c in controls)
                    {
                        c.UpdateVessel(vessel);
                    }
                }
            }

            base.Update(time);
        }

        public class LockableControl
        {
            public String name;
            public bool locked;
            public object Value;
            public Vessel vessel;
            String propertyName;
            public CPU cpu;

            public LockableControl(String name, String propertyName, CPU cpu, BindingManager manager)
            {
                this.name = name;
                this.cpu = cpu;
                this.vessel = cpu.Vessel;
                locked = false;
                Value = 0;
                
                manager.AddGetter(name, delegate(CPU c) { return Value; });
                manager.AddSetter(name, delegate(CPU c, object val)  { });

                this.propertyName = propertyName;
            }

            public void OnFlyByWire(ref FlightCtrlState c)
            {
                Expression e = cpu.GetDeepestChildContext().GetLock(propertyName);
                
                if (e != null)
                {
                    Value = e.GetValue();

                    if (propertyName == "throttle")
                    {
                        c.mainThrottle = (float)e.Double();
                    }

                    if (propertyName == "wheelthrottle")
                    {
                        c.wheelThrottle = (float)Utils.Clamp(e.Double(), -1, 1);
                    }

                    if (propertyName == "steering")
                    {
                        if (Value is String && ((string)Value).ToUpper() == "KILL")
                        {
                            SteeringHelper.KillRotation(c, vessel);
                        }
                        else if (Value is Direction)
                        {
                            SteeringHelper.SteerShipToward((Direction)Value, c, vessel);
                        }
                        else if (Value is Vector)
                        {
                            SteeringHelper.SteerShipToward(((Vector)Value).ToDirection(), c, vessel);
                        }
                        else if (Value is Node)
                        {
                            SteeringHelper.SteerShipToward(((Node)Value).GetBurnVector().ToDirection(), c, vessel);
                        }
                    }

                    if (propertyName == "wheelsteering")
                    {
                        float bearing = 0;

                        if (Value is VesselTarget)
                        {
                            bearing = VesselUtils.GetTargetBearing(vessel, ((VesselTarget)Value).target);
                        }
                        else if (Value is GeoCoordinates)
                        {
                            bearing = ((GeoCoordinates)Value).GetBearing(vessel);
                        }

                        if (vessel.horizontalSrfSpeed > 0.1f)
                        { 
                            if (Mathf.Abs(VesselUtils.AngleDelta(VesselUtils.GetHeading(vessel), VesselUtils.GetVelocityHeading(vessel))) <= 90)
                            {
                                c.wheelSteer = Mathf.Clamp(bearing / -10, -1, 1);
                            }
                            else
                            {
                                c.wheelSteer = -Mathf.Clamp(bearing / -10, -1, 1);
                            }
                        }
                    }

                    if (cpu.GetLock(name) == null)
                    {
                        locked = false;
                    }
                }
            }

            internal void UpdateVessel(Vessel vessel)
            {
                this.vessel = vessel;
            }
        }
    }
    
    [kOSBinding("ksp")]
    public class BindingActionGroups : Binding
    {
        public override void AddTo(BindingManager manager)
        {
            manager.AddSetter("SAS", delegate(CPU cpu, object val) { cpu.Vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, (bool)val); });
            manager.AddSetter("GEAR", delegate(CPU cpu, object val) { cpu.Vessel.ActionGroups.SetGroup(KSPActionGroup.Gear, (bool)val); });
            manager.AddSetter("LEGS", delegate(CPU cpu, object val) {  VesselUtils.LandingLegsCtrl(cpu.Vessel, (bool)val); });
            manager.AddSetter("CHUTES", delegate(CPU cpu, object val) {  VesselUtils.DeployParachutes(cpu.Vessel, (bool)val); });
            manager.AddSetter("LIGHTS", delegate(CPU cpu, object val) { cpu.Vessel.ActionGroups.SetGroup(KSPActionGroup.Light, (bool)val); });
            manager.AddSetter("BRAKES", delegate(CPU cpu, object val) { cpu.Vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, (bool)val); });
            manager.AddSetter("RCS", delegate(CPU cpu, object val) { cpu.Vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, (bool)val); });
            manager.AddSetter("ABORT", delegate(CPU cpu, object val) { cpu.Vessel.ActionGroups.SetGroup(KSPActionGroup.Abort, (bool)val); });
            manager.AddSetter("AG1", delegate(CPU cpu, object val) { cpu.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom01, (bool)val); });
            manager.AddSetter("AG2", delegate(CPU cpu, object val) { cpu.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom02, (bool)val); });
            manager.AddSetter("AG3", delegate(CPU cpu, object val) { cpu.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom03, (bool)val); });
            manager.AddSetter("AG4", delegate(CPU cpu, object val) { cpu.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom04, (bool)val); });
            manager.AddSetter("AG5", delegate(CPU cpu, object val) { cpu.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom05, (bool)val); });
            manager.AddSetter("AG6", delegate(CPU cpu, object val) { cpu.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom06, (bool)val); });
            manager.AddSetter("AG7", delegate(CPU cpu, object val) { cpu.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom07, (bool)val); });
            manager.AddSetter("AG8", delegate(CPU cpu, object val) { cpu.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom08, (bool)val); });
            manager.AddSetter("AG9", delegate(CPU cpu, object val) { cpu.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom09, (bool)val); });
            manager.AddSetter("AG10", delegate(CPU cpu, object val) { cpu.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom10, (bool)val); });

            manager.AddGetter("SAS", delegate(CPU cpu) { return cpu.Vessel.ActionGroups[KSPActionGroup.SAS]; });
            manager.AddGetter("GEAR", delegate(CPU cpu) { return cpu.Vessel.ActionGroups[KSPActionGroup.Gear]; });
            manager.AddGetter("LEGS", delegate(CPU cpu) { return VesselUtils.LandingLegStatus; });
            manager.AddGetter("CHUTES", delegate(CPU cpu) { return VesselUtils.ChutesStatus; });
            manager.AddGetter("LIGHTS", delegate(CPU cpu) { return cpu.Vessel.ActionGroups[KSPActionGroup.Light]; });
            manager.AddGetter("BRAKES", delegate(CPU cpu) { return cpu.Vessel.ActionGroups[KSPActionGroup.Brakes]; });
            manager.AddGetter("RCS", delegate(CPU cpu) { return cpu.Vessel.ActionGroups[KSPActionGroup.RCS]; });
            manager.AddGetter("ABORT", delegate(CPU cpu) { return cpu.Vessel.ActionGroups[KSPActionGroup.Abort]; });
            manager.AddGetter("AG1", delegate(CPU cpu) { return cpu.Vessel.ActionGroups[KSPActionGroup.Custom01]; });
            manager.AddGetter("AG2", delegate(CPU cpu) { return cpu.Vessel.ActionGroups[KSPActionGroup.Custom02]; });
            manager.AddGetter("AG3", delegate(CPU cpu) { return cpu.Vessel.ActionGroups[KSPActionGroup.Custom03]; });
            manager.AddGetter("AG4", delegate(CPU cpu) { return cpu.Vessel.ActionGroups[KSPActionGroup.Custom04]; });
            manager.AddGetter("AG5", delegate(CPU cpu) { return cpu.Vessel.ActionGroups[KSPActionGroup.Custom05]; });
            manager.AddGetter("AG6", delegate(CPU cpu) { return cpu.Vessel.ActionGroups[KSPActionGroup.Custom06]; });
            manager.AddGetter("AG7", delegate(CPU cpu) { return cpu.Vessel.ActionGroups[KSPActionGroup.Custom07]; });
            manager.AddGetter("AG8", delegate(CPU cpu) { return cpu.Vessel.ActionGroups[KSPActionGroup.Custom08]; });
            manager.AddGetter("AG9", delegate(CPU cpu) { return cpu.Vessel.ActionGroups[KSPActionGroup.Custom09]; });
            manager.AddGetter("AG10", delegate(CPU cpu) { return cpu.Vessel.ActionGroups[KSPActionGroup.Custom10]; });
        }
    }

    [kOSBinding("ksp")]
    public class BindingFlightSettings : Binding
    {
        public override void AddTo(BindingManager manager)
        {
            manager.AddSetter("TARGET", delegate(CPU cpu, object val) 
            {
                if (val is ITargetable)
                {
                    VesselUtils.SetTarget((ITargetable)val);
                }
                else if (val is VesselTarget)
                {
                    VesselUtils.SetTarget(((VesselTarget)val).target);
                }
                else
                {
                    var vessel = VesselUtils.GetVesselByName(val.ToString(), cpu.Vessel);
                    VesselUtils.SetTarget(vessel);
                }
            });

            manager.AddGetter("TARGET", delegate(CPU cpu) { return new VesselTarget((Vessel)FlightGlobals.fetch.VesselTarget, cpu); });
        }
    }
}
