using System;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;

namespace kOS.Safe.Encapsulation
{
    abstract public class ScalarValue : Structure, IConvertible, ISerializableValue
    {
        protected object internalValue;

        abstract public bool IsInt { get; }

        abstract public bool IsDouble { get; }

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

        public object Value { get { return internalValue; } }

        protected ScalarValue()
        {
            InitializeSuffixes();
        }

        public void InitializeSuffixes()
        {
            AddSuffix("ISINT", new Suffix<bool>(() => IsInt));
            AddSuffix("ISDOUBLE", new Suffix<bool>(() => IsDouble));
            AddSuffix("ISVALID", new Suffix<bool>(() => IsValid));
        }

        public static ScalarValue Create(object value)
        {
            if (value is float)
                value = Convert.ToDouble(value);
            if (value is double)
            {
                bool inBounds = Int32.MinValue < (double)value && (double)value < Int32.MaxValue;
                if (inBounds && !double.IsNaN((double)value))
                {
                    // Convert the double to an int, and check and see if they are still equal.
                    // if so, treat the double as if it was an int.
                    int intPart = Convert.ToInt32(value);
                    if ((double)value == intPart)
                    {
                        return new ScalarIntValue(intPart);
                    }
                }
                return new ScalarDoubleValue((double)value);
            }
            else if (value is int)
            {
                return new ScalarIntValue((int)value);
            }
            else if (value is ScalarValue)
            {
                return ScalarValue.Create(((ScalarValue)value).Value);
            }
            else
            {
                throw new kOS.Safe.Exceptions.KOSException(string.Format("Failed to set scalar value.  Passed type {0}, expected Double or Int", value.GetType().Name));
            }
        }

        public int GetIntValue()
        {
            return Convert.ToInt32(Value);
        }

        public double GetDoubleValue()
        {
            return Convert.ToDouble(Value);
        }

