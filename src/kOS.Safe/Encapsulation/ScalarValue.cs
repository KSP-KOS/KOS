using kOS.Safe.Exceptions;
using kOS.Safe.Serialization;
using System;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace kOS.Safe.Encapsulation
{
    [kOS.Safe.Utilities.KOSNomenclature("Scalar")]
    abstract public class ScalarValue : PrimitiveStructure, IConvertible
    {
        // regular expression used to match white space surrounding the "e" in scientific notation
        private static readonly Regex trimPattern = new Regex(@"(?![\d\.])\s*[eE]\s*(?=[\d\+-])");

        // array of characters used to determine if a string should be parsed as double, "." or "," depends on culture
        private static readonly char[] doubleCharacters = new char[] { '.', ',', 'e', 'E' };

        abstract public bool IsInt { get; }

        abstract public bool IsDouble { get; }

        abstract public bool BooleanMeaning { get; }

        public bool IsValid
        {
            get
            {
                if (IsInt) return true;
                if (IsDouble)
                {
                    double d = GetDoubleValue();
                    if (double.IsInfinity(d) || double.IsNaN(d)) return false;
                    return true;
                }
                return false;
            }
        }

        protected ScalarValue()
        {
            InitializeSuffixes();
        }


        public void InitializeSuffixes()
        {
            // TODO: Commented suffixes until the introduction of kOS types to the user.
            //AddSuffix("ISINTEGER", new Suffix<BooleanValue>(() => IsInt));
            //AddSuffix("ISVALID", new Suffix<BooleanValue>(() => IsValid));
        }

        public static ScalarValue Create(object value)
        {
            Type compareType = typeof(ScalarValue);
            if (compareType.IsInstanceOfType(value))
            {
                return (ScalarValue)value;
            }
            if (value is float)
                value = Convert.ToDouble(value);
            if (value is double)
            {
                bool inBounds = int.MinValue < (double)value && (double)value < int.MaxValue;
                if (inBounds && !double.IsNaN((double)value))
                {
                    // Convert the double to an int, and check and see if they are still equal.
                    // if so, treat the double as if it was an int.
                    int intPart = Convert.ToInt32(value);

                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if ((double)value == intPart)
                    {
                        return new ScalarIntValue(intPart);
                    }
                }
                return new ScalarDoubleValue((double)value);
            }

            if (value is int)
            {
                return new ScalarIntValue((int)value);
            }

            throw new KOSException(string.Format("Failed to set scalar value.  Passed type {0}, expected Double or Int", value.GetType().Name));
        }

        public static bool TryParse(string str, out ScalarValue result)
        {
            result = null; // default the out value to null
            str = str.Replace("_","");
          
            bool needsDoubleParse = str.IndexOfAny(doubleCharacters) >= 0;

            if (needsDoubleParse)
            {
                return TryParseDouble(str, out result);
            }
            else
            {
                return TryParseInt(str, out result);
            }
        }

        public static bool TryParseInt(string str, out ScalarValue result)
        {
            result = null; // default the out value to null
            int val;
            str = str.Replace("_","");
            if (int.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out val))
            {
                result = new ScalarIntValue(val);
                return true;
            }
            return false;
        }

        public static bool TryParseDouble(string str, out ScalarValue result)
        {
            result = null; // default the out value to null
            str = trimPattern.Replace(str, "E").Replace("_",""); // remove white space around "e" and strip spacing underscores.
            double val;
            if (double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out val))
            {
                // use Create instead of new ScalarDoubleValue so doubles that
                // represent integers will output a ScalarIntValue instead
                result = Create(val);
                return true;
            }
            return false;
        }

        public abstract int GetIntValue();
        public abstract double GetDoubleValue();

        public override string ToString()
        {
            if (IsInt) return GetIntValue().ToString();
            else if (IsDouble) return GetDoubleValue().ToString();
            return "NaN";
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            Type compareType = typeof(ScalarValue);
            if (compareType.IsInstanceOfType(obj))
            {
                var val = obj as ScalarValue;
                if (IsInt && val.IsInt)
                {
                    return GetIntValue() == val.GetIntValue();
                }
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                return GetDoubleValue() == val.GetDoubleValue();
            }
            else
            {
                const BindingFlags FLAGS = BindingFlags.ExactBinding | BindingFlags.Static | BindingFlags.Public;
                MethodInfo converter = typeof(ScalarValue).GetMethod("op_Implicit", FLAGS, null, new[] { obj.GetType() }, null);
                if (converter != null)
                {
                    var val = (ScalarValue)converter.Invoke(null, new[] { obj });
                    if (ToPrimitive() == val.ToPrimitive()) return true;
                }
            }
            return false;
        }

        public override int GetHashCode()
        {
            return ToPrimitive().GetHashCode();
        }

        public static ScalarValue Add(ScalarValue val1, ScalarValue val2)
        {
            return Create(val1.GetDoubleValue() + val2.GetDoubleValue());
        }

        public static ScalarValue Subtract(ScalarValue val1, ScalarValue val2)
        {
            return Create(val1.GetDoubleValue() - val2.GetDoubleValue());
        }

        public static ScalarValue Multiply(ScalarValue val1, ScalarValue val2)
        {
            return Create(val1.GetDoubleValue() * val2.GetDoubleValue());
        }

        public static ScalarValue Divide(ScalarValue val1, ScalarValue val2)
        {
            return Create(val1.GetDoubleValue() / val2.GetDoubleValue());
        }

        public static ScalarValue Modulus(ScalarValue val1, ScalarValue val2)
        {
            if (val1.IsInt && val2.IsInt)
            {
                return Create(val1.GetIntValue() % val2.GetIntValue());
            }
            return Create(val1.GetDoubleValue() % val2.GetDoubleValue());
        }

        public static ScalarValue Power(ScalarValue val1, ScalarValue val2)
        {
            return Create(Math.Pow(val1.GetDoubleValue(), val2.GetDoubleValue()));
        }

        public static bool GreaterThan(ScalarValue val1, ScalarValue val2)
        {
            return val1.GetDoubleValue() > val2.GetDoubleValue();
        }

        public static bool LessThan(ScalarValue val1, ScalarValue val2)
        {
            return val1.GetDoubleValue() < val2.GetDoubleValue();
        }

        public static ScalarValue Max(ScalarValue val1, ScalarValue val2)
        {
            return Create(Math.Max(val1.GetDoubleValue(), val2.GetDoubleValue()));
        }

        public static ScalarValue Min(ScalarValue val1, ScalarValue val2)
        {
            return Create(Math.Min(val1.GetDoubleValue(), val2.GetDoubleValue()));
        }

        public static ScalarValue Abs(ScalarValue val)
        {
            return Create(Math.Abs(val.GetDoubleValue()));
        }

        public static ScalarValue operator +(ScalarValue val1, ScalarValue val2)
        {
            return Add(val1, val2);
        }

        public static ScalarValue operator ++(ScalarValue val)
        {
            return Add(val, 1);
        }

        public static ScalarValue operator -(ScalarValue val1, ScalarValue val2)
        {
            return Subtract(val1, val2);
        }

        public static ScalarValue operator --(ScalarValue val)
        {
            return Subtract(val, 1);
        }

        public static ScalarValue operator *(ScalarValue val1, ScalarValue val2)
        {
            return Multiply(val1, val2);
        }

        public static ScalarValue operator +(ScalarValue val)
        {
            return Create(val.ToPrimitive());
        }

        public static ScalarValue operator -(ScalarValue val)
        {
            return Multiply(val, -1);
        }

        public static ScalarValue operator /(ScalarValue val1, ScalarValue val2)
        {
            return Divide(val1, val2);
        }

        public static ScalarValue operator %(ScalarValue val1, ScalarValue val2)
        {
            return Modulus(val1, val2);
        }

        public static ScalarValue operator ^(ScalarValue val1, ScalarValue val2)
        {
            return Power(val1, val2);
        }

        public static bool operator ==(ScalarValue val1, ScalarValue val2)
        {
            Type compareType = typeof(ScalarValue);
            if (compareType.IsInstanceOfType(val1))
            {
                return val1.Equals(val2); // val1 is not null, we can use the built in equals function
            }
            return !compareType.IsInstanceOfType(val2); // val1 is null, return true if val2 is null and false if not null
        }

        public static bool operator !=(ScalarValue val1, ScalarValue val2)
        {
            return !(val1 == val2);
        }

        public static bool operator >(ScalarValue val1, ScalarValue val2)
        {
            return GreaterThan(val1, val2);
        }

        public static bool operator <(ScalarValue val1, ScalarValue val2)
        {
            return LessThan(val1, val2);
        }

        public static bool operator >=(ScalarValue val1, ScalarValue val2)
        {
            return GreaterThan(val1, val2) || val1.Equals(val2);
        }

        public static bool operator <=(ScalarValue val1, ScalarValue val2)
        {
            return LessThan(val1, val2) || val1.Equals(val2);
        }

        public static implicit operator ScalarValue(int val)
        {
            return Create(val);
        }

        public static implicit operator ScalarValue(double val)
        {
            return Create(val);
        }

        public static implicit operator int(ScalarValue val)
        {
            return val.GetIntValue();
        }

        public static implicit operator double(ScalarValue val)
        {
            return val.GetDoubleValue();
        }

        public static implicit operator float(ScalarValue val)
        {
            return (float)val.GetDoubleValue();
        }

        TypeCode IConvertible.GetTypeCode()
        {
            return TypeCode.Object;
        }

        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            return BooleanMeaning;
        }

        byte IConvertible.ToByte(IFormatProvider provider)
        {
            throw new KOSCastException(typeof(ScalarValue), typeof(byte));
        }

        char IConvertible.ToChar(IFormatProvider provider)
        {
            throw new KOSCastException(typeof(ScalarValue), typeof(char));
        }

        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            throw new KOSCastException(typeof(ScalarValue), typeof(DateTime));
        }

        decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            return Convert.ToDecimal(GetDoubleValue());
        }

        double IConvertible.ToDouble(IFormatProvider provider)
        {
            return GetDoubleValue();
        }

        short IConvertible.ToInt16(IFormatProvider provider)
        {
            return Convert.ToInt16(GetIntValue());
        }

        int IConvertible.ToInt32(IFormatProvider provider)
        {
            return GetIntValue();
        }

        long IConvertible.ToInt64(IFormatProvider provider)
        {
            return Convert.ToInt64(GetIntValue());
        }

        sbyte IConvertible.ToSByte(IFormatProvider provider)
        {
            throw new KOSCastException(typeof(ScalarValue), typeof(sbyte));
        }

        float IConvertible.ToSingle(IFormatProvider provider)
        {
            return Convert.ToSingle(GetDoubleValue());
        }

        string IConvertible.ToString(IFormatProvider provider)
        {
            return ToString();
        }

        object IConvertible.ToType(Type conversionType, IFormatProvider provider)
        {
            // These can't be handled by ScalarValue.Create() because they MUST coerce into the asked-for type,
            // ignoring the logic used in Create() to vary the type depending on content.
            if (conversionType == GetType())
                return this;  // no conversion needed
            else if (conversionType == typeof(ScalarValue))
                return this; // no conversion needed
            else if (conversionType == typeof(ScalarDoubleValue))
                return new ScalarDoubleValue(GetDoubleValue());
            else if (conversionType == typeof(ScalarIntValue))
                return new ScalarIntValue(GetIntValue());
            else if (conversionType == typeof(BooleanValue))
                return new BooleanValue(GetIntValue() == 0 ? false : true);
            else if (conversionType.IsSubclassOf(typeof(Structure)))
                throw new KOSCastException(typeof(ScalarValue), conversionType);
            else
                return Convert.ChangeType(GetDoubleValue(), conversionType);
        }

        ushort IConvertible.ToUInt16(IFormatProvider provider)
        {
            return Convert.ToUInt16(GetIntValue());
        }

        uint IConvertible.ToUInt32(IFormatProvider provider)
        {
            return Convert.ToUInt32(GetIntValue());
        }

        ulong IConvertible.ToUInt64(IFormatProvider provider)
        {
            return Convert.ToUInt64(GetIntValue());
        }
    }
}