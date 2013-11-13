using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS
{
    public interface IRemoteTech
    {
        void AddSanctionedPilot(Guid satellite, Action<FlightCtrlState> autopilot);
        void RemoveSanctionedPilot(Guid satellite, Action<FlightCtrlState> autopilot);
        bool HasAnyConnection(Guid satellite);
        bool HasConnectionToKSC(Guid satellite);
        double GetShortestSignalDelay(Guid satellite);
        double GetSignalDelayToKSC(Guid satellite);
        double GetSignalDelayToSatellite(Guid a, Guid b);
    }
}
