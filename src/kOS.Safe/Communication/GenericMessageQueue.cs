using System;
using System.Collections.Generic;
using kOS.Safe.Serialization;
using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;
using System.Linq;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe;

namespace kOS.Safe.Communication
{
    public class GenericMessageQueue<M,TP> : IDumper where M : BaseMessage where TP : CurrentTimeProvider
    {
        /// <summary>
        /// This stores a mapping: moment in time -> list of messages that arrive at that time.
        /// We need the queue to be constructed this way because SortedList doesn't allow for duplicate keys.
        /// </summary>
        private SortedList<double, List<M>> queue = new SortedList<double, List<M>>();
        private CurrentTimeProvider timeProvider;

        /// <summary>
        /// Gives a reference to the CurrentTimeProvider of type TP this class created when
        /// it was constructed.
        /// </summary>
        public CurrentTimeProvider TimeProvider { get { return timeProvider; } }

        private List<M> Messages
        {
            get
            {
                return queue.Aggregate(new List<M>(), (acc, l) => {
                    acc.AddRange(l.Value);
                    return acc;
                });
            }
        }

        private List<M> ReceivedMessages
        {
            get
            {
                return Messages.Where(IsReceived).ToList();
            }
        }

        private bool IsReceived(double time)
        {
            return time <= timeProvider.CurrentTime();
        }

        private bool IsReceived(M message)
        {
            return IsReceived(message.ReceivedAt);
        }

        public GenericMessageQueue()
        {
            this.timeProvider = Activator.CreateInstance(typeof(TP)) as CurrentTimeProvider;
        }

        private void RemoveMessage(KeyValuePair<double, List<M>> queueItem, M message)
        {
            queueItem.Value.Remove(message);
            if (queueItem.Value.Count() == 0)
            {
                queue.Remove(queueItem.Key);
            }
        }

        public void Clear()
        {
            foreach (KeyValuePair<double, List<M>> queueItem in queue)
            {
                queueItem.Value.RemoveAll((m) => IsReceived(m));
            }

            var toRemove = queue.Where((k) => k.Value.Count() == 0).ToList();

            toRemove.ForEach(item => queue.Remove(item.Key));
        }

        public M Peek()
        {
            if (queue.Count > 0 && IsReceived(queue.First().Key))
            {
                return queue.First().Value.First();
            }

            throw new KOSCommunicationException("Message queue is empty");
        }

        public M Pop()
        {
            if (queue.Count > 0 && IsReceived(queue.First().Key))
            {
                M message = queue.First().Value.First();
                RemoveMessage(queue.First(), message);

                return message;
            }

            throw new KOSCommunicationException("Message queue is empty");
        }

        public int Count()
        {
            return Messages.Count;
        }

        public int ReceivedCount()
        {
            return ReceivedMessages.Count();
        }

        public void Push(M message)
        {
            if (!queue.ContainsKey(message.ReceivedAt))
            {
                queue.Add(message.ReceivedAt, new List<M>());
            }

            List<M> list = queue[message.ReceivedAt];

            list.Add(message);
        }

        public override string ToString()
        {
            return "MESSAGE QUEUE";
        }

        public void LoadDump(DumpList dump)
        {
            queue.Clear();

            for(int i = 0; i < dump.Count; i++)
            {
                M message = dump[i] as M;

                if (message != null)
                {
                    Push(message);
                }
            }
        }

        public Dump Dump(DumperState s)
        {
            var dump = new DumpList(this.GetType());

            foreach (M message in Messages)
            {
                using (var c = s.Context(this))
                    dump.Add(message, c);
            }

            return dump;
        }
    }
}
