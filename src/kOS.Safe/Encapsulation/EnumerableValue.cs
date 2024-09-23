using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Serialization;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace kOS.Safe.Encapsulation
{
    [kOS.Safe.Utilities.KOSNomenclature("Enumerable")]
    public abstract class EnumerableValue<T, TE> : SerializableStructure, IEnumerable<T>
        where TE : IEnumerable<T>
        where T : Structure
    {
        protected TE InnerEnumerable { get; private set; }
        private readonly string label;

        protected EnumerableValue(string label, TE enumerable)
        {
            this.label = label;
            InnerEnumerable = enumerable;

            InitializeEnumerableSuffixes();
        }

        public virtual IEnumerator<T> GetEnumerator()
        {
            return InnerEnumerable.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Contains(T item)
        {
            return InnerEnumerable.Contains(item);
        }

        public override string ToStringIndented(int level)
        {
            if (level >= TerminalFormatter.MAX_INDENT_LEVEL)
                return "<<TOSTRING REFUSES TO RECURSE DEEPER THAN NESTING LEVEL " + level + ">>";

            StringBuilder sb = new StringBuilder();
            string pad = string.Empty.PadRight(level * TerminalFormatter.INDENT_SPACES, ' ');

            int cnt = this.Count();
            if (cnt == 0)
                sb.Append(string.Format("{0} (empty)", KOSName));
            else if (cnt == 1)
                sb.Append(string.Format("{0} of 1 item:", KOSName));
            else
                sb.Append(string.Format("{0} of {1} items:", KOSName, cnt));

            sb.Append(string.Format("\n{0}",ToStringItems(level + 1)));
            return sb.ToString();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            int cnt = this.Count();
            if (cnt == 0)
                sb.Append(string.Format("{0} (empty)", KOSName));
            else if (cnt == 1)
                sb.Append(string.Format("{0} of 1 item:", KOSName));
            else
                sb.Append(string.Format("{0} of {1} items:", KOSName, cnt));

            sb.Append(string.Format("\n{0}",ToStringItems(1)));
            return sb.ToString();
        }

        /// <summary>
        /// Print the inner items (not the header) of a container.  Override this and this
        /// enumerable structure will use it in its ToString() and its ToStringIndented().
        /// IMPORTANT: If your enumerable contains zero things, then return empty string, not even
        /// a newline.  If your enumerable contains at least one thing, then print
        /// a line break at the end of each thing.
        /// </summary>
        /// <param name="level">you must pad all lines with this level of indent*TerminalFormatter.INDENT_SPACES</param>
        /// <returns></returns>
        public abstract string ToStringItems(int level);

        public override Dump Dump()
        {
            var result = new DumpWithHeader
            {
                Header = label + " of " + InnerEnumerable.Count() + " items:"
            };

            result.Add(kOS.Safe.Dump.Items, InnerEnumerable.Cast<object>().ToList());

            return result;
        }

        private void InitializeEnumerableSuffixes()
        {
            AddSuffix("ITERATOR",        new NoArgsSuffix<Enumerator>(() => new Enumerator(InnerEnumerable.GetEnumerator())));
            AddSuffix("REVERSEITERATOR", new NoArgsSuffix<Enumerator>(() => new Enumerator(Enumerable.Reverse(InnerEnumerable).GetEnumerator())));
            AddSuffix("LENGTH",          new NoArgsSuffix<ScalarValue>(() => InnerEnumerable.Count()));
            AddSuffix("CONTAINS",        new OneArgsSuffix<BooleanValue, T>((n) => Contains(n)));
            AddSuffix("EMPTY",           new NoArgsSuffix<BooleanValue>(() => !InnerEnumerable.Any()));
            AddSuffix("DUMP",            new NoArgsSuffix<StringValue>(() => new StringValue(ToString())));
        }
    }
}
