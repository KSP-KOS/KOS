using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Serialization;
using kOS.Safe.Function;

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

        // Required for all IDumpers for them to work, but can't enforced by the interface because it's static:
        public static QueueValue<T> CreateFromDump(SafeSharedObjects shared, Dump d)
        {
            var newObj = new QueueValue<T>();
            newObj.LoadDump(d);
            return newObj;
        }

        public T Pop()
        {
            return InnerEnumerable.Dequeue();
        }

        public void Push(T val)
        {
            InnerEnumerable.Enqueue(val);
        }

        public Dump Dump()
        {
            /*
            Dump result = base.Dump();
            result.Annotations.Add(0, "<-- front");
            return result;
            */
            return null;
        }
        public void LoadDump(Dump dump)
        {
            /*
            InnerEnumerable.Clear();

            List<object> values = (List<object>)dump[kOS.Safe.Dump.Items];

            foreach (object item in values)
            {
                InnerEnumerable.Enqueue((T)FromPrimitive(item));
            }*/
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
        [Function("queue")]
        public class FunctionQueue : SafeFunctionBase
        {
            public override void Execute(SafeSharedObjects shared)
            {
                Structure[] argArray = new Structure[CountRemainingArgs(shared)];
                for (int i = argArray.Length - 1; i >= 0; --i)
                    argArray[i] = PopStructureAssertEncapsulated(shared); // fill array in reverse order because .. stack args.
                AssertArgBottomAndConsume(shared);
                var queueValue = new QueueValue(argArray.ToList());
                ReturnValue = queueValue;
            }
        }

        public QueueValue()
        {
            InitializeSuffixes();
        }

        public QueueValue(IEnumerable<Structure> toCopy)
            : base(toCopy)
        {
            InitializeSuffixes();
        }

        // Required for all IDumpers for them to work, but can't enforced by the interface because it's static:
        public static new QueueValue CreateFromDump(SafeSharedObjects shared, Dump d)
        {
            var newObj = new QueueValue();
            newObj.LoadDump(d);
            return newObj;
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