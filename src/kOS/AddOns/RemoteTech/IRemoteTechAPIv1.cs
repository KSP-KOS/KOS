using System;

namespace kOS.AddOns.RemoteTech
{
    public interface IRemoteTechAPIv1
    {
        Func<Guid, bool> HasLocalControl { get; }
        Func<Guid, bool> HasFlightComputer { get; }
        Action<Guid, Action<FlightCtrlState>> AddSanctionedPilot { get; }
        Action<Guid, Action<FlightCtrlState>> RemoveSanctionedPilot { get; }
        Func<Guid, bool> HasAnyConnection { get; }
        Func<Guid, bool> HasConnectionToKSC { get; }
        Func<Part, bool> AntennaHasConnection { get; }
        Func<Guid, double> GetShortestSignalDelay { get; }
        Func<Guid, double> GetSignalDelayToKSC { get; }
        Func<Guid, Guid, double> GetSignalDelayToSatellite { get; }
    }
}
