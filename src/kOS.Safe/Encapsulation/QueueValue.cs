using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Serialization;

namespace kOS.Safe.Encapsulation
{
    [kOS.Safe.Utilities.KOSNomenclature("Queue")]
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
            return InnerEnumerable.Dequeue();
        }

        public void Push(T val)
        {
            InnerEnumerable.Enqueue(val);
        }

        public override Dump Dump()
        {
            var result = new DumpWithHeader
            {
                Header = "QUEUE of " + InnerEnumerable.Count() + " items:"
            };

            result.Add(kOS.Safe.Dump.Items, InnerEnumerable.Cast<object>().ToList());

            return result;
        }

        public override void LoadDump(Dump dump)
        {
            InnerEnumerable.Clear();

            List<object> values = (List<object>)dump[kOS.Safe.Dump.Items];

            foreach (object item in values)
            {
                InnerEnumerable.Enqueue((T)FromPrimitive(item));
            }
        }

        private void QueueInitializeSuffixes()
        {
            AddSuffix("COPY",     new NoArgsSuffix<QueueValue<T>>       (() => new QueueValue<T>(this)));

            AddSuffix("PUSH",     new OneArgsSuffix<T>                  (toPush => InnerEnumerable.Enqueue(toPush)));
            AddSuffix("POP",      new NoArgsSuffix<T>                   (() => InnerEnumerable.Dequeue()));
            AddSuffix("PEEK",     new NoArgsSuffix<T>                   (() => InnerEnumerable.Peek()));
            AddSuffix("CLEAR",    new NoArgsVoidSuffix                  (() => InnerEnumerable.Clear()));
        }

        public static QueueValue<T> CreateQueue<TU>(IEnumerable<TU> list)
        {
            return new QueueValue<T>(list.Cast<T>());
        }
    }

    [kOS.Safe.Utilities.KOSNomenclature("Queue", KOSToCSharp = false)] // one-way because the generic templated QueueValue<T> is the canonical one.  
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