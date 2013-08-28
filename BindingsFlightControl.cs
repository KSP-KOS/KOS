using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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
                    this.Value = e.GetValue();

                    if (propertyName == "throttle") c.mainThrottle = (float)Value;
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
}
