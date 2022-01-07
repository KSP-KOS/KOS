using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Communication;
using UnityEngine;
using kOS.Safe.Exceptions;
using kOS.Safe.Serialization;
using kOS.Suffixed;
using kOS.Safe.Communication;

namespace kOS.Communication
{
    [kOS.Safe.Utilities.KOSNomenclature("Connection", KOSToCSharp = false)]
    public class VesselConnection : Connection
    {
        private SharedObjects shared;
        private Vessel vessel;

        public override bool Connected
        {
            get
            {
                return Delay >= 0;
            }
        }

        public override double Delay
        {
            get
            {
                return ConnectivityManager.GetDelay(shared.Vessel, vessel);
            }
        }

        public VesselConnection(Vessel vessel, SharedObjects shared) : base()
        {
            this.shared = shared;
            this.vessel = vessel;
        }

        public override string ToString()
        {
            return "VESSEL CONNECTION(" + vessel.vesselName + ")";
        }

        protected override BooleanValue SendMessage(Structure content)
        {
            if (!Connected)
            {
                return false;
            }

            MessageQueueStructure queue = InterVesselManager.Instance.GetQueue(vessel, shared);

            double sentAt = Planetarium.GetUniversalTime();
            double receivedAt = sentAt + Delay;
            queue.Push(new Message(content, sentAt, receivedAt, VesselTarget.CreateOrGetExisting(shared)));

            return true;
        }

        protected override Structure Destination()
        {
            return VesselTarget.CreateOrGetExisting(vessel, shared);
        }
    }
}