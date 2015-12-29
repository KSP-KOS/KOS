using System;
using System.Globalization;
using System.Text.RegularExpressions;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;

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
    public class StringValue : Structure, IIndexable, IConvertible, ISerializableValue
    {
        private readonly string internalString;

        public StringValue(): 
            this (string.Empty)
        {
        }

        public StringValue(string stringValue)
        {
            internalString = stringValue;
            StringInitializeSuffixes();
        }

        public int Length
        {
            get { return internalString.Length; }
        }

        public string Substring(int start, int count)
        {
            return internalString.Substring(start, count);
        }

        public bool Contains(string s)
        {
            return internalString.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public bool EndsWith(string s)
        {
            return internalString.EndsWith(s, StringComparison.OrdinalIgnoreCase);
        }

        public int IndexOf(string s)
        {
            return internalString.IndexOf(s, StringComparison.OrdinalIgnoreCase);
        }

        // IndexOf with a start position.
        // This was named FindAt because IndexOfAt made little sense.
        public int FindAt(string s, int start)
        {
            return internalString.IndexOf(s, start, StringComparison.OrdinalIgnoreCase);
        }

        public string Insert(int location, string s)
        {
            return internalString.Insert(location, s);
        }

        public int LastIndexOf(string s)
        {
            return internalString.LastIndexOf(s, StringComparison.OrdinalIgnoreCase);
        }

        public int FindLastAt(string s, int start)
        {
            return internalString.LastIndexOf(s, start, StringComparison.OrdinalIgnoreCase);
        }

        public string PadLeft(int width)
        {
            return internalString.PadLeft(width);
        }

        public string PadRight(int width)
        {
            return internalString.PadRight(width);
        }

        public string Remove(int start, int count)
        {
            return internalString.Remove(start, count);
        }

        public string Replace(string oldString, string newString)
        {
            return Regex.Replace(internalString, Regex.Escape(oldString), newString, RegexOptions.IgnoreCase);
        }

        public string ToLower()
        {
            return internalString.ToLower();
        }

        public string ToUpper()
        {
            return internalString.ToUpper();
        }

        public bool StartsWith(string s)
        {
            return internalString.StartsWith(s, StringComparison.OrdinalIgnoreCase);
        }

        public string Trim()
        {
            return internalString.Trim();
        }

        public string TrimEnd()
        {
            return internalString.TrimEnd();
        }

        public string TrimStart()
        {
            return internalString.TrimStart();
        }

        public object GetIndex(object index)
        {
            if (index is double || index is float)
            {
                index = Convert.ToInt32(index);  // allow expressions like (1.0) to be indexes
            }
            if (!(index is int)) throw new Exception("The index must be an integer number");

            return internalString[(int)index].ToString();
        }

        // Required by the interface but unimplemented, because strings are immutable.
        public void SetIndex(object index, object value)
        {
            throw new KOSException("String are immutable; they can not be modified using the syntax \"SET string[1] TO 'a'\", etc.");
        }

        // As the regular Split, except returning a ListValue rather than an array.
        public ListValue<string> SplitToList(string separator)
        {
            string[] split = Regex.Split(internalString, Regex.Escape(separator), RegexOptions.IgnoreCase);
            return new ListValue<string>(split);
        }

        private void StringInitializeSuffixes()
        {
            AddSuffix("LENGTH",     new NoArgsSuffix<int>                           (() => Length));
            AddSuffix("SUBSTRING",  new TwoArgsSuffix<string, int, int>             (Substring));
            AddSuffix("CONTAINS",   new OneArgsSuffix<bool, string>                 (Contains));
            AddSuffix("ENDSWITH",   new OneArgsSuffix<bool, string>                 (EndsWith));
            AddSuffix("FINDAT",     new TwoArgsSuffix<int, string, int>             (FindAt));
            AddSuffix("INSERT",     new TwoArgsSuffix<string, int, string>          (Insert));
            AddSuffix("FINDLASTAT", new TwoArgsSuffix<int, string, int>             (FindLastAt));
            AddSuffix("PADLEFT",    new OneArgsSuffix<string, int>                  (PadLeft));
            AddSuffix("PADRIGHT",   new OneArgsSuffix<string, int>                  (PadRight));
            AddSuffix("REMOVE",     new TwoArgsSuffix<string, int, int>             (Remove));
            AddSuffix("REPLACE",    new TwoArgsSuffix<string, string, string>       (Replace));
            AddSuffix("SPLIT",      new OneArgsSuffix<ListValue<string>, string>    (SplitToList));
            AddSuffix("STARTSWITH", new OneArgsSuffix<bool, string>                 (StartsWith));
            AddSuffix("TOLOWER",    new NoArgsSuffix<string>                        (ToLower));
            AddSuffix("TOUPPER",    new NoArgsSuffix<string>                        (ToUpper));
            AddSuffix("TRIM",       new NoArgsSuffix<string>                        (Trim));
            AddSuffix("TRIMEND",    new NoArgsSuffix<string>                        (TrimEnd));
            AddSuffix("TRIMSTART",  new NoArgsSuffix<string>                        (TrimStart));

            // Aliased "IndexOf" with "Find" to match "FindAt" (since IndexOfAt doesn't make sense, but I wanted to stick with common/C# names when possible)
            AddSuffix(new[] { "INDEXOF",     "FIND" },     new OneArgsSuffix<int, string>   (IndexOf));
            AddSuffix(new[] { "LASTINDEXOF", "FINDLAST" }, new OneArgsSuffix<int, string>   (LastIndexOf));

        }

        public static bool operator ==(StringValue val1, StringValue val2)
        {
            if ((object)val1 == null) return ((object)val2 == null);
            return val1.Equals(val2);
        }

        public static bool operator !=(StringValue val1, StringValue val2)
        {
            return !(val1 == val2);
        }

        public static bool operator ==(StringValue val1, string val2)
        {
            if (val1 == null) return val2 == null;
            return val1.Equals(val2);
        }

        public static bool operator ==(string val1, StringValue val2)
        {
            if (val2 == null) return val1 == null;
            return val2.Equals(val1);
        }

        public static bool operator !=(StringValue val1, string val2)
        {
            return !(val1 == val2);
        }

        public static bool operator !=(string val1, StringValue val2)
        {
            return !(val1 == val2);
        }

        // Implicitly converts to a string (i.e., unboxes itself automatically)
        public static implicit operator string(StringValue value)
        {
            return value.internalString;
        }

        public static StringValue operator +(StringValue val1, StringValue val2)
        {
            return new StringValue(val1.ToString() + val2.ToString());
        }

        public static StringValue operator +(StringValue val1, Structure val2)
        {
            return new StringValue(val1.ToString() + val2.ToString());
        }

        public static StringValue operator +(Structure val1, StringValue val2)
        {
            return new StringValue(val1.ToString() + val2.ToString());
        }

        public override string ToString()
        {
            return this;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj is StringValue || obj is string)
            {
                return String.Equals(internalString, obj.ToString(), StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return internalString.GetHashCode();
        }

        TypeCode IConvertible.GetTypeCode()
        {
            return TypeCode.Object;
        }

        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            if (string.IsNullOrEmpty(internalString)) return false;
            return true;
        }

        byte IConvertible.ToByte(IFormatProvider provider)
        {
            throw new KOSCastException(typeof(StringValue), typeof(byte));
        }

        char IConvertible.ToChar(IFormatProvider provider)
        {
            throw new KOSCastException(typeof(StringValue), typeof(char));
        }

        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            throw new KOSCastException(typeof(StringValue), typeof(DateTime));
        }

        decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            throw new KOSCastException(typeof(StringValue), typeof(Decimal));
        }

        double IConvertible.ToDouble(IFormatProvider provider)
        {
            throw new KOSCastException(typeof(StringValue), typeof(Double));
        }

        short IConvertible.ToInt16(IFormatProvider provider)
        {
            throw new KOSCastException(typeof(StringValue), typeof(Int16));
        }

        int IConvertible.ToInt32(IFormatProvider provider)
        {
            throw new KOSCastException(typeof(StringValue), typeof(Int32));
        }

        long IConvertible.ToInt64(IFormatProvider provider)
        {
            throw new KOSCastException(typeof(StringValue), typeof(Int64));
        }

        sbyte IConvertible.ToSByte(IFormatProvider provider)
        {
            throw new KOSCastException(typeof(StringValue), typeof(SByte));
        }

        float IConvertible.ToSingle(IFormatProvider provider)
        {
            throw new KOSCastException(typeof(StringValue), typeof(Single));
        }

        string IConvertible.ToString(IFormatProvider provider)
        {
            return internalString;
        }

        object IConvertible.ToType(Type conversionType, IFormatProvider provider)
        {
            return Convert.ChangeType(internalString, conversionType);
        }

        ushort IConvertible.ToUInt16(IFormatProvider provider)
        {
            throw new KOSCastException(typeof(StringValue), typeof(UInt16));
        }

        uint IConvertible.ToUInt32(IFormatProvider provider)
        {
            throw new KOSCastException(typeof(StringValue), typeof(UInt32));
        }

        ulong IConvertible.ToUInt64(IFormatProvider provider)
        {
            throw new KOSCastException(typeof(StringValue), typeof(UInt64));
        }
    }
}
