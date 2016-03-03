using System;
using kOS.Safe.Encapsulation;
using kOS.Suffixed;
using kOS.Safe.Serialization;
using kOS.Safe.Exceptions;
using kOS.Safe;

namespace kOS.Communication
{
    public class Message : SerializableStructure, IComparable<Message>
    {
        public const string DumpSentAt = "sentAt";
        public const string DumpReceivedAt = "receivedAt";
        public const string DumpSender = "sender";
        public const string DumpContent = "content";

        public kOS.Suffixed.TimeSpan SentAt { get; set; }
        public kOS.Suffixed.TimeSpan ReceivedAt { get; set; }
        public VesselTarget Sender { get; set; }

        /// <summary>
        /// Message content can be either a simple encapsulated type (something that implements ISerializableValue) or a Dump.
        /// 
        /// Currently kOS serialization doesn't allow primitives as top level objects, but Messages do allow them.
        /// </summary>
        private object content;

        public object Content {
            get {
                return content;
            }
            set {
                if (value is PrimitiveStructure || value is Dump)
                {
                    content = value;
                } else
                {
                    throw new KOSException("Message can only contain primitives and serializable types");
                }
            }
        }

        public Message()
        {
        }

        public override Dump Dump()
        {
            DumpWithHeader dump = new DumpWithHeader();

            dump.Header = "MESSAGE";

            dump.Add(DumpSentAt, SentAt);
            dump.Add(DumpReceivedAt, ReceivedAt);
            dump.Add(DumpSender, Sender);
            dump.Add(DumpContent, content);

            return dump;
        }

        public override void LoadDump(Dump dump)
        {
            SentAt = dump[DumpSentAt] as kOS.Suffixed.TimeSpan;
            ReceivedAt = dump[DumpReceivedAt] as kOS.Suffixed.TimeSpan;
            Sender = dump[DumpSender] as VesselTarget;
            content = dump[DumpContent];
        }

        public override string ToString()
        {
            return "MESSAGE FROM " + Sender.GetName();
        }

        public int CompareTo(Message other)
        {
            return ReceivedAt.CompareTo(other.ReceivedAt);
        }
    }
}

