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

        public string Vessel { get; set; }
        public string Processor { get; set; }

        public Message(Structure content, double sentAt, double receivedAt, VesselTarget sender)
            : base(content, sentAt, receivedAt)
        {
            Vessel = sender.Guid.ToString();
        }

        private Message(Structure content, double sentAt, double receivedAt, string vesselGUID)
            : base(content, sentAt, receivedAt)
        {
            Vessel = vesselGUID;
        }

        public Message(PrimitiveStructure content, double sentAt, double receivedAt, VesselTarget sender)
            : base(content, sentAt, receivedAt)
        {
            Vessel = sender.Guid.ToString();
        }

        public override Dump Dump(DumperState s)
        {
            var dump = new DumpDictionary(this.GetType());

            using (var context = s.Context(this))
            {
                dump.Add(DumpSentAt, SentAt);
                dump.Add(DumpReceivedAt, ReceivedAt);
                dump.Add(DumpContent, Content, context);
                dump.Add(DumpVessel, Vessel);
            }

            return dump;
        }

        [DumpDeserializer]
        public static new Message CreateFromDump(DumpDictionary d, SafeSharedObjects shared)
        {
            return new Message(d.GetStructure(DumpContent, shared), d.GetDouble(DumpSentAt), d.GetDouble(DumpReceivedAt), d.GetString(DumpVessel));
        }

        [DumpPrinter]
        public static new void Print(DumpDictionary d, IndentedStringBuilder sb)
        {
            sb.Append("Message [sent: ");
            sb.Append(d.GetDouble(DumpSentAt).ToString());
            sb.Append(", received: ");
            sb.Append(d.GetDouble(DumpReceivedAt).ToString());
            sb.Append("]:");

            using (sb.Indent())
            {
                var inner = d.GetDump(DumpContent);
                inner.WriteReadable(sb);
            }
        }
    }
}

