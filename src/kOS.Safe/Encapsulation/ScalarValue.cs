using System;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;

namespace kOS.Safe.Encapsulation
{
    public class ScalarValue : Structure, IConvertible
    {
        private int? internalInt;
        private double? internalDouble;

        public bool IsInt { get { return internalInt.HasValue; } }

        public bool IsDouble { get { return internalDouble.HasValue; } }

        public bool IsValid
        {
            get
            {
                if (internalInt.HasValue) return true;
                else if (internalDouble.HasValue)
                {
                    if (double.IsInfinity(internalDouble.Value) || double.IsNaN(internalDouble.Value)) return false;
                    return true;
                }
                return false;
            }
        }

        public object Value
        {
            get
            {
                if (IsInt) return internalInt.Value;
                if (IsDouble) return internalDouble.Value;
                throw new kOS.Safe.Exceptions.KOSException("Scalar value contains no double or int value");
            }
        }

        public ScalarValue(object value)
            : base()
        {
            SetValue(value);
        }

        public void InitializeSuffixes()
        {
            AddSuffix("ISINT", new Suffix<bool>(() => IsInt));
            AddSuffix("ISDOUBLE", new Suffix<bool>(() => IsDouble));
            AddSuffix("ISVALID", new Suffix<bool>(() => IsValid));
        }

        protected void SetValue(object value)
        {
            if (value is float)
                value = Convert.ToDouble(value);
            if (value is double)
            {
                bool inBounds = Int32.MinValue < (double)value && (double)value < Int32.MaxValue;
                if (inBounds && !double.IsNaN((double)value))
                {
                    // I still don't quite get what this part is doing... I'm thinking of rewriting it using modulous instead
                    int intPart = Convert.ToInt32(value);
                    if ((double)value == intPart)
                    {
                        internalInt = intPart;
                        internalDouble = null;
                        return;
                    }
                }
                internalDouble = (double)value;
                internalInt = null;
            }
            else if (value is int)
            {
                internalInt = (int)value;
                internalDouble = null;
            }
            else if (value is ScalarValue)
            {
                SetValue(((ScalarValue)value).Value);
            }
            else
            {
                throw new kOS.Safe.Exceptions.KOSException(string.Format("Failed to set scalar value.  Passed type {0}, expected Double or Int", value.GetType().Name));
            }
        }

        public int GetIntValue()
        {
            if (internalInt.HasValue)
            {
                return internalInt.Value;
            }
            else
            {
                return Convert.ToInt32(internalDouble);
            }
        }

        public double GetDoubleValue()
        {
            if (IsDouble) return internalDouble.Value;
            if (IsInt) return Convert.ToDouble(internalInt.Value);
            throw new kOS.Safe.Exceptions.KOSException("Scalar value contains no double or int value");
        }

        public override string ToString()
        {
            if (IsInt) return GetIntValue().ToString();
            else if (IsDouble) return GetDoubleValue().ToString();
            return "NaN";
        }

        public override bool Equals(object obj)
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
            return false;
        }

        public static ScalarValue Add(ScalarValue val1, ScalarValue val2)
        {
            if (val1.IsInt && val2.IsInt)
            {
                return new ScalarValue(val1.GetIntValue() + val2.GetIntValue());
            }
            return new ScalarValue(val1.GetDoubleValue() + val2.GetDoubleValue());
        }

        public static ScalarValue Subtract(ScalarValue val1, ScalarValue val2)
        {
            if (val1.IsInt && val2.IsInt)
            {
                return new ScalarValue(val1.GetIntValue() - val2.GetIntValue());
            }
            return new ScalarValue(val1.GetDoubleValue() - val2.GetDoubleValue());
        }

        public static ScalarValue Multiply(ScalarValue val1, ScalarValue val2)
        {
            if (val1.IsInt && val2.IsInt)
            {
                return new ScalarValue(val1.GetIntValue() * val2.GetIntValue());
            }
            return new ScalarValue(val1.GetDoubleValue() * val2.GetDoubleValue());
        }

        public static ScalarValue Divide(ScalarValue val1, ScalarValue val2)
        {
            if (val1.IsInt && val2.IsInt)
            {
                return new ScalarValue(val1.GetIntValue() / val2.GetIntValue());
            }
            return new ScalarValue(val1.GetDoubleValue() / val2.GetDoubleValue());
        }

        public static ScalarValue Modulous(ScalarValue val1, ScalarValue val2)
        {
            if (val1.IsInt && val2.IsInt)
            {
                return new ScalarValue(val1.GetIntValue() % val2.GetIntValue());
            }
            return new ScalarValue(val1.GetDoubleValue() % val2.GetDoubleValue());
        }

        public static ScalarValue Power(ScalarValue val1, ScalarValue val2)
        {
            if (val1.IsInt && val2.IsInt)
            {
                return new ScalarValue(Math.Pow(val1.GetIntValue(), val2.GetIntValue()));
            }
            return new ScalarValue(Math.Pow(val1.GetDoubleValue(), val2.GetDoubleValue()));
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
                return new ScalarValue(Math.Max(val1.GetIntValue(), val2.GetIntValue()));
            }
            return new ScalarValue(Math.Max(val1.GetDoubleValue(), val2.GetDoubleValue()));
        }

        public static ScalarValue Min(ScalarValue val1, ScalarValue val2)
        {
            if (val1.IsInt && val2.IsInt)
            {
                return new ScalarValue(Math.Min(val1.GetIntValue(), val2.GetIntValue()));
            }
            return new ScalarValue(Math.Min(val1.GetDoubleValue(), val2.GetDoubleValue()));
        }

        public static ScalarValue Abs(ScalarValue val)
        {
            if (val.IsInt)
            {
                return new ScalarValue(Math.Abs(val.GetIntValue()));
            }
            return new ScalarValue(Math.Abs(val.GetDoubleValue()));
        }

        public static ScalarValue operator +(ScalarValue val1, ScalarValue val2)
        {
            return Add(val1, val2);
        }

        public static ScalarValue operator ++(ScalarValue val)
        {
            return new ScalarValue(Add(val, 1));
        }

        public static ScalarValue operator -(ScalarValue val1, ScalarValue val2)
        {
            return Subtract(val1, val2);
        }

        public static ScalarValue operator --(ScalarValue val)
        {
            return new ScalarValue(Subtract(val, 1));
        }

        public static ScalarValue operator *(ScalarValue val1, ScalarValue val2)
        {
            return Multiply(val1, val2);
        }

        public static ScalarValue operator +(ScalarValue val)
        {
            return new ScalarValue(val.Value);
        }

        public static ScalarValue operator -(ScalarValue val)
        {
            return new ScalarValue(Multiply(val, -1));
        }

        public static ScalarValue operator /(ScalarValue val1, ScalarValue val2)
        {
            return Divide(val1, val2);
        }

        public static ScalarValue operator %(ScalarValue val1, ScalarValue val2)
        {
            return Modulous(val1, val2);
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
            return new ScalarValue(val);
        }

        public static implicit operator ScalarValue(double val)
        {
            return new ScalarValue(val);
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
}