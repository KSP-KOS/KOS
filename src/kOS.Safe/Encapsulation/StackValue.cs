using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Serialization;
using kOS.Safe.Function;

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

        // Required for all IDumpers for them to work, but can't enforced by the interface because it's static:
        public static StackValue<T> CreateFromDump(SafeSharedObjects shared, Dump d)
        {
            var newObj = new StackValue<T>();
            newObj.LoadDump(d);
            return newObj;
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
        public override string ToStringItems(int level)
        {
            StringBuilder sb = new StringBuilder();
            string pad = string.Empty.PadRight(level * TerminalFormatter.INDENT_SPACES, ' ');
            var asArray = InnerEnumerable.ToArray();
            int i = 0;
            foreach (object item in asArray)
            {
                Structure asStructure = item as Structure;
                if (asStructure != null)
                {
                    sb.Append(string.Format("{0}[{1}] = {2}\n",
                        pad,
                        (i == 0 ? "front->" : (i == asArray.Count() ? "back ->" : "       ")),
                        asStructure.ToStringIndented(level)
                        ));
                }
                else // Hypothetically this case should not happen, but if we screwed up somewhere so it does, at least you can see something.
                {
                    sb.Append(item.ToString());
                }
                ++i;
            }
            return sb.ToString();
        }
    }

    [kOS.Safe.Utilities.KOSNomenclature("Stack", KOSToCSharp = false)] // one-way because the generic templated StackValue<T> is the canonical one.  
    public class StackValue : StackValue<Structure>
    {
        [Function("stack")]
        public class FunctionStack : SafeFunctionBase
        {
            public override void Execute(SafeSharedObjects shared)
            {
                Structure[] argArray = new Structure[CountRemainingArgs(shared)];
                for (int i = argArray.Length - 1; i >= 0; --i)
                    argArray[i] = PopStructureAssertEncapsulated(shared); // fill array in reverse order because .. stack args.
                AssertArgBottomAndConsume(shared);
                var stackValue = new StackValue(argArray.ToList());
                ReturnValue = stackValue;
            }
        }

        public StackValue()
        {
            InitializeSuffixes();
        }

        public StackValue(IEnumerable<Structure> toCopy)
            : base(toCopy)
        {
            InitializeSuffixes();
        }

        // Required for all IDumpers for them to work, but can't enforced by the interface because it's static:
        public static new StackValue CreateFromDump(SafeSharedObjects shared, Dump d)
        {
            var newObj = new StackValue();
            newObj.LoadDump(d);
            return newObj;
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