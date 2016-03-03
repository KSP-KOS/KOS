using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Communication;
using UnityEngine;
using kOS.Safe.Exceptions;
using kOS.Safe.Serialization;
using kOS.Suffixed;
using TimeSpan = kOS.Suffixed.TimeSpan;

namespace kOS.Communication
{
    [kOS.Safe.Utilities.KOSNomenclature("Connection", KOSToCSharp = false)]
    public class VesselConnection : Connection
    {
        private Vessel vessel;

        public override bool Connected {
            get {
                return shared.ConnectivityMgr.GetDelay(shared.Vessel, vessel) != Connection.Infinity;
            }
        }

        public override double Delay {
            get {
                return shared.ConnectivityMgr.GetDelay(shared.Vessel, vessel);
            }
        }

        public VesselConnection(Vessel vessel, SharedObjects shared) : base(shared)
        {
            this.vessel = vessel;
        }

        public override string ToString()
        {
            return "VESSEL CONNECTION(" + shared.Vessel.vesselName + ")";
        }

        protected override BooleanValue SendMessage(Structure content)
        {
            MessageQueueStructure queue = InterVesselManager.Instance.GetQueue(vessel, shared);
            double delay = shared.ConnectivityMgr.GetDelay(shared.Vessel, vessel);

            if (delay == -1)
            {
                return false;
            }

            TimeSpan sentAt = new TimeSpan(Planetarium.GetUniversalTime());
            TimeSpan receivedAt = new TimeSpan(sentAt.ToUnixStyleTime() + delay);
            queue.Push(content, sentAt, receivedAt, new VesselTarget(shared));

            return true;
        }

        protected override Structure Destination()
        {
            return new VesselTarget(vessel, shared);
        }
    }
}