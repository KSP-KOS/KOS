using System;
using System.Globalization;
using System.Text.RegularExpressions;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;
using kOS.Safe.Serialization;
using System.Collections.Generic;
using System.Collections;

namespace kOS.Safe.Encapsulation
{
    /// <summary>
    /// The class is a simple wrapper around the string class to 
    /// implement the Structure and IIndexable interface on
    /// strings. Currently, strings are only boxed with this
    /// class temporarily when suffix/indexing support is
    /// necessary.
    /// </summary>
    [KOSNomenclature("String")]
    public class StringValue : PrimitiveStructure, IIndexable, IConvertible, IEnumerable<string>
    {
        private readonly string internalString;

        public static StringValue Empty { get; } = new StringValue();
        public static StringValue None { get; } = new StringValue("None");

        public StringValue(): 
            this (string.Empty)
        {
        }

        public StringValue(string stringValue)
        {
            internalString = stringValue;
            RegisterInitializer(StringInitializeSuffixes);
        }

        public StringValue(StringValue stringValue)
        {
            internalString = stringValue.ToString();
            RegisterInitializer(StringInitializeSuffixes);
        }

        public StringValue(char ch)
        {
            internalString = new string(new char[] {ch});
            RegisterInitializer(StringInitializeSuffixes);
        }

        public override object ToPrimitive()
        {
            return ToString();
        }

        public ScalarValue Length
        {
            get { return internalString.Length; }
        }

        public string Substring(ScalarValue start, ScalarValue count)
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

        public ScalarValue IndexOf(string s)
        {
            return internalString.IndexOf(s, StringComparison.OrdinalIgnoreCase);
        }

        // IndexOf with a start position.
        // This was named FindAt because IndexOfAt made little sense.
        public ScalarValue FindAt(string s, ScalarValue start)
        {
            return internalString.IndexOf(s, start, StringComparison.OrdinalIgnoreCase);
        }

        public string Insert(ScalarValue location, string s)
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
        
        /// <summary>
        /// A wrapper around ToScalar to handle the fact that a kOS suffix can't
        /// handle being called with zero or one args (optional arg), but can handle
        /// a var-args list like this:
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public ScalarValue ToScalarVarArgsWrapper(params Structure [] args)
        {
            if (args.Length > 1)
                throw new KOSArgumentMismatchException(1, args.Length, "TONUMBER must be called with zero or one argument, no more.");
            if (args.Length == 0)
                return ToScalar();
            else
            {
                return ToScalar((ScalarValue)args[0]); // should throw error if args[0] isn't ScalarValue.
            }
        }
        
        /// <summary>
        /// Parse the string into a number
        /// </summary>
        /// <param name="defaultIfError">If the string parse fails, return this value instead.  Note that if
        /// this optional value is left off, a KOSexception will be thrown on parsing errors instead.</param>
        /// <returns></returns>
        public ScalarValue ToScalar(ScalarValue defaultIfError = null)
        {
            ScalarValue result;

            if (ScalarValue.TryParse(internalString, out result))
            {
                return result;
            }
            else if (defaultIfError != null)
            {
                return defaultIfError;
            }

            throw new KOSNumberParseException(internalString);
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
            throw new KOSCastException(index.GetType(), typeof(ScalarValue));

        }

        // Required by the interface but unimplemented, because strings are immutable.
        public void SetIndex(Structure index, Structure value)
        {
            throw new KOSException("Strings are immutable; they can not be modified using the syntax \"SET string[1] TO 'a'\", etc.");
        }
        // Required by the interface but unimplemented, because strings are immutable.
        public void SetIndex(int index, Structure value)
        {
            throw new KOSException("Strings are immutable; they can not be modified using the syntax \"SET string[1] TO 'a'\", etc.");
        }

        public IEnumerator<string> GetEnumerator ()
        {
            for (int i = 0; i < internalString.Length; i++) {
                yield return internalString[i].ToString();
            }

        }

        System.Collections.IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
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

        public BooleanValue MatchesPattern(string pattern)
        {
            return new BooleanValue(Regex.IsMatch(internalString, pattern, RegexOptions.IgnoreCase));
        }

        public StringValue Format(params Structure[] args)
        {
            if (args.Length == 0)
                return this;
            return new StringValue(string.Format(CultureInfo.InvariantCulture, this, args));
        }

