using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Safe.Encapsulation
{
    public class StackValue<T> : EnumerableValue<T, Stack<T>>
    {
        public StackValue() : this(new Stack<T>())
        {
        }

        public StackValue(IEnumerable<T> stackValue) : base("STACK", new Stack<T>(stackValue))
        {
            StackInitializeSuffixes();
        }

        public override IEnumerator<T> GetEnumerator()
        {
            return Collection.Reverse().GetEnumerator();
        }

        public override int Count
        {
            get { return Collection.Count; }
        }

        public T Pop()
        {
            return Collection.Pop();
        }

        public void Push(T val)
        {
            Collection.Push(val);
        }

        public override void LoadDump(IDictionary<object, object> dump)
        {
            Collection.Clear();

            foreach (object item in dump.Values)
            {
                Collection.Push((T)Structure.FromPrimitive(item));
            }
        }


        private void StackInitializeSuffixes()
        {
            AddSuffix("COPY",     new NoArgsSuffix<StackValue<T>>       (() => new StackValue<T>(this)));
            AddSuffix("LENGTH",   new NoArgsSuffix<int>                 (() => Collection.Count));
            AddSuffix("PUSH",     new OneArgsSuffix<T>                  (toPush => Collection.Push(toPush)));
            AddSuffix("POP",      new NoArgsSuffix<T>                   (() => Collection.Pop()));
            AddSuffix("PEEK",     new NoArgsSuffix<T>                   (() => Collection.Peek()));
            AddSuffix("CLEAR",    new NoArgsSuffix                      (() => Collection.Clear()));
        }

        public static StackValue<T> CreateStack<TU>(IEnumerable<TU> list)
        {
            return new StackValue<T>(list.Cast<T>());
        }
    }

    public class StackValue : StackValue<object>
    {
        public StackValue()
        {
            InitializeSuffixes();
        }

        public StackValue(IEnumerable<object> toCopy)
            : base(toCopy)
        {
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("COPY", new NoArgsSuffix<StackValue>(() => new StackValue(this)));
        }

        public new static StackValue CreateStack<T>(IEnumerable<T> toCopy)
        {
            return new StackValue(toCopy.Cast<object>());
        }
    }
}