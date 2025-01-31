using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;
using kOS.Suffixed;
using kOS.Utilities;
using System;
using UnityEngine;

namespace kOS.Control
{
    internal class WheelSteeringManager : IFlightControlParameter
    {
        private Vessel internalVessel;
        private uint controlPartId;
        private SharedObjects controlShared;

        public bool Enabled { get; private set; }
        public float Value { get; set; }

        public bool FightsWithSas { get { return false; } }

        public WheelSteeringManager(Vessel vessel)
        {
            Enabled = false;
            controlPartId = 0;

            internalVessel = vessel;
        }

        uint IFlightControlParameter.ControlPartId
        {
            get
            {
                return controlPartId;
            }
        }

        bool IFlightControlParameter.Enabled
        {
            get
            {
                return Enabled;
            }
        }

        bool IFlightControlParameter.IsAutopilot
        {
            get
            {
                return true;
            }
        }

        void IFlightControlParameter.CopyFrom(IFlightControlParameter origin)
        {
            object val = origin.GetValue();
            Value = Convert.ToSingle(val);
        }

        void IFlightControlParameter.DisableControl()
        {
            controlShared = null;
            controlPartId = 0;
            Enabled = false;
        }

        void IFlightControlParameter.DisableControl(SharedObjects shared)
        {
            if (shared.KSPPart.flightID != controlPartId)
                return;
            ((IFlightControlParameter)this).DisableControl();
        }

        void IFlightControlParameter.EnableControl(SharedObjects shared)
        {
            controlPartId = shared.KSPPart.flightID;
            controlShared = shared;
            Enabled = true;
        }

        SharedObjects IFlightControlParameter.GetShared()
        {
            return controlShared;
        }

        Vessel IFlightControlParameter.GetResponsibleVessel()
        {
            return controlShared.Vessel;
        }

        object IFlightControlParameter.GetValue()
        {
            return Value;
        }

        void IFlightControlParameter.UpdateAutopilot(FlightCtrlState c, ControlTypes ctrlLock)
        {
            if (!Enabled) return;

            if (!(controlShared.Vessel.horizontalSrfSpeed > 0.1f)) return;
            
            float vesselHeading = VesselUtils.GetHeading(controlShared.Vessel);
            float bearing = VesselUtils.AngleDelta(vesselHeading, Value);

            if (Mathf.Abs(VesselUtils.AngleDelta(vesselHeading, VesselUtils.GetVelocityHeading(controlShared.Vessel))) <= 90)
            {
                c.wheelSteer = Mathf.Clamp(bearing / -10, -1, 1);
            }
            else
            {
                c.wheelSteer = -Mathf.Clamp(bearing / -10, -1, 1);
            }
        }

        bool IFlightControlParameter.SuppressAutopilot(FlightCtrlState c)
        {
            return Enabled;
        }

        void IFlightControlParameter.UpdateValue(object value, SharedObjects shared)
        {
            if (!Enabled)
                ((IFlightControlParameter)this).EnableControl(shared);

            if (value is VesselTarget vessel)
            {
                Value = VesselUtils.GetTargetHeading(controlShared.Vessel, vessel.Vessel);
            }
            else if (value is GeoCoordinates gc)
            {
                Value = gc.GetHeading();
            }
            else
            {
                try
                {
                    float heading = Convert.ToSingle(value);
                    if (!float.IsInfinity(heading) && !float.IsNaN(heading))
                        Value = heading;
                }
                catch
                {
                    throw new KOSWrongControlValueTypeException(
                        "WHEELSTEERING",
                        KOSNomenclature.GetKOSName(value.GetType()),
                        string.Format(
                            "{0}, {1}, or {2} (compass heading)",
                            KOSNomenclature.GetKOSName(typeof(VesselTarget)),
                            KOSNomenclature.GetKOSName(typeof(GeoCoordinates)),
                            KOSNomenclature.GetKOSName(typeof(ScalarValue))
                        )
                    );
                }
            }
        }
    }
}