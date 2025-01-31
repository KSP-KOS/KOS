using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;
using System;

namespace kOS.Control
{
    internal class WheelThrottleManager : IFlightControlParameter
    {
        private Vessel internalVessel;
        private uint controlPartId;
        private SharedObjects controlShared;

        public bool Enabled { get; private set; }
        public double Value { get; set; }

        public bool FightsWithSas { get { return false; } }

        public WheelThrottleManager(Vessel vessel)
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
            Value = Convert.ToDouble(val);
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
            if (Enabled)
            {
                return Value;
            }
            return internalVessel.ctrlState.wheelThrottle;
        }

        void IFlightControlParameter.UpdateAutopilot(FlightCtrlState c, ControlTypes ctrlLock)
        {
            if (Enabled)
            {
                c.wheelThrottle = (float)KOSMath.Clamp(Value, -1, 1);
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

            try
            {
                Value = Convert.ToDouble(value);
            }
            catch
            {
                throw new KOSWrongControlValueTypeException(
                    "WHEELTHROTTLE",
                    KOSNomenclature.GetKOSName(value.GetType()),
                    string.Format("{0} in the range [0..1]", KOSNomenclature.GetKOSName(typeof(ScalarValue)))
                    );
            }
        }
    }
}