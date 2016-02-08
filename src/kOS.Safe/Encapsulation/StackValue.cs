using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Serialization;

namespace kOS.Safe.Encapsulation
{
    public class StackValue<T> : EnumerableValue<T, Stack<T>>
        where T : Structure
    {
        public StackValue() : this(new Stack<T>())
        {
        }

        public StackValue(IEnumerable<T> stackValue) : base(new Stack<T>(stackValue))
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

        public override Dump Dump()
        {
            var result = new DumpWithHeader
            {
                Header = "STACK of " + Collection.Count() + " items:"
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

            List<object> values = ((List<object>)dump[kOS.Safe.Dump.Items]);

            values.Reverse();

            foreach (object item in values)
            {
                Collection.Push((T)Structure.FromPrimitive(item));
            }
        }


        private void StackInitializeSuffixes()
        {
            AddSuffix("COPY",     new NoArgsSuffix<StackValue<T>>       (() => new StackValue<T>(this)));
            AddSuffix("LENGTH",   new NoArgsSuffix<ScalarValue>      (() => Collection.Count));
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

    public class StackValue : StackValue<Structure>
    {
        public StackValue()
        {
            InitializeSuffixes();
        }

        public StackValue(IEnumerable<Structure> toCopy)
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
            return new StackValue(toCopy.Select(x => Structure.FromPrimitiveWithAssert(x)));
        }
    }
}