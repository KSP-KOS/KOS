using System;
using kOS.Communication;
using System.Collections.Generic;
using kOS.Safe.Serialization;
using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;
using UnityEngine;
using System.Linq;
using kOS.Serialization;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe;
using kOS.Suffixed;
using TimeSpan = kOS.Suffixed.TimeSpan;

namespace kOS.Communication
{
    public class MessageQueue : SerializableStructure
    {
        private SortedList<TimeSpan, Message> messages = new SortedList<TimeSpan, Message>();

        private List<Message> ReceivedMessages {
            get {
                return messages.Values.Where(IsReceived).ToList();
            }
        }

        private bool IsReceived(Message message)
        {
            return message.ReceivedAt.ToUnixStyleTime() <= Planetarium.GetUniversalTime();
        }

        public void Clear()
        {
            foreach (KeyValuePair<TimeSpan, Message> pair in messages)
            {
                if (IsReceived(pair.Value))
                {
                    messages.Remove(pair.Key);
                }
            }
        }

        public Message Peek()
        {
            if (messages.Count > 0 && IsReceived(messages.First().Value))
            {
                return messages.First().Value;
            }

            throw new KOSException("Message queue is empty");
        }

        public Message Pop()
        {
            if (messages.Count > 0 && IsReceived(messages.First().Value))
            {
                Message m =  messages.First().Value;
                messages.RemoveAt(0);

                return m;
            }

            throw new KOSException("Message queue is empty");
        }

        public int Count()
        {
            return messages.Count;
        }

        public int ReceivedCount()
        {
            return ReceivedMessages.Count();
        }

        public void Push(object content, kOS.Suffixed.TimeSpan sentAt, kOS.Suffixed.TimeSpan receivedAt, VesselTarget sender)
        {
            Message message = new Message();
            message.SentAt = sentAt;
            message.ReceivedAt = receivedAt;
            message.Sender = sender;

            if (content is SerializableStructure)
            {
                message.Content = new SafeSerializationMgr().Dump(content as SerializableStructure, true);
            } else if (SerializationMgr.IsSerializablePrimitive(content))
            {
                message.Content = content;
            } else
            {
                throw new KOSException("Only serializable types and primitives can be sent in a message");
            }

            InsertMessage(message);
        }

        private void InsertMessage(Message message)
        {
            messages.Add(message.ReceivedAt, message);
        }

        public override string ToString()
        {
            return "MESSAGE QUEUE";
        }

        public override Dump Dump()
        {
            DumpWithHeader dump = new DumpWithHeader();
            dump.Header = "MESSAGE QUEUE";

            int i = 0;

            foreach (Message message in messages.Values)
            {
                dump.Add(i, message);

                i++;
            }

            return dump;
        }

        public override void LoadDump(Dump dump)
        {
            messages.Clear();

            foreach (KeyValuePair<object, object> entry in dump)
            {
                Message message = entry.Value as Message;

                if (message != null)
                {
                    messages.Add(message.ReceivedAt, message);
                }
            }
        }

    }
}