        public override string ToString()
        {
            if (IsInt) return GetIntValue().ToString();
            else if (IsDouble) return GetDoubleValue().ToString();
            return "NaN";
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj is ScalarValue)
            {
                ScalarValue val = obj as ScalarValue;
                if (val != null)
                {
                    if (this.IsInt && val.IsInt)
                    {
                        return this.GetIntValue() == val.GetIntValue();
                    }
                    return this.GetDoubleValue() == val.GetDoubleValue();
                }
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public static ScalarValue Add(ScalarValue val1, ScalarValue val2)
        {
            if (val1.IsInt && val2.IsInt)
            {
                return ScalarValue.Create(val1.GetIntValue() + val2.GetIntValue());
            }
            return ScalarValue.Create(val1.GetDoubleValue() + val2.GetDoubleValue());
        }

        public static ScalarValue Subtract(ScalarValue val1, ScalarValue val2)
        {
            if (val1.IsInt && val2.IsInt)
            {
                return ScalarValue.Create(val1.GetIntValue() - val2.GetIntValue());
            }
            return ScalarValue.Create(val1.GetDoubleValue() - val2.GetDoubleValue());
        }

        public static ScalarValue Multiply(ScalarValue val1, ScalarValue val2)
        {
            if (val1.IsInt && val2.IsInt)
            {
                return ScalarValue.Create(val1.GetIntValue() * val2.GetIntValue());
            }
            return ScalarValue.Create(val1.GetDoubleValue() * val2.GetDoubleValue());
        }

        public static ScalarValue Divide(ScalarValue val1, ScalarValue val2)
        {
            if (val1.IsInt && val2.IsInt)
            {
                return new ScalarIntValue(val1.GetIntValue() / val2.GetIntValue());
            }
            return ScalarValue.Create(val1.GetDoubleValue() / val2.GetDoubleValue());
        }

        public static ScalarValue Modulus(ScalarValue val1, ScalarValue val2)
        {
            if (val1.IsInt && val2.IsInt)
            {
                return ScalarValue.Create(val1.GetIntValue() % val2.GetIntValue());
            }
            return ScalarValue.Create(val1.GetDoubleValue() % val2.GetDoubleValue());
        }

        public static ScalarValue Power(ScalarValue val1, ScalarValue val2)
        {
            if (val1.IsInt && val2.IsInt)
            {
                return ScalarValue.Create(Math.Pow(val1.GetIntValue(), val2.GetIntValue()));
            }
            return ScalarValue.Create(Math.Pow(val1.GetDoubleValue(), val2.GetDoubleValue()));
        }

        public static bool GreaterThan(ScalarValue val1, ScalarValue val2)
        {
            if (val1.IsInt && val2.IsInt)
            {
                return val1.GetIntValue() > val2.GetIntValue();
            }
            return val1.GetDoubleValue() > val2.GetDoubleValue();
        }

        public static bool LessThan(ScalarValue val1, ScalarValue val2)
        {
            if (val1.IsInt && val2.IsInt)
            {
                return val1.GetIntValue() < val2.GetIntValue();
            }
            return val1.GetDoubleValue() < val2.GetDoubleValue();
        }

        public static ScalarValue Max(ScalarValue val1, ScalarValue val2)
        {
            if (val1.IsInt && val2.IsInt)
            {
                return ScalarValue.Create(Math.Max(val1.GetIntValue(), val2.GetIntValue()));
            }
            return ScalarValue.Create(Math.Max(val1.GetDoubleValue(), val2.GetDoubleValue()));
        }

        public static ScalarValue Min(ScalarValue val1, ScalarValue val2)
        {
            if (val1.IsInt && val2.IsInt)
            {
                return new ScalarIntValue(Math.Min(val1.GetIntValue(), val2.GetIntValue()));
            }
            return ScalarValue.Create(Math.Min(val1.GetDoubleValue(), val2.GetDoubleValue()));
        }

        public static ScalarValue Abs(ScalarValue val)
        {
            if (val.IsInt)
            {
                return new ScalarIntValue(Math.Abs(val.GetIntValue()));
            }
            return ScalarValue.Create(Math.Abs(val.GetDoubleValue()));
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
            return ScalarValue.Create(val.Value);
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
            return val1.Equals(val2);
        }

        public static bool operator ==(ScalarValue val1, object val2)
        {
            return val1.Equals(val2);
        }

        public static bool operator ==(object val1, ScalarValue val2)
        {
            return val2.Equals(val1);
        }

        public static bool operator !=(ScalarValue val1, ScalarValue val2)
        {
            return !val1.Equals(val2);
        }

        public static bool operator !=(ScalarValue val1, object val2)
        {
            return !val1.Equals(val2);
        }

        public static bool operator !=(object val1, ScalarValue val2)
        {
            return !val2.Equals(val1);
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
            return ScalarValue.Create(val);
        }

        public static implicit operator ScalarValue(double val)
        {
            return ScalarValue.Create(val);
        }

        public static implicit operator int(ScalarValue val)
        {
            return val.GetIntValue();
        }

        public static implicit operator double(ScalarValue val)
        {
            return val.GetDoubleValue();
        }

        TypeCode IConvertible.GetTypeCode()
        {
            return TypeCode.Object;
        }

        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            if (GetIntValue() == 0) return false;
            return true;
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
            throw new KOSCastException(typeof(ScalarValue), typeof(SByte));
        }

        float IConvertible.ToSingle(IFormatProvider provider)
        {
            throw new KOSCastException(typeof(ScalarValue), typeof(Single));
        }

        string IConvertible.ToString(IFormatProvider provider)
        {
            return ToString();
        }

        object IConvertible.ToType(Type conversionType, IFormatProvider provider)
        {
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

    public class ScalarIntValue : ScalarValue
    {
        public override bool IsDouble
        {
            get { return false; }
        }
        public override bool IsInt
        {
            get { return true; }
        }

        public ScalarIntValue(int value)
        {
            internalValue = value;
        }
    }

    public class ScalarDoubleValue : ScalarValue
    {
        public override bool IsDouble
        {
            get { return true; }
        }
        public override bool IsInt
        {
            get { return false; }
        }

        public ScalarDoubleValue(double value)
        {
            internalValue = value;
        }
    }
}