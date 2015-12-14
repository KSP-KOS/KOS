using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace kOS.Safe.Encapsulation
{
    public class QueueValue<T> : EnumerableValue<T, Queue<T>>
    {
        public QueueValue() : this(new Queue<T>())
        {
        }

        public QueueValue(IEnumerable<T> queueValue) : base("QUEUE", new Queue<T>(queueValue))
        {
            QueueInitializeSuffixes();
        }

        public override int Count
        {
            get { return collection.Count; }
        }

        public T Pop()
        {
            return collection.Dequeue();
        }

        public void Push(T val)
        {
            collection.Enqueue(val);
        }
            
        public override void LoadDump(IDictionary<object, object> dump)
        {
            collection.Clear();

            foreach (object item in dump.Values)
            {
                collection.Enqueue((T)item);
            }
        }

        private void QueueInitializeSuffixes()
        {
            AddSuffix("COPY",     new NoArgsSuffix<QueueValue<T>>       (() => new QueueValue<T>(this)));
            AddSuffix("LENGTH",   new NoArgsSuffix<int>                 (() => collection.Count));
            AddSuffix("PUSH",     new OneArgsSuffix<T>                  (toPush => collection.Enqueue(toPush)));
            AddSuffix("POP",      new NoArgsSuffix<T>                   (() => collection.Dequeue()));
            AddSuffix("PEEK",     new NoArgsSuffix<T>                   (() => collection.Peek()));
            AddSuffix("CLEAR",    new NoArgsSuffix                      (() => collection.Clear()));
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