using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Communication;
using UnityEngine;
using kOS.Safe.Exceptions;
using kOS.Safe.Serialization;
using kOS.Suffixed;
using TimeSpan = kOS.Suffixed.TimeSpan;
using kOS.Safe.Communication;

namespace kOS.Communication
{
    [kOS.Safe.Utilities.KOSNomenclature("Connection", KOSToCSharp = false)]
    public class VesselConnection : Connection<kOS.SharedObjects>
    {
        private Vessel vessel;

        public override bool Connected {
            get {
                return shared.ConnectivityMgr.GetDelay(shared.Vessel, vessel) != -1;
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
            return "VESSEL CONNECTION(" + vessel.vesselName + ")";
        }

        protected override BooleanValue SendMessage(Structure content)
        {
            double delay = shared.ConnectivityMgr.GetDelay(shared.Vessel, vessel);

            if (delay == -1)
            {
                return false;
            }

            MessageQueueStructure queue = InterVesselManager.Instance.GetQueue(vessel, shared);

            double sentAt = Planetarium.GetUniversalTime();
            double receivedAt = sentAt + delay;
            queue.Push(Message.Create(content, sentAt, receivedAt, new VesselTarget(shared), shared.Processor.Tag));

            return true;
        }

        protected override Structure Destination()
        {
            return new VesselTarget(vessel, shared);
        }
    }
}