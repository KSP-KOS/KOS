﻿using System;
using System.Collections.Generic;

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
        Func<Guid, double> GetShortestSignalDelay { get; }
        Func<Guid, double> GetSignalDelayToKSC { get; }
        Func<Guid, Guid, double> GetSignalDelayToSatellite { get; }
        Func<IEnumerable<String>> GetGroundStations { get; }
        Func<String, Guid> GetGroundStationGuid { get; }
        Func<CelestialBody, Guid> GetCelestialBodyGuid { get; }
        Func<Part, Guid> GetAntennaTarget { get; }
        Action<Part, Guid> SetAntennaTarget { get; }
        Func<Guid> GetNoTargetGuid { get; }
        Func<Guid> GetActiveVesselGuid { get; }
   }
}
