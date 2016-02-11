using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Serialization;

namespace kOS.Safe.Encapsulation
{
    public class QueueValue<T> : EnumerableValue<T, Queue<T>>
        where T : Structure
    {
        public QueueValue() : this(new Queue<T>())
        {
        }

        public QueueValue(IEnumerable<T> queueValue) : base("QUEUE", new Queue<T>(queueValue))
        {
            QueueInitializeSuffixes();
        }

        public T Pop()
        {
            return InnerEnum.Dequeue();
        }

        public void Push(T val)
        {
            InnerEnum.Enqueue(val);
        }

        public override Dump Dump()
        {
            var result = new DumpWithHeader
            {
                Header = "QUEUE of " + InnerEnum.Count() + " items:"
            };

            result.Add(kOS.Safe.Dump.Items, InnerEnum.Cast<object>().ToList());

            return result;
        }

        public override void LoadDump(Dump dump)
        {
            InnerEnum.Clear();

            List<object> values = (List<object>)dump[kOS.Safe.Dump.Items];

            foreach (object item in values)
            {
                InnerEnum.Enqueue((T)FromPrimitive(item));
            }
        }

        private void QueueInitializeSuffixes()
        {
            AddSuffix("COPY",     new NoArgsSuffix<QueueValue<T>>       (() => new QueueValue<T>(this)));

            AddSuffix("PUSH",     new OneArgsSuffix<T>                  (toPush => InnerEnum.Enqueue(toPush)));
            AddSuffix("POP",      new NoArgsSuffix<T>                   (() => InnerEnum.Dequeue()));
            AddSuffix("PEEK",     new NoArgsSuffix<T>                   (() => InnerEnum.Peek()));
            AddSuffix("CLEAR",    new NoArgsVoidSuffix                  (() => InnerEnum.Clear()));
        }

        public static QueueValue<T> CreateQueue<TU>(IEnumerable<TU> list)
        {
            return new QueueValue<T>(list.Cast<T>());
        }
    }

    public class QueueValue : QueueValue<Structure>
    {
        public QueueValue()
        {
            InitializeSuffixes();
        }

        public QueueValue(IEnumerable<Structure> toCopy)
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
            return new QueueValue(toCopy.Select(x => FromPrimitiveWithAssert(x)));
        }
    }
}