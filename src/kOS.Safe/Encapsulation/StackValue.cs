using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Serialization;

namespace kOS.Safe.Encapsulation
{
    [kOS.Safe.Utilities.KOSNomenclature("Stack")]
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
            return InnerEnumerable.Reverse().GetEnumerator();
        }

        public T Pop()
        {
            return InnerEnumerable.Pop();
        }

        public void Push(T val)
        {
            InnerEnumerable.Push(val);
        }

        public override void LoadDump(Dump dump)
        {
            InnerEnumerable.Clear();

            List<object> values = ((List<object>)dump[kOS.Safe.Dump.Items]);

            values.Reverse();

            foreach (object item in values)
            {
                InnerEnumerable.Push((T)Structure.FromPrimitive(item));
            }
        }

        private void StackInitializeSuffixes()
        {
            AddSuffix("COPY",     new NoArgsSuffix<StackValue<T>>       (() => new StackValue<T>(this)));
            AddSuffix("PUSH",     new OneArgsSuffix<T>                  (toPush => InnerEnumerable.Push(toPush)));
            AddSuffix("POP",      new NoArgsSuffix<T>                   (() => InnerEnumerable.Pop()));
            AddSuffix("PEEK",     new NoArgsSuffix<T>                   (() => InnerEnumerable.Peek()));
            AddSuffix("CLEAR",    new NoArgsVoidSuffix                  (() => InnerEnumerable.Clear()));
        }

        public static StackValue<T> CreateStack<TU>(IEnumerable<TU> list)
        {
            return new StackValue<T>(list.Cast<T>());
        }
    }

    [kOS.Safe.Utilities.KOSNomenclature("Stack", KOSToCSharp = false)] // one-way because the generic templated StackValue<T> is the canonical one.  
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