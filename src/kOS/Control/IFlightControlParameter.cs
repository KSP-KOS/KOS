using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS.Control
{
    public interface IFlightControlParameter
    {
        bool Enabled { get; }
        bool IsAutopilot { get; }
        uint ControlPartId { get; }
        void UpdateValue(object value, SharedObjects shared);
        object GetValue();
        SharedObjects GetShared();
        /// <summary>
        /// The vessel that was responsible for having set this control.
        /// </summary>
        /// <returns>The responsible vessel.</returns>
        Vessel GetResponsibleVessel();
        void UpdateAutopilot(FlightCtrlState c);
        void EnableControl(SharedObjects shared);
        void DisableControl(SharedObjects shared);
        void DisableControl();
        void CopyFrom(IFlightControlParameter origin);
    }
}
