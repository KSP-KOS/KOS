using kOS.Safe.Exceptions;
using System;
using System.Reflection;

namespace kOS.Safe.Encapsulation
{
    abstract public class ScalarValue : Structure, IConvertible, ISerializableValue
    {
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

        public object Value { get; protected set; }

        protected ScalarValue()
        {
            InitializeSuffixes();
        }

        public void InitializeSuffixes()
        {
            // TODO: Commented suffixes until the introduction of kOS types to the user.
            //AddSuffix("ISINTEGER", new Suffix<bool>(() => IsInt));
            //AddSuffix("ISVALID", new Suffix<bool>(() => IsValid));
        }

        public static ScalarValue Create(object value)
        {
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

            var scalarValue = value as ScalarValue;
            if (scalarValue != null)
            {
                return Create(scalarValue.Value);
            }

            throw new KOSException(string.Format("Failed to set scalar value.  Passed type {0}, expected Double or Int", value.GetType().Name));
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
            var val = obj as ScalarValue;
            if (val != null)
            {
                if (IsInt && val.IsDouble)
                {
                    return false;
                }
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
                    val = (ScalarValue)converter.Invoke(null, new[] { obj });
                    if (Value == val.Value) return true;
                }
            }
            return false;
        }

        public static bool NullSafeEquals(object obj1, object obj2)
        {
            if (obj1 == null)
            {
                if (obj2 == null) return true;
                return false;
            }
            if (obj2 == null) return false;
            ScalarValue val1 = Create(obj1);
            return val1.Equals(obj2);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public static ScalarValue Add(ScalarValue val1, ScalarValue val2)
        {
            if (val1.IsInt && val2.IsInt)
            {
                return Create(val1.GetIntValue() + val2.GetIntValue());
            }
            return Create(val1.GetDoubleValue() + val2.GetDoubleValue());
        }

        public static ScalarValue Subtract(ScalarValue val1, ScalarValue val2)
        {
            if (val1.IsInt && val2.IsInt)
            {
                return Create(val1.GetIntValue() - val2.GetIntValue());
            }
            return Create(val1.GetDoubleValue() - val2.GetDoubleValue());
        }

        public static ScalarValue Multiply(ScalarValue val1, ScalarValue val2)
        {
            if (val1.IsInt && val2.IsInt)
            {
                return Create(val1.GetIntValue() * val2.GetIntValue());
            }
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
                return Create(Math.Max(val1.GetIntValue(), val2.GetIntValue()));
            }
            return Create(Math.Max(val1.GetDoubleValue(), val2.GetDoubleValue()));
        }

        public static ScalarValue Min(ScalarValue val1, ScalarValue val2)
        {
            if (val1.IsInt && val2.IsInt)
            {
                return new ScalarIntValue(Math.Min(val1.GetIntValue(), val2.GetIntValue()));
            }
            return Create(Math.Min(val1.GetDoubleValue(), val2.GetDoubleValue()));
        }

        public static ScalarValue Abs(ScalarValue val)
        {
            if (val.IsInt)
            {
                return new ScalarIntValue(Math.Abs(val.GetIntValue()));
            }
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
            return Create(val.Value);
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
            return NullSafeEquals(val1, val2);
        }

        public static bool operator !=(ScalarValue val1, ScalarValue val2)
        {
            return !NullSafeEquals(val1, val2);
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
            throw new KOSCastException(typeof(ScalarValue), typeof(sbyte));
        }

        float IConvertible.ToSingle(IFormatProvider provider)
        {
            throw new KOSCastException(typeof(ScalarValue), typeof(float));
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