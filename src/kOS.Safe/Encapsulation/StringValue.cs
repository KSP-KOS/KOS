using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS.Safe.Encapsulation
{
    /// <summary>
    /// The class is a simple wrapper around the string class to 
    /// implement the Structure and IIndexable interface on
    /// strings. Currently, strings are only boxed with this
    /// class temporarily when suffix/indexing support is
    /// necessary.
    /// 
    /// </summary>
    public class StringValue : Structure, IIndexable
    {
        private readonly string internalString;

        public StringValue(): 
            this ("")
        {
        }

        public StringValue(string stringValue)
        {
            this.internalString = stringValue;
            StringInitializeSuffixes();
        }

        public int Length
        {
            get { return internalString.Length; }
        }

        public String Substring(int start, int count)
        {
            return internalString.Substring(start, count);
        }

        public bool Contains(String s)
        {
            return internalString.Contains(s);
        }

        public bool EndsWith(String s)
        {
            return internalString.EndsWith(s);
        }

        // To match C# naming
        public int IndexOf(String s)
        {
            return internalString.IndexOf(s);
        }

        // To be consistant with FindAt, below
        public int Find(String s)
        {
            return internalString.IndexOf(s);
        }

        // IndexOf with a start position.
        // This was named FindAt because IndexOfAt made little sense.
        public int FindAt(String s, int start)
        {
            return internalString.IndexOf(s, start);
        }

        public String Insert(int location, String s)
        {
            return internalString.Insert(location, s);
        }

        public int LastIndexOf(String s)
        {
            return internalString.LastIndexOf(s);
        }

        public int FindLast(String s)
        {
            return internalString.LastIndexOf(s);
        }

        public int FindLastAt(String s, int start)
        {
            return internalString.LastIndexOf(s, start);
        }

        public String PadLeft(int width)
        {
            return internalString.PadLeft(width);
        }

        public String PadRight(int width)
        {
            return internalString.PadRight(width);
        }

        public String Remove(int start, int count)
        {
            return internalString.Remove(start, count);
        }

        public String Replace(String oldString, String newString)
        {
            return internalString.Replace(oldString, newString);
        }

        public String ToLower()
        {
            return internalString.ToLower();
        }

        public String ToUpper()
        {
            return internalString.ToUpper();
        }

        public bool StartsWith(String s)
        {
            return internalString.StartsWith(s);
        }

        public String Trim()
        {
            return internalString.Trim();
        }

        public String TrimEnd()
        {
            return internalString.TrimEnd();
        }

        public String TrimStart()
        {
            return internalString.TrimStart();
        }

        public object GetIndex(int index)
        {
            return internalString[index];
        }

        // Required by the interface but unimplemented, because strings are immutable.
        public void SetIndex(int index, object value)
        {
            throw new KOSException("String are immutable; they can not be modified using the syntax \"SET string[1] TO 'a'\", etc.");
        }

        // As the regular Split, except returning a ListValue rather than an array.
        public ListValue<String> SplitToList(String separator)
        {
            String[] split = internalString.Split(new string[] { separator }, StringSplitOptions.None);
            return new ListValue<String>(split);
        }

        private void StringInitializeSuffixes()
        {
            AddSuffix("LENGTH",     new NoArgsSuffix<int>                           (() => Length));
            AddSuffix("SUBSTRING",  new TwoArgsSuffix<String, int, int>             (Substring));
            AddSuffix("CONTAINS",   new OneArgsSuffix<bool, String>                 (Contains));
            AddSuffix("ENDSWITH",   new OneArgsSuffix<bool, String>                 (EndsWith));
            AddSuffix("FINDAT",     new TwoArgsSuffix<int, String, int>             (FindAt));
            AddSuffix("INSERT",     new TwoArgsSuffix<String, int, String>          (Insert));
            AddSuffix("FINDLASTAT", new TwoArgsSuffix<int, String, int>             (FindLastAt));
            AddSuffix("PADLEFT",    new OneArgsSuffix<String, int>                  (PadLeft));
            AddSuffix("PADRIGHT",   new OneArgsSuffix<String, int>                  (PadRight));
            AddSuffix("REMOVE",     new TwoArgsSuffix<String, int, int>             (Remove));
            AddSuffix("REPLACE",    new TwoArgsSuffix<String, String, String>       (Replace));
            AddSuffix("SPLIT",      new OneArgsSuffix<ListValue<String>, String>    (SplitToList));
            AddSuffix("STARTSWITH", new OneArgsSuffix<bool, String>                 (StartsWith));
            AddSuffix("TOLOWER",    new NoArgsSuffix<String>                        (ToLower));
            AddSuffix("TOUPPER",    new NoArgsSuffix<String>                        (ToUpper));
            AddSuffix("TRIM",       new NoArgsSuffix<String>                        (Trim));
            AddSuffix("TRIMEND",    new NoArgsSuffix<String>                        (TrimEnd));
            AddSuffix("TRIMSTART",  new NoArgsSuffix<String>                        (TrimStart));

            // Aliased "IndexOf" with "Find" to match "FindAt" (since IndexOfAt doesn't make sense, but I wanted to stick with common/C# names when possible)
            AddSuffix(new string[] { "INDEXOF",     "FIND" },     new OneArgsSuffix<int, String>   (IndexOf));
            AddSuffix(new string[] { "LASTINDEXOF", "FINDLAST" }, new OneArgsSuffix<int, String>   (LastIndexOf));

        }

        // Implicitly converts to a string (i.e., unboxes itself automatically)
        public static implicit operator string(StringValue value)
        {
            return value.internalString;
        }
    }
}
