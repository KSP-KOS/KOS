using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Serialization;

namespace kOS.Safe.Encapsulation
{
    public class QueueValue<T> : EnumerableValue<T, Queue<T>>
    {
        public QueueValue() : this(new Queue<T>())
        {
        }

        public QueueValue(IEnumerable<T> queueValue) : base(new Queue<T>(queueValue))
        {
            QueueInitializeSuffixes();
        }

        public override int Count
        {
            get { return Collection.Count; }
        }

        public T Pop()
        {
            return Collection.Dequeue();
        }

        public void Push(T val)
        {
            Collection.Enqueue(val);
        }

        public override Dump Dump()
        {
            var result = new DumpWithHeader
            {
                Header = "QUEUE of " + Collection.Count() + " items:"
            };

            result.Add(kOS.Safe.Dump.Items, Collection.ToList());

            return result;
        }

        public override void LoadDump(Dump dump)
        {
            Collection.Clear();

            List<object> values = (List<object>)dump[kOS.Safe.Dump.Items];

            foreach (object item in values)
            {
                Collection.Enqueue((T)Structure.FromPrimitive(item));
            }
        }

        private void QueueInitializeSuffixes()
        {
            AddSuffix("COPY",     new NoArgsSuffix<QueueValue<T>>       (() => new QueueValue<T>(this)));
            AddSuffix("LENGTH",   new NoArgsSuffix<int>                 (() => Collection.Count));
            AddSuffix("PUSH",     new OneArgsSuffix<T>                  (toPush => Collection.Enqueue(toPush)));
            AddSuffix("POP",      new NoArgsSuffix<T>                   (() => Collection.Dequeue()));
            AddSuffix("PEEK",     new NoArgsSuffix<T>                   (() => Collection.Peek()));
            AddSuffix("CLEAR",    new NoArgsSuffix                      (() => Collection.Clear()));
        }

        public static QueueValue<T> CreateQueue<TU>(IEnumerable<TU> list)
        {
            return new QueueValue<T>(list.Cast<T>());
        }

    }

    public class QueueValue : QueueValue<object>
    {
        public QueueValue()
        {
            InitializeSuffixes();
        }

        public QueueValue(IEnumerable<object> toCopy)
            : base(toCopy)
        {
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("COPY", new NoArgsSuffix<QueueValue>(() => new QueueValue(this)));
        }

        public new static QueueValue CreateQueue<T>(IEnumerable<T> toCopy)
        {
            return new QueueValue(toCopy.Cast<object>());
        }
    }
}