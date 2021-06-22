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

        public IDumper Content { get; set; }

        public BaseMessage(Structure content, double sentAt, double receivedAt)
        {
            Content = content;
            SentAt = sentAt;
            ReceivedAt = receivedAt;
        }

        public virtual Dump Dump(DumperState s)
        {
            var dump = new DumpDictionary(typeof(BaseMessage));

            using (var context = s.Context(this))
            {
                dump.Add(DumpSentAt, SentAt);
                dump.Add(DumpReceivedAt, ReceivedAt);
                dump.Add(DumpContent, Content, context);
            }

            return dump;
        }

        public static BaseMessage CreateFromDump(DumpDictionary d, SafeSharedObjects shared)
        {
            return new BaseMessage(d.GetStructure(DumpContent, shared), d.GetDouble(DumpSentAt), d.GetDouble(DumpReceivedAt));
        }

        public static void Print(DumpDictionary d, IndentedStringBuilder sb)
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
        
        public int CompareTo(BaseMessage other)
        {
            return ReceivedAt.CompareTo(other.ReceivedAt);
        }
    }
}

