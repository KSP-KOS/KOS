using System;
using kOS.Safe.Communication;
using kOS.Suffixed;
using kOS.Safe;
using kOS.Safe.Encapsulation;
using kOS.Safe.Serialization;
using kOS.Safe.Exceptions;

namespace kOS.Communication
{
    public class Message : BaseMessage
    {
        public const string DumpVessel = "vessel";
        public const string DumpProcessor = "processor";

        public VesselTarget Vessel { get; set; }
        public string Processor { get; set; }

        public static Message Create(object content, double sentAt, double receivedAt, VesselTarget sender, string processor)
        {
            if (content is SerializableStructure)
            {
                return new Message(new SafeSerializationMgr().Dump(content as SerializableStructure), sentAt, receivedAt, sender);
            } else if (content is PrimitiveStructure)
            {
                return new Message(content as PrimitiveStructure, sentAt, receivedAt, sender);
            } else
            {
                throw new KOSCommunicationException("Only serializable types and primitives can be sent in a message");
            }
        }

        public Message()
            : base()
        {
        }

        public Message(Dump content, double sentAt, double receivedAt, VesselTarget sender)
            : base(content, sentAt, receivedAt)
        {
            Vessel = sender;
        }

        public Message(PrimitiveStructure content, double sentAt, double receivedAt, VesselTarget sender)
            : base(content, sentAt, receivedAt)
        {
            Vessel = sender;
        }

        public override Dump Dump()
        {
            Dump dump = base.Dump();

            dump.Add(DumpVessel, Vessel);

            return dump;
        }

        public override void LoadDump(Dump dump)
        {
            base.LoadDump(dump);

            Vessel = dump[DumpVessel] as VesselTarget;
        }
    }
}