        private void StringInitializeSuffixes()
        {
            AddSuffix("LENGTH",     new NoArgsSuffix<ScalarValue>( () => Length));
            AddSuffix("SUBSTRING",  new TwoArgsSuffix<StringValue, ScalarValue, ScalarValue>( (one, two) => Substring(one, two)));
            AddSuffix("CONTAINS",   new OneArgsSuffix<BooleanValue, StringValue>( one => Contains(one)));
            AddSuffix("ENDSWITH",   new OneArgsSuffix<BooleanValue, StringValue>( one => EndsWith(one)));
            AddSuffix("FINDAT",     new TwoArgsSuffix<ScalarValue, StringValue, ScalarValue>( (one, two) => FindAt(one, two)));
            AddSuffix("INSERT",     new TwoArgsSuffix<StringValue, ScalarValue, StringValue>( (one, two) => Insert(one, two)));
            AddSuffix("FINDLASTAT", new TwoArgsSuffix<ScalarValue, StringValue, ScalarValue>( (one, two) => FindLastAt(one, two)));
            AddSuffix("PADLEFT",    new OneArgsSuffix<StringValue, ScalarValue>( one => PadLeft(one)));
            AddSuffix("PADRIGHT",   new OneArgsSuffix<StringValue, ScalarValue>( one => PadRight(one)));
            AddSuffix("REMOVE",     new TwoArgsSuffix<StringValue, ScalarValue, ScalarValue>( (one, two) => Remove(one, two)));
            AddSuffix("REPLACE",    new TwoArgsSuffix<StringValue, StringValue, StringValue>( (one, two) => Replace(one, two)));
            AddSuffix("SPLIT",      new OneArgsSuffix<ListValue<StringValue>, StringValue>( one => SplitToList(one)));
            AddSuffix("STARTSWITH", new OneArgsSuffix<BooleanValue, StringValue>( one => StartsWith(one)));
            AddSuffix("TOLOWER",    new NoArgsSuffix<StringValue>(() => ToLower()));
            AddSuffix("TOUPPER",    new NoArgsSuffix<StringValue>(() => ToUpper()));
            AddSuffix("TRIM",       new NoArgsSuffix<StringValue>(() => Trim()));
            AddSuffix("TRIMEND",    new NoArgsSuffix<StringValue>(() => TrimEnd()));
            AddSuffix("TRIMSTART",  new NoArgsSuffix<StringValue>(() => TrimStart()));
            AddSuffix("MATCHESPATTERN", new OneArgsSuffix<BooleanValue, StringValue>( one => MatchesPattern(one)));
            AddSuffix(new[] { "TONUMBER", "TOSCALAR" }, new VarArgsSuffix<ScalarValue, Structure>(ToScalarVarArgsWrapper));
            AddSuffix("FORMAT",     new VarArgsSuffix<StringValue, Structure>(Format));

            // Aliased "IndexOf" with "Find" to match "FindAt" (since IndexOfAt doesn't make sense, but I wanted to stick with common/C# names when possible)
            AddSuffix(new[] { "INDEXOF",     "FIND" },     new OneArgsSuffix<ScalarValue, StringValue>   ( one => IndexOf(one)));
            AddSuffix(new[] { "LASTINDEXOF", "FINDLAST" }, new OneArgsSuffix<ScalarValue, StringValue>   ( s => LastIndexOf(s)));
            AddSuffix("ITERATOR", new NoArgsSuffix<Enumerator>( () => new Enumerator(GetEnumerator()) ));
        }

        public static bool operator ==(StringValue val1, StringValue val2)
        {
            Type compareType = typeof(StringValue);
            if (compareType.IsInstanceOfType(val1))
            {
                return val1.Equals(val2); // val1 is not null, we can use the built in equals function
            }
            return !compareType.IsInstanceOfType(val2); // val1 is null, return true if val2 is null and false if not null
        }

        public static bool operator !=(StringValue val1, StringValue val2)
        {
            return !(val1 == val2);
        }

        public static bool operator >(StringValue val1, StringValue val2)
        {
            int compareNum = string.Compare(val1, val2, StringComparison.OrdinalIgnoreCase);
            return compareNum > 0;
        }

        public static bool operator <(StringValue val1, StringValue val2)
        {
            int compareNum = string.Compare(val1, val2, StringComparison.OrdinalIgnoreCase);
            return compareNum < 0;
        }

        public static bool operator >=(StringValue val1, StringValue val2)
        {
            int compareNum = string.Compare(val1, val2, StringComparison.OrdinalIgnoreCase);
            return compareNum >= 0;
        }

        public static bool operator <=(StringValue val1, StringValue val2)
        {
            int compareNum = string.Compare(val1, val2, StringComparison.OrdinalIgnoreCase);
            return compareNum <= 0;
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
                return string.Equals(internalString, obj.ToString(), StringComparison.OrdinalIgnoreCase);
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
            if (conversionType == typeof(StringValue))
                return this;
            else if (conversionType == typeof(BooleanValue))
                return new BooleanValue(string.IsNullOrEmpty(internalString) ? false : true);
            else if (conversionType.IsSubclassOf(typeof(Structure)))
                throw new KOSCastException(typeof(StringValue), conversionType);
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


        #region Serialization
        public override Dump Dump(DumperState s)
        {
            DumpDictionary dump = new DumpDictionary(this.GetType());

            dump.Add("value", internalString);

            return dump;
        }

        [DumpDeserializer]
        public static StringValue CreateFromDump(DumpDictionary d, SafeSharedObjects shared)
        {
            return new StringValue(d.GetString("value"));
        }

        [DumpPrinter]
        public static void Print(DumpDictionary dump, IndentedStringBuilder sb)
        {
            sb.Append("\"");
            sb.Append(dump.GetString("value").Replace("\"", "\"\""));
            sb.Append("\"");
        }
        #endregion
    }
}
