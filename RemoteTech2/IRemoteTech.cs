using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS.RemoteTech2
{
    public interface IRemoteTech
    {
        void AddSanctionedPilot(Guid satellite, Action<FlightCtrlState> autopilot);
        void RemoveSanctionedPilot(Guid satellite, Action<FlightCtrlState> autopilot);
    }
}
