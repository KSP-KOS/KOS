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

        public StringValue(StringValue stringValue)
        {
            internalString = stringValue.ToString();
            StringInitializeSuffixes();
        }

        public StringValue(char ch)
        {
            internalString = new string(new char[] {ch});
            StringInitializeSuffixes();
        }

        public ScalarIntValue Length
        {
            get { return internalString.Length; }
        }

        public string Substring(ScalarIntValue start, ScalarIntValue count)
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

        public ScalarIntValue IndexOf(string s)
        {
            return internalString.IndexOf(s, StringComparison.OrdinalIgnoreCase);
        }

        // IndexOf with a start position.
        // This was named FindAt because IndexOfAt made little sense.
        public ScalarIntValue FindAt(string s, ScalarIntValue start)
        {
            return internalString.IndexOf(s, start, StringComparison.OrdinalIgnoreCase);
        }

        public string Insert(ScalarIntValue location, string s)
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

        public Structure GetIndex(int index)
        {
            return new StringValue(internalString[index]);
        }

        public Structure GetIndex(Structure index)
        {
            if (index is ScalarValue)
            {
                int i = Convert.ToInt32(index);  // allow expressions like (1.0) to be indexes
                return new StringValue(internalString[i]);
            }
            throw new KOSCastException(index.GetType(), typeof(int)/*So the message will say it needs integer, not just any Scalar*/);

        }

        // Required by the interface but unimplemented, because strings are immutable.
        public void SetIndex(Structure index, Structure value)
        {
            throw new KOSException("String are immutable; they can not be modified using the syntax \"SET string[1] TO 'a'\", etc.");
        }
        // Required by the interface but unimplemented, because strings are immutable.
        public void SetIndex(int index, Structure value)
        {
            throw new KOSException("String are immutable; they can not be modified using the syntax \"SET string[1] TO 'a'\", etc.");
        }

        // As the regular Split, except returning a ListValue rather than an array.
        public ListValue<StringValue> SplitToList(string separator)
        {
            string[] split = Regex.Split(internalString, Regex.Escape(separator), RegexOptions.IgnoreCase);
            ListValue<StringValue> returnList = new ListValue<StringValue>();
            foreach (string s in split)
                returnList.Add(new StringValue(s));
            return returnList;
        }

        private void StringInitializeSuffixes()
        {
            AddSuffix("LENGTH",     new NoArgsSuffix<ScalarIntValue>                           (() => Length));
            AddSuffix("SUBSTRING",  new TwoArgsSuffix<StringValue, ScalarIntValue, ScalarIntValue>             ( (one, two) => Substring(one, two)));
            AddSuffix("CONTAINS",   new OneArgsSuffix<BooleanValue, StringValue>                 (one => Contains(one)));
            AddSuffix("ENDSWITH",   new OneArgsSuffix<BooleanValue, StringValue>                 (one => EndsWith(one)));
            AddSuffix("FINDAT",     new TwoArgsSuffix<ScalarIntValue, StringValue, ScalarIntValue>             ( (one, two) => FindAt(one, two)));
            AddSuffix("INSERT",     new TwoArgsSuffix<StringValue, ScalarIntValue, StringValue>          ( (one, two) => Insert(one, two)));
            AddSuffix("FINDLASTAT", new TwoArgsSuffix<ScalarIntValue, StringValue, ScalarIntValue>             ( (one, two) => FindLastAt(one, two)));
            AddSuffix("PADLEFT",    new OneArgsSuffix<StringValue, ScalarIntValue>                  (one => PadLeft(one)));
            AddSuffix("PADRIGHT",   new OneArgsSuffix<StringValue, ScalarIntValue>                  ( one => PadRight(one)));
            AddSuffix("REMOVE",     new TwoArgsSuffix<StringValue, ScalarIntValue, ScalarIntValue>             ( (one, two) => Remove(one, two)));
            AddSuffix("REPLACE",    new TwoArgsSuffix<StringValue, StringValue, StringValue>       ( (one, two) => Replace(one, two)));
            AddSuffix("SPLIT",      new OneArgsSuffix<ListValue<StringValue>, StringValue>    (one => SplitToList(one)));
            AddSuffix("STARTSWITH", new OneArgsSuffix<BooleanValue, StringValue>                 (one => StartsWith(one)));
            AddSuffix("TOLOWER",    new NoArgsSuffix<StringValue>                        (() => ToLower()));
            AddSuffix("TOUPPER",    new NoArgsSuffix<StringValue>                        (() => ToUpper()));
            AddSuffix("TRIM",       new NoArgsSuffix<StringValue>                        (() => Trim()));
            AddSuffix("TRIMEND",    new NoArgsSuffix<StringValue>                        (() => TrimEnd()));
            AddSuffix("TRIMSTART",  new NoArgsSuffix<StringValue>                        (() => TrimStart()));

            // Aliased "IndexOf" with "Find" to match "FindAt" (since IndexOfAt doesn't make sense, but I wanted to stick with common/C# names when possible)
            AddSuffix(new[] { "INDEXOF",     "FIND" },     new OneArgsSuffix<ScalarIntValue, StringValue>   ( one => IndexOf(one)));
            AddSuffix(new[] { "LASTINDEXOF", "FINDLAST" }, new OneArgsSuffix<ScalarIntValue, StringValue>   ( s => LastIndexOf(s)));

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

        // Implicitly converts to a string (i.e., unboxes itself automatically)
        public static implicit operator string(StringValue value)
        {
            return value.internalString;
        }

        public static implicit operator StringValue(string value)
        {
            return new StringValue(value);
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
