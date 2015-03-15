﻿using System;

namespace kOS.AddOns.RemoteTech
{
    internal class RemoteTechAPI : IRemoteTechAPIv1
    {
        public Func<Guid, bool> HasFlightComputer { get; internal set; }
        public Action<Guid, Action<FlightCtrlState>> AddSanctionedPilot { get; internal set; }
        public Action<Guid, Action<FlightCtrlState>> RemoveSanctionedPilot { get; internal set; }
        public Func<Guid, bool> HasAnyConnection { get; internal set; }
        public Func<Guid, bool> HasConnectionToKSC { get; internal set; }
        public Func<Guid, double> GetShortestSignalDelay { get; internal set; }
        public Func<Guid, double> GetSignalDelayToKSC { get; internal set; }
        public Func<Guid, Guid, double> GetSignalDelayToSatellite { get; internal set; }
    }
}