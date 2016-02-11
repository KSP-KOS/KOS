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

        public StackValue(IEnumerable<T> stackValue) : base("STACK", new Stack<T>(stackValue))
        {
            StackInitializeSuffixes();
        }

        public override IEnumerator<T> GetEnumerator()
        {
            return InnerEnum.Reverse().GetEnumerator();
        }

        public T Pop()
        {
            return InnerEnum.Pop();
        }

        public void Push(T val)
        {
            InnerEnum.Push(val);
        }

        public override Dump Dump()
        {
            var result = new DumpWithHeader
            {
                Header = "STACK of " + InnerEnum.Count() + " items:"
            };

            result.Add(kOS.Safe.Dump.Items, InnerEnum.Cast<object>().ToList());

            return result;
        }

        public override void LoadDump(Dump dump)
        {
            InnerEnum.Clear();

            List<object> values = ((List<object>)dump[kOS.Safe.Dump.Items]);

            values.Reverse();

            foreach (object item in values)
            {
                InnerEnum.Push((T)Structure.FromPrimitive(item));
            }
        }


        private void StackInitializeSuffixes()
        {
            AddSuffix("COPY",     new NoArgsSuffix<StackValue<T>>       (() => new StackValue<T>(this)));
            AddSuffix("PUSH",     new OneArgsSuffix<T>                  (toPush => InnerEnum.Push(toPush)));
            AddSuffix("POP",      new NoArgsSuffix<T>                   (() => InnerEnum.Pop()));
            AddSuffix("PEEK",     new NoArgsSuffix<T>                   (() => InnerEnum.Peek()));
            AddSuffix("CLEAR",    new NoArgsVoidSuffix                  (() => InnerEnum.Clear()));
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