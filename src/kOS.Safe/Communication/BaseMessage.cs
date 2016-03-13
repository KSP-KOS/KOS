using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Serialization;
using kOS.Safe.Exceptions;
using kOS.Safe;

namespace kOS.Safe.Communication
{
    public class BaseMessage : IDumper, IComparable<BaseMessage>
    {
        public const string DumpSentAt = "sentAt";
        public const string DumpReceivedAt = "receivedAt";
        public const string DumpContent = "content";

        public double SentAt { get; set; }
        public double ReceivedAt { get; set; }

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

        public BaseMessage()
        {

        }

        public BaseMessage(Dump content, double sentAt, double receivedAt)
        {
            Content = content;
            SentAt = sentAt;
            ReceivedAt = receivedAt;
        }

        public BaseMessage(PrimitiveStructure content, double sentAt, double receivedAt)
        {
            Content = content;
            SentAt = sentAt;
            ReceivedAt = receivedAt;
        }

        public virtual Dump Dump()
        {
            DumpWithHeader dump = new DumpWithHeader();

            dump.Header = "MESSAGE";

            dump.Add(DumpSentAt, SentAt);
            dump.Add(DumpReceivedAt, ReceivedAt);
            dump.Add(DumpContent, content);

            return dump;
        }

        public virtual void LoadDump(Dump dump)
        {
            SentAt = Convert.ToDouble(dump[DumpSentAt]);
            ReceivedAt = Convert.ToDouble(dump[DumpReceivedAt]);
            content = dump[DumpContent];
        }

        public int CompareTo(BaseMessage other)
        {
            return ReceivedAt.CompareTo(other.ReceivedAt);
        }
    }
}

