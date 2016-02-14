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
            
            // This conversion is needed because TerminalFormatter.WriteIndented() demands to only
            // work with exactly List<object> and bombs out on List<Structure>'s:
            List<object> list = new List<object>();
            foreach (object entry in Collection.ToList())
                list.Add(entry);

            result.Add(kOS.Safe.Dump.Items, list);

            return result;
        }

        public override void LoadDump(Dump dump)
        {
            Collection.Clear();

            List<object> values = (List<object>)dump[kOS.Safe.Dump.Items];

            foreach (object item in values)
            {
                Collection.Enqueue((T)FromPrimitive(item));
            }
        }

        private void QueueInitializeSuffixes()
        {
            AddSuffix("COPY",     new NoArgsSuffix<QueueValue<T>>       (() => new QueueValue<T>(this)));
            AddSuffix("LENGTH",   new NoArgsSuffix<ScalarValue>                 (() => Collection.Count));
            AddSuffix("PUSH",     new OneArgsSuffix<T>                  (toPush => Collection.Enqueue(toPush)));
            AddSuffix("POP",      new NoArgsSuffix<T>                   (() => Collection.Dequeue()));
            AddSuffix("PEEK",     new NoArgsSuffix<T>                   (() => Collection.Peek()));
            AddSuffix("CLEAR",    new NoArgsVoidSuffix                      (() => Collection.Clear()));
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