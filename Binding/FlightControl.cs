using System;
using System.Collections.Generic;
using UnityEngine;
using kOS.Context;
using kOS.Utilities;
using kOS.Value;

namespace kOS.Binding
{
    [KOSBinding("ksp")]
    public class FlightControl : IBinding
    {
        private Vessel vessel;
        private ICPU cpu;
        private readonly List<LockableControl> controls = new List<LockableControl>();

        public  void BindTo(IBindingManager manager)
        {
            cpu = manager.Cpu;
            vessel = manager.Cpu.Vessel;

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

        public  void Update(float time)
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
        }

        public class LockableControl
        {
            private readonly string propertyName;

            public LockableControl(string name, string propertyName, ICPU cpu, IBindingManager manager)
            {
                Name = name;
                Cpu = cpu;
                Vessel = cpu.Vessel;
                Locked = false;
                Value = 0;
                
                manager.AddGetter(name, c => Value);
                manager.AddSetter(name, delegate { });

                this.propertyName = propertyName;
            }

            public ICPU Cpu { get; private set; }
            public Vessel Vessel { get; private set; }
            public bool Locked { get; private set; }
            public object Value { get; private set; }
            public string Name { get; private set; }

            public void OnFlyByWire(ref FlightCtrlState c)
            {
                var e = Cpu.GetDeepestChildContext().GetLock(propertyName);

                if (e == null) return;
                Value = e.GetValue();

                switch (propertyName)
                {
                    case "throttle":
                        c.mainThrottle = (float)e.Double();
                        break;
                    case "wheelthrottle":
                        c.wheelThrottle = (float)Utils.Clamp(e.Double(), -1, 1);
                        break;
                    case "steering":
                        SteerByWire(c);
                        break;
                    case "wheelsteering":
                        WheelSteer(c);
                        break;
                }

                if (Cpu.GetLock(Name) == null)
                {
                    Locked = false;
                }
            }

            private void WheelSteer(FlightCtrlState c)
            {
                float bearing = 0;

                if (Value is VesselTarget)
                {
                    bearing = VesselUtils.GetTargetBearing(Vessel, ((VesselTarget) Value).Target);
                }
                else if (Value is GeoCoordinates)
                {
                    bearing = ((GeoCoordinates) Value).GetBearing(Vessel);
                }
                else if (Value is double)
                {
                    bearing = (float) (Math.Round((double) Value) - Mathf.Round(FlightGlobals.ship_heading));
                }

                if (!(Vessel.horizontalSrfSpeed > 0.1f)) return;

                if (Mathf.Abs(VesselUtils.AngleDelta(VesselUtils.GetHeading(Vessel), VesselUtils.GetVelocityHeading(Vessel))) <=
                    90)
                {
                    c.wheelSteer = Mathf.Clamp(bearing/-10, -1, 1);
                }
                else
                {
                    c.wheelSteer = -Mathf.Clamp(bearing/-10, -1, 1);
                }
            }

            private void SteerByWire(FlightCtrlState c)
            {
                if (Value is string && ((string) Value).ToUpper() == "KILL")
                {
                    SteeringHelper.KillRotation(c, Vessel);
                }
                else if (Value is Direction)
                {
                    SteeringHelper.SteerShipToward((Direction) Value, c, Vessel);
                }
                else if (Value is Vector)
                {
                    SteeringHelper.SteerShipToward(((Vector) Value).ToDirection(), c, Vessel);
                }
                else if (Value is Node)
                {
                    SteeringHelper.SteerShipToward(((Node) Value).GetBurnVector().ToDirection(), c, Vessel);
                }
            }

            internal void UpdateVessel(Vessel vessel)
            {
                this.Vessel = vessel;
            }
        }
    }
}
