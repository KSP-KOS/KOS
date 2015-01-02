using System.Collections.Generic;
using System.Linq;

namespace kOS.Safe.Encapsulation
{
    public class SuffixedList<T> : ListValue<T> where T : ISuffixed
    {
        public SuffixedList()
        {
        }

        public SuffixedList(IEnumerable<T> toCopy) : base(toCopy)
        {
        }

        public static ListValue CreateList(List<T> toCopy)
        {
            return new ListValue(toCopy.Cast<object>());
        }
    }
}